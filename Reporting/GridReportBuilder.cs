using System;
using System.IO;
using System.Text;
using ElectricGrid.Models;
using ElectricGrid.Services;

namespace ElectricGrid.Reporting
{
    // Genera el reporte de texto con el resumen de la red.
    public static class GridReportBuilder
    {
        // Arma el reporte con el resumen, las zonas, las subestaciones y las líneas.
        public static string Build(IPowerGridService service)
        {
            StringBuilder report = new StringBuilder();
            GridSummary summary = service.GetSummary();

            report.AppendLine("REPORTE DE LA RED ELÉCTRICA MUNICIPAL");
            report.AppendLine($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine(new string('=', 60));
            report.AppendLine($"Subestaciones ......... {summary.SubstationCount}");
            report.AppendLine($"Líneas ................ {summary.LineCount}");
            report.AppendLine($"Zonas eléctricas ...... {summary.ZoneCount}");
            report.AppendLine($"Capacidad total ....... {summary.TotalCapacityMw:F1} MW");
            report.AppendLine($"Longitud de líneas .... {summary.TotalLengthKm:F1} km");
            report.AppendLine($"Costo de mantenimiento  {summary.TotalMaintenanceCost:F0}");
            report.AppendLine();

            report.AppendLine("ZONAS ELÉCTRICAS");
            report.AppendLine(new string('-', 60));
            foreach (ElectricalZone zone in service.GetZones())
            {
                report.AppendLine($"Zona {zone.Id}: {zone.Members.Length} subestación(es), {zone.TotalCapacityMw:F1} MW");
                foreach (Substation member in zone.Members)
                {
                    report.AppendLine($"    {member.Code,-10} {member.Name,-28} {member.CapacityMw,8:F1} MW");
                }
            }

            report.AppendLine();
            report.AppendLine("LÍNEAS");
            report.AppendLine(new string('-', 60));
            foreach (PowerLine line in service.GetAllLines())
            {
                string arrow = line.IsBidirectional ? "<->" : " ->";
                report.AppendLine($"    {line.SourceCode,-10} {arrow} {line.TargetCode,-10} {line.LengthKm,6:F1} km   costo {line.MaintenanceCost,8:F0}");
            }

            return report.ToString();
        }

        // Guarda el reporte en disco y devuelve la ruta completa.
        public static string Export(IPowerGridService service, string path)
        {
            string fullPath = Path.GetFullPath(path);
            File.WriteAllText(fullPath, Build(service), Encoding.UTF8);
            return fullPath;
        }
    }
}
