using System;
using System.Text;
using ElectricGrid.ConsoleApp.Tui;
using ElectricGrid.ConsoleApp.UI;
using ElectricGrid.Core.IO;
using ElectricGrid.Core.Services;

namespace ElectricGrid.ConsoleApp
{
    /// <summary>
    /// Single entry point (composition root). It creates the Core service and starts one of the two
    /// front ends over it:
    /// <list type="bullet">
    /// <item><description>default: the classic numbered console menu;</description></item>
    /// <item><description><c>--tui</c>: the full-screen text interface with the drawn graph (<c>--ascii</c> for plain characters).</description></item>
    /// </list>
    /// A future GUI project can replace this one and reuse <c>ElectricGrid.Core</c> untouched.
    /// </summary>
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (HasFlag(args, "--help") || HasFlag(args, "-h"))
            {
                PrintUsage();
                return;
            }

            Console.OutputEncoding = Encoding.UTF8;

            PowerGridService service = new PowerGridService();
            bool ascii = HasFlag(args, "--ascii") || IsLimitedTerminal();

            if (HasFlag(args, "--tui-snapshot"))
            {
                PrintSnapshot(service, ascii, args);
                return;
            }

            if (HasFlag(args, "--tui") || HasFlag(args, "-t"))
            {
                new TuiApp(service, service, ascii).Run();
                return;
            }

            Console.Title = "Municipal Electrical Grid Management";
            new MainMenu(service, service).Run();
        }

        /// <summary>
        /// Developer aid: loads the sample data and prints one TUI frame as plain text
        /// (<c>--tui-snapshot --size=100x30</c>). Useful for documentation and for testing without a terminal.
        /// </summary>
        private static void PrintSnapshot(PowerGridService service, bool ascii, string[] args)
        {
            new GridDataLoader(service).LoadAll("substations.csv", "lines.csv");

            int width = 100, height = 30;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--size=", StringComparison.Ordinal))
                {
                    string[] parts = args[i].Substring(7).Split('x');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                    {
                        width = w;
                        height = h;
                    }
                }
            }

            Console.Write(new TuiApp(service, service, ascii).RenderSnapshot(width, height));
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

        /// <summary>Terminals known to lack box-drawing characters get the ASCII drawing automatically.</summary>
        private static bool IsLimitedTerminal()
        {
            string? term = Environment.GetEnvironmentVariable("TERM");
            return term == "linux" || term == "dumb" || term == "vt100";
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Municipal Electrical Grid Management");
            Console.WriteLine();
            Console.WriteLine("  dotnet run --project src/ElectricGrid.ConsoleApp               classic numbered menu");
            Console.WriteLine("  dotnet run --project src/ElectricGrid.ConsoleApp -- --tui      full-screen interface with graph drawing");
            Console.WriteLine("  ... -- --tui --ascii                                            plain ASCII characters (no box drawing)");
        }
    }
}
