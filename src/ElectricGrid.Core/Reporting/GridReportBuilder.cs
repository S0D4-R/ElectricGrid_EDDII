using System;
using System.IO;
using System.Text;
using ElectricGrid.Core.Models;
using ElectricGrid.Core.Services;

namespace ElectricGrid.Core.Reporting
{
    /// <summary>
    /// Produces a plain-text management report (summary, zones, substations and lines)
    /// that can be shown on screen, exported to a file or embedded in a GUI.
    /// </summary>
    public static class GridReportBuilder
    {
        /// <summary>Builds the report text.</summary>
        public static string Build(IPowerGridService service)
        {
            StringBuilder report = new StringBuilder();
            GridSummary summary = service.GetSummary();

            report.AppendLine("MUNICIPAL ELECTRICAL GRID REPORT");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine(new string('=', 60));
            report.AppendLine($"Substations ........ {summary.SubstationCount}");
            report.AppendLine($"Lines .............. {summary.LineCount}");
            report.AppendLine($"Electrical zones ... {summary.ZoneCount}");
            report.AppendLine($"Total capacity ..... {summary.TotalCapacityMw:F1} MW");
            report.AppendLine($"Total line length .. {summary.TotalLengthKm:F1} km");
            report.AppendLine($"Maintenance cost ... {summary.TotalMaintenanceCost:F0}");
            report.AppendLine();

            report.AppendLine("ELECTRICAL ZONES");
            report.AppendLine(new string('-', 60));
            foreach (ElectricalZone zone in service.GetZones())
            {
                report.AppendLine($"Zone {zone.Id}: {zone.Members.Length} substation(s), {zone.TotalCapacityMw:F1} MW");
                foreach (Substation member in zone.Members)
                {
                    report.AppendLine($"    {member.Code,-10} {member.Name,-28} {member.CapacityMw,8:F1} MW");
                }
            }

            report.AppendLine();
            report.AppendLine("LINES");
            report.AppendLine(new string('-', 60));
            foreach (PowerLine line in service.GetAllLines())
            {
                string arrow = line.IsBidirectional ? "<->" : " ->";
                report.AppendLine($"    {line.SourceCode,-10} {arrow} {line.TargetCode,-10} {line.LengthKm,6:F1} km   cost {line.MaintenanceCost,8:F0}");
            }

            return report.ToString();
        }

        /// <summary>Builds the report and writes it to a file.</summary>
        /// <returns>The full path of the written file.</returns>
        public static string Export(IPowerGridService service, string path)
        {
            string fullPath = Path.GetFullPath(path);
            File.WriteAllText(fullPath, Build(service), Encoding.UTF8);
            return fullPath;
        }
    }
}
