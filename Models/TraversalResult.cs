namespace ElectricGrid.Models
{
    public sealed class TraversalResult
    {
        // Guarda el resultado y el orden de visita.
        internal TraversalResult(bool success, string message, Substation[] order)
        {
            Success = success;
            Message = message;
            Order = order;
        }

        public bool Success { get; }

        public string Message { get; }

        public Substation[] Order { get; }
    }
}
