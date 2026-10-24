namespace ElectricGrid.Models
{
    /// <summary>Cifras agregadas de la red para resúmenes y reportes.</summary>
    public sealed class GridSummary
    {
        internal GridSummary(int substationCount, int lineCount, int zoneCount, double totalCapacityMw,
                             double totalLengthKm, double totalMaintenanceCost)
        {
            SubstationCount = substationCount;
            LineCount = lineCount;
            ZoneCount = zoneCount;
            TotalCapacityMw = totalCapacityMw;
            TotalLengthKm = totalLengthKm;
            TotalMaintenanceCost = totalMaintenanceCost;
        }

        /// <summary>Cantidad de subestaciones registradas.</summary>
        public int SubstationCount { get; }

        /// <summary>Cantidad de líneas; una línea de doble sentido cuenta una vez.</summary>
        public int LineCount { get; }

        /// <summary>Cantidad de zonas eléctricas independientes.</summary>
        public int ZoneCount { get; }

        /// <summary>Capacidad instalada total en MW.</summary>
        public double TotalCapacityMw { get; }

        /// <summary>Longitud total de las líneas en kilómetros.</summary>
        public double TotalLengthKm { get; }

        /// <summary>Costo total de mantenimiento de las líneas.</summary>
        public double TotalMaintenanceCost { get; }
    }
}
