namespace ElectricGrid.Models
{
    public sealed class ConnectivityResult
    {
        // Guarda el resultado de comparar dos subestaciones.
        internal ConnectivityResult(bool bothExist, bool connected, string message)
        {
            BothExist = bothExist;
            Connected = connected;
            Message = message;
        }

        public bool BothExist { get; }

        public bool Connected { get; }

        public string Message { get; }
    }
}
