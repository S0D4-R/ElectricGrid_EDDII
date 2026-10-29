namespace ElectricGrid.Models
{
    public sealed class GridSummary
    {
        // Guarda las cifras calculadas de la red.
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

        public int SubstationCount { get; }

        public int LineCount { get; }

        public int ZoneCount { get; }

        public double TotalCapacityMw { get; }

        public double TotalLengthKm { get; }

        public double TotalMaintenanceCost { get; }
    }
}
