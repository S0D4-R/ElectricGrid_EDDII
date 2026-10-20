namespace ElectricGrid.Core.Models
{
    /// <summary>Classification of a proposed new line.</summary>
    public enum LineProposalVerdict
    {
        /// <summary>One or both substations are unknown, or both are the same substation.</summary>
        Invalid,

        /// <summary>The substations are in different zones: the line would merge two zones.</summary>
        JoinsDifferentZones,

        /// <summary>The substations already share a zone: the line only adds a redundant path.</summary>
        AddsRedundancyInSameZone
    }

    /// <summary>Analysis produced before approving a new line (Union-Find check).</summary>
    public sealed class LineProposalResult
    {
        internal LineProposalResult(LineProposalVerdict verdict, string message, bool directLineExists)
        {
            Verdict = verdict;
            Message = message;
            DirectLineExists = directLineExists;
        }

        /// <summary>Classification of the proposal.</summary>
        public LineProposalVerdict Verdict { get; }

        /// <summary>Explanation intended for the operator.</summary>
        public string Message { get; }

        /// <summary>Whether a direct line between the pair already exists (in any direction).</summary>
        public bool DirectLineExists { get; }

        /// <summary>Whether the proposal is technically valid and can be registered.</summary>
        public bool IsValid => Verdict != LineProposalVerdict.Invalid;
    }
}
