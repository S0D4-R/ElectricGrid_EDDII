namespace ElectricGrid.Models
{
    /// <summary>Clasificación de una línea propuesta.</summary>
    public enum LineProposalVerdict
    {
        /// <summary>Alguna subestación no existe o ambas son la misma.</summary>
        Invalid,

        /// <summary>Las subestaciones están en zonas distintas: la línea las uniría.</summary>
        JoinsDifferentZones,

        /// <summary>Las subestaciones ya comparten zona: la línea solo agrega redundancia.</summary>
        AddsRedundancyInSameZone
    }

    /// <summary>Análisis de Union-Find previo a la aprobación de una línea nueva.</summary>
    public sealed class LineProposalResult
    {
        internal LineProposalResult(LineProposalVerdict verdict, string message, bool directLineExists)
        {
            Verdict = verdict;
            Message = message;
            DirectLineExists = directLineExists;
        }

        /// <summary>Clasificación de la propuesta.</summary>
        public LineProposalVerdict Verdict { get; }

        /// <summary>Explicación para el operador.</summary>
        public string Message { get; }

        /// <summary>Indica si ya existe una línea directa entre el par, en cualquier sentido.</summary>
        public bool DirectLineExists { get; }

        /// <summary>Indica si la propuesta es técnicamente válida.</summary>
        public bool IsValid => Verdict != LineProposalVerdict.Invalid;
    }
}
