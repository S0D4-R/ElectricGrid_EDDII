namespace ElectricGrid.Core.Models
{
    /// <summary>
    /// Outcome of a command-style operation. Lets any front end (console, GUI, web)
    /// decide how to present success or failure without parsing exceptions.
    /// </summary>
    public sealed class OperationResult
    {
        private OperationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        /// <summary>Whether the operation succeeded.</summary>
        public bool Success { get; }

        /// <summary>Human readable explanation.</summary>
        public string Message { get; }

        /// <summary>Creates a successful result.</summary>
        public static OperationResult Ok(string message) => new OperationResult(true, message);

        /// <summary>Creates a failed result.</summary>
        public static OperationResult Fail(string message) => new OperationResult(false, message);
    }
}
