namespace ElectricGrid.Models
{
    public enum LineProposalVerdict
    {
        Invalid,

        JoinsDifferentZones,

        AddsRedundancyInSameZone
    }

    public sealed class LineProposalResult
    {
        // Guarda la clasificación y el mensaje de la propuesta.
        internal LineProposalResult(LineProposalVerdict verdict, string message, bool directLineExists)
        {
            Verdict = verdict;
            Message = message;
            DirectLineExists = directLineExists;
        }

        public LineProposalVerdict Verdict { get; }

        public string Message { get; }

        public bool DirectLineExists { get; }

        public bool IsValid => Verdict != LineProposalVerdict.Invalid;
    }
}
