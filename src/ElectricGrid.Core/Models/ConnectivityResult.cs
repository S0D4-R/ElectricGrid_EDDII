namespace ElectricGrid.Core.Models
{
    /// <summary>Result of a "same electrical zone" query.</summary>
    public sealed class ConnectivityResult
    {
        internal ConnectivityResult(bool bothExist, bool connected, string message)
        {
            BothExist = bothExist;
            Connected = connected;
            Message = message;
        }

        /// <summary>Whether both codes are registered.</summary>
        public bool BothExist { get; }

        /// <summary>Whether both substations belong to the same zone.</summary>
        public bool Connected { get; }

        /// <summary>Explanation for the operator.</summary>
        public string Message { get; }
    }
}
