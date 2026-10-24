namespace ElectricGrid.Models
{
    /// <summary>Resultado de una operación de comando, con indicador de éxito y mensaje para el usuario.</summary>
    public sealed class OperationResult
    {
        private OperationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        /// <summary>Indica si la operación tuvo éxito.</summary>
        public bool Success { get; }

        /// <summary>Mensaje descriptivo del resultado.</summary>
        public string Message { get; }

        /// <summary>Crea un resultado exitoso.</summary>
        public static OperationResult Ok(string message) => new OperationResult(true, message);

        /// <summary>Crea un resultado fallido.</summary>
        public static OperationResult Fail(string message) => new OperationResult(false, message);
    }
}
