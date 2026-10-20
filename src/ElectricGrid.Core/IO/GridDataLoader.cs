using System;
using System.Globalization;
using System.IO;
using ElectricGrid.Core.Collections;
using ElectricGrid.Core.Models;
using ElectricGrid.Core.Services;

namespace ElectricGrid.Core.IO
{
    /// <summary>
    /// Imports substations and lines from delimited text files (.csv / .txt), so no data is
    /// hard-coded in the program.
    /// <para>Substations file: <c>code,name,capacity_mw</c></para>
    /// <para>Lines file: <c>source_code,target_code,length_km,maintenance_cost,bidirectional</c>
    /// (the last column is optional and defaults to <c>true</c>).</para>
    /// Blank lines and lines starting with <c>#</c> are ignored; an optional header row is skipped.
    /// Only native file and string helpers are used here; all the logic lives in the custom structures.
    /// </summary>
    public sealed class GridDataLoader
    {
        private const char Separator = ',';
        private readonly IPowerGridService _service;

        /// <summary>Creates a loader bound to a grid service.</summary>
        public GridDataLoader(IPowerGridService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Locates a data file. Tries the path as typed, then relative to the application folder
        /// and to its <c>Data</c> sub-folder.
        /// </summary>
        /// <returns>The full path, or <c>null</c> if the file does not exist.</returns>
        public static string? ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string cleaned = path.Trim().Trim('"');

            if (File.Exists(cleaned))
            {
                return Path.GetFullPath(cleaned);
            }

            if (!Path.IsPathRooted(cleaned))
            {
                string besideExecutable = Path.Combine(AppContext.BaseDirectory, cleaned);
                if (File.Exists(besideExecutable))
                {
                    return besideExecutable;
                }

                string inDataFolder = Path.Combine(AppContext.BaseDirectory, "Data", cleaned);
                if (File.Exists(inDataFolder))
                {
                    return inDataFolder;
                }
            }

            return null;
        }

        /// <summary>Loads substations from a file.</summary>
        public LoadReport LoadSubstations(string path)
        {
            if (!TryReadLines(path, out string[] lines, out string fileName, out LoadReport? failure))
            {
                return failure!;
            }

            ArrayBuffer<string> errors = new ArrayBuffer<string>();
            int loaded = 0;
            int skipped = 0;
            bool firstDataRow = true;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!TryGetFields(lines[i], out string[] fields))
                {
                    continue;
                }

                if (firstDataRow)
                {
                    firstDataRow = false;
                    if (IsHeader(fields[0], "code"))
                    {
                        continue;
                    }
                }

                if (fields.Length < 3)
                {
                    skipped++;
                    errors.Add($"{fileName}:{i + 1} expected 3 fields (code,name,capacity_mw).");
                    continue;
                }

                if (!TryParseNumber(fields[2], out double capacity))
                {
                    skipped++;
                    errors.Add($"{fileName}:{i + 1} invalid capacity '{fields[2]}'.");
                    continue;
                }

                OperationResult result = _service.RegisterSubstation(fields[0], fields[1], capacity);
                if (result.Success)
                {
                    loaded++;
                }
                else
                {
                    skipped++;
                    errors.Add($"{fileName}:{i + 1} {result.Message}");
                }
            }

            return new LoadReport(true, loaded, 0, skipped, errors.ToArray());
        }

        /// <summary>Loads lines from a file. Substations must already be registered.</summary>
        public LoadReport LoadLines(string path)
        {
            if (!TryReadLines(path, out string[] lines, out string fileName, out LoadReport? failure))
            {
                return failure!;
            }

            ArrayBuffer<string> errors = new ArrayBuffer<string>();
            int loaded = 0;
            int skipped = 0;
            bool firstDataRow = true;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!TryGetFields(lines[i], out string[] fields))
                {
                    continue;
                }

                if (firstDataRow)
                {
                    firstDataRow = false;
                    if (IsHeader(fields[0], "source"))
                    {
                        continue;
                    }
                }

                if (fields.Length < 4)
                {
                    skipped++;
                    errors.Add($"{fileName}:{i + 1} expected at least 4 fields (source,target,length_km,maintenance_cost).");
                    continue;
                }

                if (!TryParseNumber(fields[2], out double length) || !TryParseNumber(fields[3], out double cost))
                {
                    skipped++;
                    errors.Add($"{fileName}:{i + 1} invalid length or cost.");
                    continue;
                }

                bool bidirectional = fields.Length < 5 || ParseBoolean(fields[4]);

                OperationResult result = _service.RegisterLine(new PowerLine(fields[0], fields[1], length, cost, bidirectional));
                if (result.Success)
                {
                    loaded++;
                }
                else
                {
                    skipped++;
                    errors.Add($"{fileName}:{i + 1} {result.Message}");
                }
            }

            return new LoadReport(true, 0, loaded, skipped, errors.ToArray());
        }

        /// <summary>Loads a substations file followed by a lines file.</summary>
        public LoadReport LoadAll(string substationsPath, string linesPath)
        {
            LoadReport substations = LoadSubstations(substationsPath);
            LoadReport lines = LoadLines(linesPath);
            return LoadReport.Combine(substations, lines);
        }

        private static bool TryReadLines(string path, out string[] lines, out string fileName, out LoadReport? failure)
        {
            lines = new string[0];
            fileName = string.Empty;
            failure = null;

            string? resolved = ResolvePath(path);
            if (resolved == null)
            {
                failure = LoadReport.Failure($"File not found: '{path}'. Searched the given path, the application folder and its Data folder.");
                return false;
            }

            try
            {
                lines = File.ReadAllLines(resolved);
                fileName = Path.GetFileName(resolved);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                failure = LoadReport.Failure($"The file '{resolved}' could not be read: {ex.Message}");
                return false;
            }
        }

        private static bool TryGetFields(string rawLine, out string[] fields)
        {
            string trimmed = rawLine.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#')
            {
                fields = new string[0];
                return false;
            }

            fields = trimmed.Split(Separator);
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i] = fields[i].Trim();
            }

            return true;
        }

        private static bool IsHeader(string firstField, string expected)
            => string.Equals(firstField, expected, StringComparison.OrdinalIgnoreCase)
               || firstField.StartsWith(expected + "_", StringComparison.OrdinalIgnoreCase);

        private static bool TryParseNumber(string text, out double value)
            => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static bool ParseBoolean(string text)
        {
            string value = text.Trim().ToLowerInvariant();
            return value == "true" || value == "1" || value == "yes" || value == "y" || value == "si" || value == "sí";
        }
    }
}
