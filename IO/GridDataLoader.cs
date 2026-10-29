using System;
using System.Globalization;
using System.IO;
using ElectricGrid.Collections;
using ElectricGrid.Models;
using ElectricGrid.Services;

namespace ElectricGrid.IO
{
    // Lee subestaciones y líneas desde archivos CSV o TXT.
    public sealed class GridDataLoader
    {
        private const char Separator = ',';
        private readonly IPowerGridService _service;

        // Guarda el servicio que recibirá los datos cargados.
        public GridDataLoader(IPowerGridService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        // Busca el archivo en la ruta indicada o en las carpetas de datos.
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

        // Carga las subestaciones y registra las filas válidas.
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
                    if (IsHeader(fields[0], "codigo", "code"))
                    {
                        continue;
                    }
                }

                if (fields.Length < 3)
                {
                    skipped++;
                    errors.Add($"{fileName}:{i + 1} se esperaban 3 campos (codigo,nombre,capacidad_mw).");
                    continue;
                }

                if (!TryParseNumber(fields[2], out double capacity))
                {
                    skipped++;
                    errors.Add($"{fileName}:{i + 1} capacidad inválida '{fields[2]}'.");
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

        // Carga las líneas; las subestaciones deben existir antes.
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
                    if (IsHeader(fields[0], "origen", "source"))
                    {
                        continue;
                    }
                }

                if (fields.Length < 4)
                {
                    skipped++;
                    errors.Add($"{fileName}:{i + 1} se esperaban al menos 4 campos (origen,destino,longitud_km,costo_mantenimiento).");
                    continue;
                }

                if (!TryParseNumber(fields[2], out double length) || !TryParseNumber(fields[3], out double cost))
                {
                    skipped++;
                    errors.Add($"{fileName}:{i + 1} longitud o costo inválidos.");
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

        // Carga primero las subestaciones y después las líneas.
        public LoadReport LoadAll(string substationsPath, string linesPath)
        {
            LoadReport substations = LoadSubstations(substationsPath);
            LoadReport lines = LoadLines(linesPath);
            return LoadReport.Combine(substations, lines);
        }

        // Resuelve y lee el archivo; deja el error listo si no se puede.
        private static bool TryReadLines(string path, out string[] lines, out string fileName, out LoadReport? failure)
        {
            lines = new string[0];
            fileName = string.Empty;
            failure = null;

            string? resolved = ResolvePath(path);
            if (resolved == null)
            {
                failure = LoadReport.Failure($"No se encontró el archivo '{path}'. Se buscó en la ruta indicada, junto al ejecutable y en su carpeta Data.");
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
                failure = LoadReport.Failure($"No se pudo leer el archivo '{resolved}': {ex.Message}");
                return false;
            }
        }

        // Separa los campos y descarta líneas vacías o comentadas.
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

        // Reconoce el encabezado en español o en inglés.
        private static bool IsHeader(string firstField, string spanishName, string englishName)
            => firstField.StartsWith(spanishName, StringComparison.OrdinalIgnoreCase)
               || firstField.StartsWith(englishName, StringComparison.OrdinalIgnoreCase);

        // Interpreta números con punto decimal.
        private static bool TryParseNumber(string text, out double value)
            => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        // Acepta true, 1, yes, si y sus variantes.
        private static bool ParseBoolean(string text)
        {
            string value = text.Trim().ToLowerInvariant();
            return value == "true" || value == "1" || value == "yes" || value == "y" || value == "si" || value == "sí" || value == "s";
        }
    }
}
