namespace ElectricGrid.Models
{
    /// <summary>Línea de distribución entre dos subestaciones; si es de un solo sentido, la energía fluye del origen al destino.</summary>
    public sealed class PowerLine
    {
        /// <summary>Crea la descripción de una línea.</summary>
        public PowerLine(string sourceCode, string targetCode, double lengthKm, double maintenanceCost, bool isBidirectional)
        {
            SourceCode = sourceCode;
            TargetCode = targetCode;
            LengthKm = lengthKm;
            MaintenanceCost = maintenanceCost;
            IsBidirectional = isBidirectional;
        }

        /// <summary>Código de la subestación de origen.</summary>
        public string SourceCode { get; }

        /// <summary>Código de la subestación de destino.</summary>
        public string TargetCode { get; }

        /// <summary>Longitud en kilómetros.</summary>
        public double LengthKm { get; }

        /// <summary>Costo de mantenimiento.</summary>
        public double MaintenanceCost { get; }

        /// <summary>Indica si la energía puede fluir en ambos sentidos.</summary>
        public bool IsBidirectional { get; }
    }
}
