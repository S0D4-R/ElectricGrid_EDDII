using System;
using System.Text;
using ElectricGrid.Core.DataStructures;
using ElectricGrid.Core.Models;

namespace ElectricGrid.ConsoleApp.UI
{
    /// <summary>
    /// Formats Core results for the console. Keeping all presentation code here means the
    /// Core library can be consumed by any other front end without changes.
    /// </summary>
    internal static class ConsolePresenter
    {
        public static void Title(string text)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"=== {text} ===");
            Console.ResetColor();
        }

        public static void Success(string message) => WriteColored(message, ConsoleColor.Green);

        public static void Warning(string message) => WriteColored(message, ConsoleColor.Yellow);

        public static void Error(string message) => WriteColored(message, ConsoleColor.Red);

        public static void Info(string message) => WriteColored(message, ConsoleColor.Gray);

        public static void Result(OperationResult result)
        {
            if (result.Success)
            {
                Success(result.Message);
            }
            else
            {
                Error(result.Message);
            }
        }

        public static void Substations(Substation[] substations)
        {
            if (substations.Length == 0)
            {
                Info("(no substations)");
                return;
            }

            Console.WriteLine($"{"CODE",-10} {"NAME",-30} {"CAPACITY (MW)",13}");
            Console.WriteLine(new string('-', 55));
            foreach (Substation s in substations)
            {
                Console.WriteLine($"{s.Code,-10} {s.Name,-30} {s.CapacityMw,13:F1}");
            }

            Console.WriteLine($"Total: {substations.Length}");
        }

        public static void Lines(PowerLine[] lines)
        {
            if (lines.Length == 0)
            {
                Info("(no lines)");
                return;
            }

            Console.WriteLine($"{"FROM",-10} {"DIR",-4} {"TO",-10} {"LENGTH (km)",11} {"MAINT. COST",12}");
            Console.WriteLine(new string('-', 52));
            foreach (PowerLine line in lines)
            {
                string arrow = line.IsBidirectional ? "<->" : "->";
                Console.WriteLine($"{line.SourceCode,-10} {arrow,-4} {line.TargetCode,-10} {line.LengthKm,11:F1} {line.MaintenanceCost,12:F0}");
            }

            Console.WriteLine($"Total: {lines.Length}");
        }

        public static void Route(RouteResult route)
        {
            if (!route.Found)
            {
                Warning(route.Message);
                return;
            }

            string criterion = route.Criterion == EdgeWeightKind.Length ? "shortest length" : "lowest maintenance cost";
            Success($"Optimal route ({criterion}):");

            StringBuilder path = new StringBuilder();
            for (int i = 0; i < route.Path.Length; i++)
            {
                if (i > 0)
                {
                    path.Append("  ->  ");
                }

                path.Append($"{route.Path[i].Code} ({route.Path[i].Name})");
            }

            Console.WriteLine($"  {path}");
            Console.WriteLine($"  Hops: {route.Path.Length - 1} | Total length: {route.TotalLengthKm:F1} km | Total maintenance cost: {route.TotalMaintenanceCost:F0}");
        }

        public static void Traversal(string title, TraversalResult traversal)
        {
            if (!traversal.Success)
            {
                Error(traversal.Message);
                return;
            }

            Success($"{title}: {traversal.Order.Length} substation(s) reached");
            StringBuilder order = new StringBuilder();
            for (int i = 0; i < traversal.Order.Length; i++)
            {
                if (i > 0)
                {
                    order.Append(" -> ");
                }

                order.Append(traversal.Order[i].Code);
            }

            Console.WriteLine($"  {order}");
        }

        public static void Zones(ElectricalZone[] zones)
        {
            if (zones.Length == 0)
            {
                Info("(no zones)");
                return;
            }

            foreach (ElectricalZone zone in zones)
            {
                Console.WriteLine($"Zone {zone.Id}: {zone.Members.Length} substation(s), {zone.TotalCapacityMw:F1} MW");
                foreach (Substation member in zone.Members)
                {
                    Console.WriteLine($"    {member.Code,-10} {member.Name}");
                }
            }
        }

        public static void Summary(GridSummary summary)
        {
            Console.WriteLine($"Substations ........ {summary.SubstationCount}");
            Console.WriteLine($"Lines .............. {summary.LineCount}");
            Console.WriteLine($"Electrical zones ... {summary.ZoneCount}");
            Console.WriteLine($"Total capacity ..... {summary.TotalCapacityMw:F1} MW");
            Console.WriteLine($"Total line length .. {summary.TotalLengthKm:F1} km");
            Console.WriteLine($"Maintenance cost ... {summary.TotalMaintenanceCost:F0}");
        }

        public static void LoadReport(LoadReport report)
        {
            bool loadedSomething = report.SubstationsLoaded + report.LinesLoaded > 0;
            if (report.FileFound || loadedSomething)
            {
                Success($"Loaded {report.SubstationsLoaded} substation(s) and {report.LinesLoaded} line(s). Rows skipped: {report.RowsSkipped}.");
            }

            foreach (string error in report.Errors)
            {
                if (report.FileFound)
                {
                    Warning($"  {error}");
                }
                else
                {
                    Error($"  {error}");
                }
            }
        }

        private static void WriteColored(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
