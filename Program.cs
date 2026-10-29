using System;
using System.Text;
using ElectricGrid.Tui;
using ElectricGrid.Services;

namespace ElectricGrid
{
    // Punto de entrada de la aplicación.
    internal static class Program
    {
        // Inicia el servicio y arranca la interfaz de texto.
        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            PowerGridService service = new PowerGridService();
            bool ascii = HasFlag(args, "--ascii") || IsLimitedTerminal();

            new TuiApp(service, service, ascii).Run();
        }

        // Comprueba si el argumento indicado está presente.
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

        // Detecta terminales que no muestran bien los caracteres de recuadro.
        private static bool IsLimitedTerminal()
        {
            string? term = Environment.GetEnvironmentVariable("TERM");
            return term == "linux" || term == "dumb" || term == "vt100";
        }
    }
}
