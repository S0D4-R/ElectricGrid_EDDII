using ElectricGrid.Collections;

namespace ElectricGrid.Models
{
    public sealed class LoadReport
    {
        // Guarda el resultado de la carga y los errores encontrados.
        public LoadReport(bool fileFound, int substationsLoaded, int linesLoaded, int rowsSkipped, string[] errors)
        {
            FileFound = fileFound;
            SubstationsLoaded = substationsLoaded;
            LinesLoaded = linesLoaded;
            RowsSkipped = rowsSkipped;
            Errors = errors;
        }

        public bool FileFound { get; }

        public int SubstationsLoaded { get; }

        public int LinesLoaded { get; }

        public int RowsSkipped { get; }

        public string[] Errors { get; }

        // Crea un reporte para un archivo que no se pudo leer.
        public static LoadReport Failure(string message) => new LoadReport(false, 0, 0, 0, new[] { message });

        // Junta los dos reportes en uno solo.
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
