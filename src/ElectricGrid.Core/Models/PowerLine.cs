namespace ElectricGrid.Core.Models
{
    /// <summary>
    /// Distribution line between two substations. A one-way line carries power only from
    /// <see cref="SourceCode"/> to <see cref="TargetCode"/> (e.g. main to secondary substation).
    /// </summary>
    public sealed class PowerLine
    {
        /// <summary>Creates a line description.</summary>
        public PowerLine(string sourceCode, string targetCode, double lengthKm, double maintenanceCost, bool isBidirectional)
        {
            SourceCode = sourceCode;
            TargetCode = targetCode;
            LengthKm = lengthKm;
            MaintenanceCost = maintenanceCost;
            IsBidirectional = isBidirectional;
        }

        /// <summary>Code of the origin substation.</summary>
        public string SourceCode { get; }

        /// <summary>Code of the destination substation.</summary>
        public string TargetCode { get; }

        /// <summary>Length in kilometres.</summary>
        public double LengthKm { get; }

        /// <summary>Maintenance cost (currency units).</summary>
        public double MaintenanceCost { get; }

        /// <summary><c>true</c> when power can flow in both directions.</summary>
        public bool IsBidirectional { get; }
    }
}
