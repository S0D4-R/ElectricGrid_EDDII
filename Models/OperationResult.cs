namespace ElectricGrid.Models
{
    public sealed class OperationResult
    {
        // Crea el resultado con su estado y mensaje.
        private OperationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; }

        public string Message { get; }

        // Devuelve un resultado exitoso.
        public static OperationResult Ok(string message) => new OperationResult(true, message);

        // Devuelve un resultado fallido.
        public static OperationResult Fail(string message) => new OperationResult(false, message);
    }
}
