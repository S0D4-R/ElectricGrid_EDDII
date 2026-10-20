using System;
using System.Text;
using ElectricGrid.Core.Collections;
using ElectricGrid.Core.DataStructures;
using ElectricGrid.Core.Models;

namespace ElectricGrid.Core.Services
{
    /// <summary>
    /// Facade that coordinates the four mandatory data structures so they work as one flow:
    /// <list type="number">
    /// <item><description><b>Trie</b> suggests substation names while the operator types (PENDING: see <c>TODO [TRIE</c> markers).</description></item>
    /// <item><description><b>Hash table</b> resolves a code into the complete substation record.</description></item>
    /// <item><description><b>Union-Find</b> validates connectivity (electrical zones) before any heavy work.</description></item>
    /// <item><description><b>Graph</b> computes routes (Dijkstra, PENDING) and traversals (BFS / DFS).</description></item>
    /// </list>
    /// The class contains no console or UI code, which makes it directly reusable by a GUI.
    /// </summary>
    /// <remarks>
    /// Integration layer, written jointly by the whole team.
    /// Consistency rule: vertex index in the graph == element index in the Union-Find ==
    /// position in <see cref="_substationByVertex"/>.
    /// </remarks>
    public sealed class PowerGridService : IPowerGridService, IStructureInspector
    {
        private const int DefaultSuggestionLimit = 10;

        private HashTable<Substation> _registry = new HashTable<Substation>();
        private Graph _graph = new Graph();
        private DisjointSet _zones = new DisjointSet();
        // TODO [TRIE]: uncomment once ElectricGrid.Core/DataStructures/Trie.cs is implemented.
        // private Trie _nameIndex = new Trie();
        private ArrayBuffer<Substation?> _substationByVertex = new ArrayBuffer<Substation?>();

        // ------------------------------------------------------------------ Substations

        /// <inheritdoc />
        public OperationResult RegisterSubstation(string code, string name, double capacityMw)
        {
            string normalizedCode = NormalizeCode(code);
            string cleanName = name?.Trim() ?? string.Empty;

            if (normalizedCode.Length == 0)
            {
                return OperationResult.Fail("The substation code cannot be empty.");
            }

            if (cleanName.Length == 0)
            {
                return OperationResult.Fail("The substation name cannot be empty.");
            }

            if (capacityMw <= 0 || double.IsNaN(capacityMw) || double.IsInfinity(capacityMw))
            {
                return OperationResult.Fail("The capacity must be a positive number of MW.");
            }

            if (_registry.ContainsKey(normalizedCode))
            {
                return OperationResult.Fail($"A substation with code '{normalizedCode}' already exists.");
            }

            int vertex = _graph.AddVertex();
            int element = _zones.MakeSet();
            if (vertex != element || vertex != _substationByVertex.Count)
            {
                throw new InvalidOperationException("Internal indices of the graph and Union-Find are out of sync.");
            }

            Substation substation = new Substation(normalizedCode, cleanName, capacityMw, vertex);
            _registry.Insert(normalizedCode, substation);
            // TODO [TRIE]: _nameIndex.Insert(cleanName, normalizedCode);
            _substationByVertex.Add(substation);

            return OperationResult.Ok($"Substation '{normalizedCode}' ({cleanName}) registered.");
        }

        /// <inheritdoc />
        public OperationResult RemoveSubstation(string code)
        {
            Substation? substation = FindSubstation(code);
            if (substation == null)
            {
                return OperationResult.Fail($"Substation '{NormalizeCode(code)}' was not found.");
            }

            // TODO [TRIE]: _nameIndex.Remove(substation.Name, substation.Code);
            _registry.Remove(substation.Code);
            _graph.RemoveVertex(substation.VertexIndex);
            _substationByVertex[substation.VertexIndex] = null;
            RebuildZones();

            return OperationResult.Ok($"Substation '{substation.Code}' and its lines were removed.");
        }

        /// <inheritdoc />
        public Substation? FindSubstation(string code)
        {
            string normalized = NormalizeCode(code);
            if (normalized.Length == 0)
            {
                return null;
            }

            return _registry.TryGetValue(normalized, out Substation substation) ? substation : null;
        }

        /// <inheritdoc />
        /// <remarks>
        /// PENDING: returns no suggestions until the trie is implemented.
        /// TODO [TRIE]: replace the body with the two-step flow below and flip
        /// <see cref="SupportsAutocomplete"/> to <c>true</c>.
        /// </remarks>
        public Substation[] SuggestSubstations(string prefix, int maxResults)
        {
            // int limit = maxResults <= 0 ? DefaultSuggestionLimit : maxResults;
            //
            // // Step 1: the trie finds the names (and codes) that start with the prefix.
            // TrieMatch[] matches = _nameIndex.SearchByPrefix(prefix, limit);
            // ArrayBuffer<Substation> suggestions = new ArrayBuffer<Substation>(matches.Length + 1);
            //
            // // Step 2: the hash table turns each code into the complete record.
            // for (int i = 0; i < matches.Length; i++)
            // {
            //     if (_registry.TryGetValue(matches[i].Payload, out Substation substation))
            //     {
            //         suggestions.Add(substation);
            //     }
            // }
            //
            // return suggestions.ToArray();

            return new Substation[0];
        }

        /// <inheritdoc />
        /// <remarks>TODO [TRIE]: change to <c>true</c> when <see cref="SuggestSubstations"/> is implemented.</remarks>
        public bool SupportsAutocomplete => false;

        /// <inheritdoc />
        public Substation[] GetAllSubstations()
        {
            ArrayBuffer<Substation> result = new ArrayBuffer<Substation>(_registry.Count + 1);
            for (int i = 0; i < _substationByVertex.Count; i++)
            {
                Substation? substation = _substationByVertex[i];
                if (substation != null)
                {
                    result.Add(substation);
                }
            }

            return result.ToArray();
        }

        // ------------------------------------------------------------------ Lines

        /// <inheritdoc />
        public OperationResult RegisterLine(PowerLine line)
        {
            Substation? source = FindSubstation(line.SourceCode);
            Substation? target = FindSubstation(line.TargetCode);

            if (source == null)
            {
                return OperationResult.Fail($"Origin substation '{NormalizeCode(line.SourceCode)}' does not exist.");
            }

            if (target == null)
            {
                return OperationResult.Fail($"Destination substation '{NormalizeCode(line.TargetCode)}' does not exist.");
            }

            if (source.Code == target.Code)
            {
                return OperationResult.Fail("A line must connect two different substations.");
            }

            if (line.LengthKm <= 0 || line.MaintenanceCost < 0)
            {
                return OperationResult.Fail("The length must be positive and the maintenance cost cannot be negative.");
            }

            bool added = _graph.AddEdge(source.VertexIndex, target.VertexIndex,
                                        line.LengthKm, line.MaintenanceCost, line.IsBidirectional);
            if (!added)
            {
                return OperationResult.Fail($"A line {source.Code} -> {target.Code} already exists.");
            }

            _zones.Union(source.VertexIndex, target.VertexIndex);

            string direction = line.IsBidirectional ? "<->" : "->";
            return OperationResult.Ok($"Line {source.Code} {direction} {target.Code} registered.");
        }

        /// <inheritdoc />
        public OperationResult RemoveLine(string sourceCode, string targetCode, bool bidirectional)
        {
            Substation? source = FindSubstation(sourceCode);
            Substation? target = FindSubstation(targetCode);

            if (source == null || target == null)
            {
                return OperationResult.Fail("Both substations must exist.");
            }

            if (!_graph.RemoveEdge(source.VertexIndex, target.VertexIndex, bidirectional))
            {
                return OperationResult.Fail($"No line {source.Code} -> {target.Code} was found.");
            }

            RebuildZones();
            return OperationResult.Ok($"Line between {source.Code} and {target.Code} removed.");
        }

        /// <inheritdoc />
        public PowerLine[] GetAllLines()
        {
            ArrayBuffer<PowerLine> lines = new ArrayBuffer<PowerLine>();

            _graph.ForEachEdge((from, edge) =>
            {
                GraphEdge? reverse = _graph.FindEdge(edge.Target, from);
                bool mirrored = reverse != null && reverse.Length == edge.Length && reverse.Cost == edge.Cost;

                // A mirrored pair is reported once (from the lower vertex index).
                if (mirrored && from > edge.Target)
                {
                    return;
                }

                Substation? source = _substationByVertex[from];
                Substation? target = _substationByVertex[edge.Target];
                if (source != null && target != null)
                {
                    lines.Add(new PowerLine(source.Code, target.Code, edge.Length, edge.Cost, mirrored));
                }
            });

            return lines.ToArray();
        }

        /// <inheritdoc />
        public LineProposalResult EvaluateProposedLine(string sourceCode, string targetCode)
        {
            Substation? source = FindSubstation(sourceCode);
            Substation? target = FindSubstation(targetCode);

            if (source == null || target == null)
            {
                return new LineProposalResult(LineProposalVerdict.Invalid,
                    "Both substations must exist before a line can be proposed.", false);
            }

            if (source.Code == target.Code)
            {
                return new LineProposalResult(LineProposalVerdict.Invalid,
                    "A line must connect two different substations.", false);
            }

            bool directExists = _graph.FindEdge(source.VertexIndex, target.VertexIndex) != null
                             || _graph.FindEdge(target.VertexIndex, source.VertexIndex) != null;

            // Union-Find decides in (almost) constant time whether the zones are already joined.
            bool sameZone = _zones.AreConnected(source.VertexIndex, target.VertexIndex);

            if (sameZone)
            {
                return new LineProposalResult(LineProposalVerdict.AddsRedundancyInSameZone,
                    $"{source.Code} and {target.Code} are already in the same electrical zone. " +
                    "The new line would add a redundant supply path (a loop), improving resilience but also maintenance cost.",
                    directExists);
            }

            return new LineProposalResult(LineProposalVerdict.JoinsDifferentZones,
                $"{source.Code} and {target.Code} belong to different electrical zones. " +
                "The new line would merge both zones into one.",
                directExists);
        }

        // ------------------------------------------------------------------ Analysis

        /// <inheritdoc />
        public RouteResult FindOptimalRoute(string sourceCode, string targetCode, EdgeWeightKind criterion)
        {
            // Step 1 (Hash Table): resolve both codes into full records.
            Substation? source = FindSubstation(sourceCode);
            if (source == null)
            {
                return Failed(RouteStatus.SourceNotFound, $"Origin '{NormalizeCode(sourceCode)}' is not registered.", criterion);
            }

            Substation? target = FindSubstation(targetCode);
            if (target == null)
            {
                return Failed(RouteStatus.TargetNotFound, $"Destination '{NormalizeCode(targetCode)}' is not registered.", criterion);
            }

            if (source.Code == target.Code)
            {
                return new RouteResult(RouteStatus.Found, "Origin and destination are the same substation.",
                    new[] { source }, 0, 0, 0, criterion);
            }

            // Step 2 (Union-Find): reject impossible requests without running Dijkstra.
            if (!_zones.AreConnected(source.VertexIndex, target.VertexIndex))
            {
                return Failed(RouteStatus.DifferentZones,
                    $"{source.Code} and {target.Code} are in different electrical zones; no route can exist. Dijkstra was not executed.",
                    criterion);
            }

            // Step 3 (Graph): Dijkstra over the directed lines.
            // TODO [DIJKSTRA]: Graph.ShortestPath currently throws NotImplementedException; this call needs no change once it is implemented.
            PathResult path = _graph.ShortestPath(source.VertexIndex, target.VertexIndex, criterion);
            if (!path.Found)
            {
                return Failed(RouteStatus.NoDirectedPath,
                    "Both substations share a zone, but one-way lines prevent power from reaching the destination.",
                    criterion);
            }

            Substation[] route = new Substation[path.Vertices.Length];
            for (int i = 0; i < route.Length; i++)
            {
                route[i] = _substationByVertex[path.Vertices[i]]!;
            }

            string metricName = criterion == EdgeWeightKind.Length ? "length" : "maintenance cost";
            return new RouteResult(RouteStatus.Found, $"Route found optimizing {metricName}.",
                route, path.TotalWeight, path.TotalLength, path.TotalCost, criterion);
        }

        /// <inheritdoc />
        public TraversalResult TraverseBreadthFirst(string startCode)
        {
            Substation? start = FindSubstation(startCode);
            if (start == null)
            {
                return new TraversalResult(false, $"Substation '{NormalizeCode(startCode)}' was not found.", new Substation[0]);
            }

            return new TraversalResult(true, "BFS completed.", MapVertices(_graph.BreadthFirstTraversal(start.VertexIndex)));
        }

        /// <inheritdoc />
        public TraversalResult TraverseDepthFirst(string startCode)
        {
            Substation? start = FindSubstation(startCode);
            if (start == null)
            {
                return new TraversalResult(false, $"Substation '{NormalizeCode(startCode)}' was not found.", new Substation[0]);
            }

            return new TraversalResult(true, "DFS completed.", MapVertices(_graph.DepthFirstTraversal(start.VertexIndex)));
        }

        /// <inheritdoc />
        public ConnectivityResult CheckSameZone(string codeA, string codeB)
        {
            Substation? a = FindSubstation(codeA);
            Substation? b = FindSubstation(codeB);

            if (a == null || b == null)
            {
                return new ConnectivityResult(false, false, "Both substations must exist.");
            }

            bool connected = _zones.AreConnected(a.VertexIndex, b.VertexIndex);
            string message = connected
                ? $"{a.Code} and {b.Code} belong to the SAME electrical zone."
                : $"{a.Code} and {b.Code} belong to DIFFERENT electrical zones.";

            return new ConnectivityResult(true, connected, message);
        }

        /// <inheritdoc />
        public ElectricalZone[] GetZones()
        {
            int slots = _graph.VertexSlotCount;
            int[] zoneIdOfRoot = new int[slots];
            int[] zoneSize = new int[slots];
            int zoneCount = 0;

            for (int i = 0; i < slots; i++)
            {
                zoneIdOfRoot[i] = -1;
            }

            // Pass 1: assign a zone id to every root and count members.
            for (int vertex = 0; vertex < slots; vertex++)
            {
                if (!_graph.IsActive(vertex))
                {
                    continue;
                }

                int root = _zones.Find(vertex);
                if (zoneIdOfRoot[root] == -1)
                {
                    zoneIdOfRoot[root] = zoneCount++;
                }

                zoneSize[zoneIdOfRoot[root]]++;
            }

            // Pass 2: distribute the members.
            Substation[][] members = new Substation[zoneCount][];
            int[] fill = new int[zoneCount];
            for (int z = 0; z < zoneCount; z++)
            {
                members[z] = new Substation[zoneSize[z]];
            }

            for (int vertex = 0; vertex < slots; vertex++)
            {
                if (!_graph.IsActive(vertex))
                {
                    continue;
                }

                int zone = zoneIdOfRoot[_zones.Find(vertex)];
                members[zone][fill[zone]++] = _substationByVertex[vertex]!;
            }

            ElectricalZone[] result = new ElectricalZone[zoneCount];
            for (int z = 0; z < zoneCount; z++)
            {
                result[z] = new ElectricalZone(z + 1, members[z]);
            }

            return result;
        }

        /// <inheritdoc />
        public GridSummary GetSummary()
        {
            double capacity = 0;
            Substation[] substations = GetAllSubstations();
            for (int i = 0; i < substations.Length; i++)
            {
                capacity += substations[i].CapacityMw;
            }

            double length = 0;
            double cost = 0;
            PowerLine[] lines = GetAllLines();
            for (int i = 0; i < lines.Length; i++)
            {
                // A physical line is counted once, whether it is one-way or two-way.
                length += lines[i].LengthKm;
                cost += lines[i].MaintenanceCost;
            }

            return new GridSummary(substations.Length, lines.Length, GetZones().Length, capacity, length, cost);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _registry = new HashTable<Substation>();
            _graph = new Graph();
            _zones = new DisjointSet();
            // TODO [TRIE]: _nameIndex = new Trie();
            _substationByVertex = new ArrayBuffer<Substation?>();
        }

        // ------------------------------------------------------------------ Inspector

        /// <inheritdoc />
        public string DescribeHashTable() => _registry.Describe();

        /// <inheritdoc />
        public string DescribeGraph() => _graph.Describe(LabelOf);

        /// <inheritdoc />
        public string DescribeTrie() => "Trie is pending implementation.\n"; // TODO [TRIE]: return _nameIndex.Describe();

        /// <inheritdoc />
        public string DescribeDisjointSets()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Union-Find | elements: {_zones.ElementCount} | sets (incl. removed slots): {_zones.SetCount}");

            for (int vertex = 0; vertex < _graph.VertexSlotCount; vertex++)
            {
                if (!_graph.IsActive(vertex))
                {
                    continue;
                }

                int parent = _zones.PeekParent(vertex);
                string role = parent == vertex ? "ROOT" : $"parent = {LabelOf(parent)}";
                builder.AppendLine($"  {LabelOf(vertex)}  ({role}, rank {_zones.PeekRank(vertex)})");
            }

            return builder.ToString();
        }

        // ------------------------------------------------------------------ Helpers

        /// <summary>
        /// Union-Find cannot split sets, so after deleting data the zones are recomputed
        /// from the surviving edges. O(V + E * alpha(V)).
        /// </summary>
        private void RebuildZones()
        {
            _zones.ResetToSingletons();
            _graph.ForEachEdge((from, edge) => _zones.Union(from, edge.Target));
        }

        private Substation[] MapVertices(int[] vertices)
        {
            Substation[] result = new Substation[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                result[i] = _substationByVertex[vertices[i]]!;
            }

            return result;
        }

        private string LabelOf(int vertex)
        {
            Substation? substation = _substationByVertex[vertex];
            return substation == null ? $"#{vertex}" : $"{substation.Code} ({substation.Name})";
        }

        private static RouteResult Failed(RouteStatus status, string message, EdgeWeightKind criterion)
            => new RouteResult(status, message, new Substation[0], 0, 0, 0, criterion);

        private static string NormalizeCode(string? code)
            => string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();
    }
}
