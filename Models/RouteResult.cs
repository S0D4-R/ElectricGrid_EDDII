using ElectricGrid.DataStructures;

namespace ElectricGrid.Models
{
    public enum RouteStatus
    {
        Found,

        SourceNotFound,

        TargetNotFound,

        DifferentZones,

        NoDirectedPath
    }

    public sealed class RouteResult
    {
        // Guarda el estado, el camino y los totales de la ruta.
        internal RouteResult(RouteStatus status, string message, Substation[] path,
                             double totalWeight, double totalLength, double totalCost,
                             EdgeWeightKind criterion)
        {
            Status = status;
            Message = message;
            Path = path;
            TotalWeight = totalWeight;
            TotalLengthKm = totalLength;
            TotalMaintenanceCost = totalCost;
            Criterion = criterion;
        }

        public RouteStatus Status { get; }

        public string Message { get; }

        public Substation[] Path { get; }

        public double TotalWeight { get; }

        public double TotalLengthKm { get; }

        public double TotalMaintenanceCost { get; }

        public EdgeWeightKind Criterion { get; }

        public bool Found => Status == RouteStatus.Found;
    }
}
