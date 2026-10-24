namespace ElectricGrid.Models
{
    /// <summary>Resultado de un recorrido BFS o DFS.</summary>
    public sealed class TraversalResult
    {
        internal TraversalResult(bool success, string message, Substation[] order)
        {
            Success = success;
            Message = message;
            Order = order;
        }

        /// <summary>Indica si la subestación inicial existía.</summary>
        public bool Success { get; }

        /// <summary>Mensaje descriptivo del resultado.</summary>
        public string Message { get; }

        /// <summary>Subestaciones en orden de visita.</summary>
        public Substation[] Order { get; }
    }
}
