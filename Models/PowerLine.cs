namespace ElectricGrid.Models
{
    public sealed class PowerLine
    {
        // Guarda los extremos y los datos de la línea.
        public PowerLine(string sourceCode, string targetCode, double lengthKm, double maintenanceCost, bool isBidirectional)
        {
            SourceCode = sourceCode;
            TargetCode = targetCode;
            LengthKm = lengthKm;
            MaintenanceCost = maintenanceCost;
            IsBidirectional = isBidirectional;
        }

        public string SourceCode { get; }

        public string TargetCode { get; }

        public double LengthKm { get; }

        public double MaintenanceCost { get; }

        public bool IsBidirectional { get; }
    }
}
