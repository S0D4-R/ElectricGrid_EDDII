using ElectricGrid.DataStructures;

namespace ElectricGrid.Models
{
    /// <summary>Posibles resultados de una solicitud de ruta.</summary>
    public enum RouteStatus
    {
        /// <summary>Se encontró una ruta.</summary>
        Found,

        /// <summary>El origen no está registrado.</summary>
        SourceNotFound,

        /// <summary>El destino no está registrado.</summary>
        TargetNotFound,

        /// <summary>Union-Find indica zonas distintas, por lo que no se ejecutó Dijkstra.</summary>
        DifferentZones,

        /// <summary>Comparten zona, pero el sentido de las líneas impide llegar al destino.</summary>
        NoDirectedPath
    }

    /// <summary>Resultado del flujo integrado de ruta: tabla hash, Union-Find y Dijkstra.</summary>
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

        /// <summary>Resultado de la solicitud.</summary>
        public RouteStatus Status { get; }

        /// <summary>Explicación del resultado.</summary>
        public string Message { get; }

        /// <summary>Subestaciones de la ruta, del origen al destino.</summary>
        public Substation[] Path { get; }

        /// <summary>Valor del criterio optimizado.</summary>
        public double TotalWeight { get; }

        /// <summary>Longitud total de la ruta en kilómetros.</summary>
        public double TotalLengthKm { get; }

        /// <summary>Costo total de mantenimiento de la ruta.</summary>
        public double TotalMaintenanceCost { get; }

        /// <summary>Criterio que se optimizó.</summary>
        public EdgeWeightKind Criterion { get; }

        /// <summary>Indica si se encontró la ruta.</summary>
        public bool Found => Status == RouteStatus.Found;
    }
}
