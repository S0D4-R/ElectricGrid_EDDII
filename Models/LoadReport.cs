using ElectricGrid.Collections;

namespace ElectricGrid.Models
{
    /// <summary>Resumen de una importación de archivo, con los problemas encontrados por fila.</summary>
    public sealed class LoadReport
    {
        /// <summary>Crea un reporte de carga.</summary>
        public LoadReport(bool fileFound, int substationsLoaded, int linesLoaded, int rowsSkipped, string[] errors)
        {
            FileFound = fileFound;
            SubstationsLoaded = substationsLoaded;
            LinesLoaded = linesLoaded;
            RowsSkipped = rowsSkipped;
            Errors = errors;
        }

        /// <summary>Indica si el archivo fue localizado y abierto.</summary>
        public bool FileFound { get; }

        /// <summary>Subestaciones registradas correctamente.</summary>
        public int SubstationsLoaded { get; }

        /// <summary>Líneas registradas correctamente.</summary>
        public int LinesLoaded { get; }

        /// <summary>Filas rechazadas.</summary>
        public int RowsSkipped { get; }

        /// <summary>Descripción de cada fila rechazada o problema de archivo.</summary>
        public string[] Errors { get; }

        /// <summary>Crea un reporte para un archivo que no pudo usarse.</summary>
        public static LoadReport Failure(string message) => new LoadReport(false, 0, 0, 0, new[] { message });

        /// <summary>Combina dos reportes en uno.</summary>
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
