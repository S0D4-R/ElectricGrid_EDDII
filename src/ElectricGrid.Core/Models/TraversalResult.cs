namespace ElectricGrid.Core.Models
{
    /// <summary>Result of a BFS or DFS traversal.</summary>
    public sealed class TraversalResult
    {
        internal TraversalResult(bool success, string message, Substation[] order)
        {
            Success = success;
            Message = message;
            Order = order;
        }

        /// <summary>Whether the start substation existed.</summary>
        public bool Success { get; }

        /// <summary>Explanation (mainly useful on failure).</summary>
        public string Message { get; }

        /// <summary>Substations in visiting order.</summary>
        public Substation[] Order { get; }
    }
}
