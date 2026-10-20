using ElectricGrid.Core.DataStructures;
using ElectricGrid.Core.Models;

namespace ElectricGrid.Core.Services
{
    /// <summary>
    /// Application-level contract of the grid management system. Every front end
    /// (the current console menu or a future GUI) talks to the core exclusively through this
    /// interface, so the data-structure layer never depends on any user interface.
    /// </summary>
    public interface IPowerGridService
    {
        /// <summary>Registers a substation (hash table + graph vertex + Union-Find element + trie entry).</summary>
        OperationResult RegisterSubstation(string code, string name, double capacityMw);

        /// <summary>Removes a substation and every line attached to it.</summary>
        OperationResult RemoveSubstation(string code);

        /// <summary>Finds a substation by code (hash table lookup). Returns <c>null</c> when unknown.</summary>
        Substation? FindSubstation(string code);

        /// <summary>Suggests substations whose name starts with the typed prefix (trie + hash table).</summary>
        Substation[] SuggestSubstations(string prefix, int maxResults);

        /// <summary>
        /// Whether name autocomplete is available. Front ends use it to enable or hide
        /// suggestion widgets. <c>false</c> until the trie is implemented.
        /// </summary>
        bool SupportsAutocomplete { get; }

        /// <summary>Returns every substation in registration order.</summary>
        Substation[] GetAllSubstations();

        /// <summary>Registers a line between two existing substations and merges their zones.</summary>
        OperationResult RegisterLine(PowerLine line);

        /// <summary>Removes a line and rebuilds the electrical zones.</summary>
        OperationResult RemoveLine(string sourceCode, string targetCode, bool bidirectional);

        /// <summary>Returns every line (a two-way line appears once).</summary>
        PowerLine[] GetAllLines();

        /// <summary>Analyses a proposed line using Union-Find before it is approved.</summary>
        LineProposalResult EvaluateProposedLine(string sourceCode, string targetCode);

        /// <summary>
        /// Integrated workflow: hash table lookup, Union-Find connectivity validation and
        /// Dijkstra's algorithm. Throws <see cref="System.NotImplementedException"/> after the
        /// validations while Dijkstra is pending.
        /// </summary>
        RouteResult FindOptimalRoute(string sourceCode, string targetCode, EdgeWeightKind criterion);

        /// <summary>Breadth-first traversal following line direction.</summary>
        TraversalResult TraverseBreadthFirst(string startCode);

        /// <summary>Depth-first traversal following line direction.</summary>
        TraversalResult TraverseDepthFirst(string startCode);

        /// <summary>Checks whether two substations belong to the same electrical zone.</summary>
        ConnectivityResult CheckSameZone(string codeA, string codeB);

        /// <summary>Returns the connected electrical zones.</summary>
        ElectricalZone[] GetZones();

        /// <summary>Returns aggregated network figures.</summary>
        GridSummary GetSummary();

        /// <summary>Removes all data and starts from an empty network.</summary>
        void Clear();
    }
}
