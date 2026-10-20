namespace ElectricGrid.Core.Models
{
    /// <summary>Aggregated figures of the network, useful for dashboards and reports.</summary>
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

        /// <summary>Number of registered substations.</summary>
        public int SubstationCount { get; }

        /// <summary>Number of lines (a two-way line counts once).</summary>
        public int LineCount { get; }

        /// <summary>Number of independent electrical zones.</summary>
        public int ZoneCount { get; }

        /// <summary>Total installed capacity (MW).</summary>
        public double TotalCapacityMw { get; }

        /// <summary>Total length of all lines (km).</summary>
        public double TotalLengthKm { get; }

        /// <summary>Total maintenance cost of all lines.</summary>
        public double TotalMaintenanceCost { get; }
    }
}
