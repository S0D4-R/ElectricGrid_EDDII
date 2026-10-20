using ElectricGrid.Core.Collections;

namespace ElectricGrid.Core.Models
{
    /// <summary>Summary of a file import, including per-row problems.</summary>
    public sealed class LoadReport
    {
        /// <summary>Creates a report.</summary>
        public LoadReport(bool fileFound, int substationsLoaded, int linesLoaded, int rowsSkipped, string[] errors)
        {
            FileFound = fileFound;
            SubstationsLoaded = substationsLoaded;
            LinesLoaded = linesLoaded;
            RowsSkipped = rowsSkipped;
            Errors = errors;
        }

        /// <summary>Whether the file was located and opened.</summary>
        public bool FileFound { get; }

        /// <summary>Substations successfully registered.</summary>
        public int SubstationsLoaded { get; }

        /// <summary>Lines successfully registered.</summary>
        public int LinesLoaded { get; }

        /// <summary>Rows that were rejected.</summary>
        public int RowsSkipped { get; }

        /// <summary>Description of every rejected row or file-level problem.</summary>
        public string[] Errors { get; }

        /// <summary>Creates a report for a file that could not be used.</summary>
        public static LoadReport Failure(string message) => new LoadReport(false, 0, 0, 0, new[] { message });

        /// <summary>Merges two reports into one.</summary>
        public static LoadReport Combine(LoadReport first, LoadReport second)
        {
            ArrayBuffer<string> errors = new ArrayBuffer<string>();
            for (int i = 0; i < first.Errors.Length; i++)
            {
                errors.Add(first.Errors[i]);
            }

            for (int i = 0; i < second.Errors.Length; i++)
            {
                errors.Add(second.Errors[i]);
            }

            return new LoadReport(
                first.FileFound && second.FileFound,
                first.SubstationsLoaded + second.SubstationsLoaded,
                first.LinesLoaded + second.LinesLoaded,
                first.RowsSkipped + second.RowsSkipped,
                errors.ToArray());
        }
    }
}
