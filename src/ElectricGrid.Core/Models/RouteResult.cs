using ElectricGrid.Core.DataStructures;

namespace ElectricGrid.Core.Models
{
    /// <summary>Possible outcomes of a route request.</summary>
    public enum RouteStatus
    {
        /// <summary>A route was found.</summary>
        Found,

        /// <summary>The origin code is not registered.</summary>
        SourceNotFound,

        /// <summary>The destination code is not registered.</summary>
        TargetNotFound,

        /// <summary>Union-Find reports different electrical zones: Dijkstra was skipped.</summary>
        DifferentZones,

        /// <summary>Same zone, but edge directions prevent reaching the destination.</summary>
        NoDirectedPath
    }

    /// <summary>Result of the integrated route-finding workflow (Hash Table + Union-Find + Dijkstra).</summary>
    public sealed class RouteResult
    {
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

        /// <summary>Outcome of the request.</summary>
        public RouteStatus Status { get; }

        /// <summary>Explanation of the outcome.</summary>
        public string Message { get; }

        /// <summary>Substations visited from origin to destination (empty if no route).</summary>
        public Substation[] Path { get; }

        /// <summary>Value of the optimized criterion.</summary>
        public double TotalWeight { get; }

        /// <summary>Total length of the route in km.</summary>
        public double TotalLengthKm { get; }

        /// <summary>Total maintenance cost of the route.</summary>
        public double TotalMaintenanceCost { get; }

        /// <summary>Criterion that was optimized.</summary>
        public EdgeWeightKind Criterion { get; }

        /// <summary>Shortcut for <c>Status == RouteStatus.Found</c>.</summary>
        public bool Found => Status == RouteStatus.Found;
    }
}
