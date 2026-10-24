using System;
using System.Text;
using ElectricGrid.Tui;
using ElectricGrid.Services;

namespace ElectricGrid
{
    /// <summary>
    /// Punto de entrada único de la aplicación. Crea el servicio del núcleo e inicia directamente la interfaz TUI.
    /// El parámetro opcional --ascii fuerza el dibujo con caracteres ASCII simples.
    /// </summary>
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            PowerGridService service = new PowerGridService();
            bool ascii = HasFlag(args, "--ascii") || IsLimitedTerminal();

            new TuiApp(service, service, ascii).Run();
        }

        private static bool HasFlag(string[] args, string flag)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Indica si la terminal es de las que no soportan caracteres de recuadro, para usar ASCII automáticamente.</summary>
        private static bool IsLimitedTerminal()
        {
            string? term = Environment.GetEnvironmentVariable("TERM");
            return term == "linux" || term == "dumb" || term == "vt100";
        }
    }
}
