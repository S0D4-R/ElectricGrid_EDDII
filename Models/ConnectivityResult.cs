namespace ElectricGrid.Models
{
    /// <summary>Resultado de la consulta de pertenencia a la misma zona eléctrica.</summary>
    public sealed class ConnectivityResult
    {
        internal ConnectivityResult(bool bothExist, bool connected, string message)
        {
            BothExist = bothExist;
            Connected = connected;
            Message = message;
        }

        /// <summary>Indica si ambos códigos están registrados.</summary>
        public bool BothExist { get; }

        /// <summary>Indica si ambas subestaciones pertenecen a la misma zona.</summary>
        public bool Connected { get; }

        /// <summary>Explicación para el operador.</summary>
        public string Message { get; }
    }
}
