using ElectricGrid.DataStructures;
using ElectricGrid.Models;

namespace ElectricGrid.Services
{
    // Operaciones de la red que usa cualquier interfaz de usuario.
    public interface IPowerGridService
    {
        // Registra una subestación en la tabla hash, el grafo, Union-Find y el Trie.
        OperationResult RegisterSubstation(string code, string name, double capacityMw);

        // Elimina la subestación y todas sus líneas.
        OperationResult RemoveSubstation(string code);

        // Busca una subestación por su código.
        Substation? FindSubstation(string code);

        // Sugiere subestaciones cuyo nombre empieza con el prefijo.
        Substation[] SuggestSubstations(string prefix, int maxResults);

        // Busca subestaciones por texto en el nombre o en el código.
        Substation[] SearchSubstations(string text, int maxResults);

        // Indica si está disponible el autocompletado por nombre.
        bool SupportsAutocomplete { get; }

        // Devuelve todas las subestaciones registradas.
        Substation[] GetAllSubstations();

        // Registra una línea entre dos subestaciones existentes.
        OperationResult RegisterLine(PowerLine line);

        // Elimina una línea y recalcula las zonas.
        OperationResult RemoveLine(string sourceCode, string targetCode, bool bidirectional);

        // Devuelve todas las líneas sin duplicar las bidireccionales.
        PowerLine[] GetAllLines();

        // Analiza si una línea nueva uniría zonas o sería redundante.
        LineProposalResult EvaluateProposedLine(string sourceCode, string targetCode);

        // Calcula la ruta óptima entre dos subestaciones.
        RouteResult FindOptimalRoute(string sourceCode, string targetCode, EdgeWeightKind criterion);

        // Recorre la red en anchura desde una subestación.
        TraversalResult TraverseBreadthFirst(string startCode);

        // Recorre la red en profundidad desde una subestación.
        TraversalResult TraverseDepthFirst(string startCode);

        // Indica si dos subestaciones están en la misma zona.
        ConnectivityResult CheckSameZone(string codeA, string codeB);

        // Devuelve las zonas eléctricas conectadas.
        ElectricalZone[] GetZones();

        // Devuelve las cifras agregadas de la red.
        GridSummary GetSummary();

        // Borra todos los datos de la red.
        void Clear();
    }
}
