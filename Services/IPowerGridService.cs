using ElectricGrid.DataStructures;
using ElectricGrid.Models;

namespace ElectricGrid.Services
{
    /// <summary>
    /// Contrato de las operaciones de la red eléctrica. Cualquier interfaz de usuario
    /// (la TUI actual o una GUI futura) se comunica con el núcleo únicamente a través de él.
    /// </summary>
    public interface IPowerGridService
    {
        /// <summary>Registra una subestación en la tabla hash, el grafo, Union-Find y el Trie.</summary>
        OperationResult RegisterSubstation(string code, string name, double capacityMw);

        /// <summary>Elimina una subestación y todas las líneas conectadas a ella.</summary>
        OperationResult RemoveSubstation(string code);

        /// <summary>Busca una subestación por código en la tabla hash; devuelve nulo si no existe.</summary>
        Substation? FindSubstation(string code);

        /// <summary>Sugiere subestaciones cuyo nombre empieza con el prefijo, usando el Trie y la tabla hash.</summary>
        Substation[] SuggestSubstations(string prefix, int maxResults);
        

        /// <summary>
        /// Busca subestaciones por texto. Usa el Trie cuando está disponible y, mientras tanto,
        /// una búsqueda lineal por coincidencia parcial en el código o el nombre.
        /// </summary>
        Substation[] SearchSubstations(string text, int maxResults);

        /// <summary>Indica si el autocompletado por nombre está disponible.</summary>
        bool SupportsAutocomplete { get; }

        /// <summary>Devuelve todas las subestaciones en orden de registro.</summary>
        Substation[] GetAllSubstations();

        /// <summary>Registra una línea entre dos subestaciones existentes y unifica sus zonas.</summary>
        OperationResult RegisterLine(PowerLine line);

        /// <summary>Elimina una línea y reconstruye las zonas eléctricas.</summary>
        OperationResult RemoveLine(string sourceCode, string targetCode, bool bidirectional);

        /// <summary>Devuelve todas las líneas; una de doble sentido aparece una sola vez.</summary>
        PowerLine[] GetAllLines();

        /// <summary>Analiza con Union-Find una línea propuesta antes de aprobarla.</summary>
        LineProposalResult EvaluateProposedLine(string sourceCode, string targetCode);

        /// <summary>Calcula la ruta óptima: resuelve códigos con la tabla hash, valida la zona con Union-Find y aplica Dijkstra.</summary>
        RouteResult FindOptimalRoute(string sourceCode, string targetCode, EdgeWeightKind criterion);

        /// <summary>Recorrido en anchura desde una subestación, siguiendo el sentido de las líneas.</summary>
        TraversalResult TraverseBreadthFirst(string startCode);

        /// <summary>Recorrido en profundidad desde una subestación, siguiendo el sentido de las líneas.</summary>
        TraversalResult TraverseDepthFirst(string startCode);

        /// <summary>Indica si dos subestaciones pertenecen a la misma zona eléctrica.</summary>
        ConnectivityResult CheckSameZone(string codeA, string codeB);

        /// <summary>Devuelve las zonas eléctricas conectadas.</summary>
        ElectricalZone[] GetZones();

        /// <summary>Devuelve las cifras agregadas de la red.</summary>
        GridSummary GetSummary();

        /// <summary>Elimina todos los datos y deja la red vacía.</summary>
        void Clear();
    }
}
