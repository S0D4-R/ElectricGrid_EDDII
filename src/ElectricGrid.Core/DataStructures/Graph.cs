using System;
using System.Collections.Generic;
using System.Text;
using ElectricGrid.Core.Collections;

namespace ElectricGrid.Core.DataStructures
{
    /// <summary>Selects which edge attribute Dijkstra's algorithm minimizes.</summary>
    public enum EdgeWeightKind
    {
        /// <summary>Minimize the accumulated maintenance cost.</summary>
        MaintenanceCost,

        /// <summary>Minimize the accumulated line length (km).</summary>
        Length
    }

    /// <summary>Outgoing edge stored in an adjacency (linked) list.</summary>
    public sealed class GraphEdge
    {
        internal GraphEdge(int target, double length, double cost)
        {
            Target = target;
            Length = length;
            Cost = cost;
        }

        /// <summary>Index of the destination vertex.</summary>
        public int Target { get; }

        /// <summary>Length of the line in kilometres.</summary>
        public double Length { get; }

        /// <summary>Maintenance cost of the line.</summary>
        public double Cost { get; }

        internal GraphEdge? Next { get; set; }

        internal double WeightFor(EdgeWeightKind kind) => kind == EdgeWeightKind.Length ? Length : Cost;
    }

    /// <summary>Outcome of a shortest-path computation on the graph.</summary>
    public sealed class PathResult
    {
        private PathResult(bool found, int[] vertices, double weight, double length, double cost)
        {
            Found = found;
            Vertices = vertices;
            TotalWeight = weight;
            TotalLength = length;
            TotalCost = cost;
        }

        /// <summary>Whether a path exists.</summary>
        public bool Found { get; }

        /// <summary>Vertex indices from source to target (empty when not found).</summary>
        public int[] Vertices { get; }

        /// <summary>Value of the minimized metric along the path.</summary>
        public double TotalWeight { get; }

        /// <summary>Total length (km) along the path.</summary>
        public double TotalLength { get; }

        /// <summary>Total maintenance cost along the path.</summary>
        public double TotalCost { get; }

        internal static PathResult NotFound() => new PathResult(false, new int[0], 0, 0, 0);

        internal static PathResult Success(int[] vertices, double weight, double length, double cost)
            => new PathResult(true, vertices, weight, length, cost);
    }

    /// <summary>
    /// Weighted directed graph stored as adjacency lists (linked lists of edges).
    /// <para>
    /// ROLE IN THE SYSTEM: models the distribution network. Vertices are substations and
    /// edges are power lines; a two-way line is stored as two opposite edges, a one-way line
    /// (main substation to secondary substations) as a single edge. It provides the BFS and
    /// DFS traversals; Dijkstra's algorithm (lowest-cost / shortest route) is a pending slot, see <see cref="ShortestPath"/>.
    /// </para>
    /// <para>
    /// Why adjacency lists: electrical grids are sparse (E is close to V), so the O(V + E)
    /// memory of lists beats the O(V^2) of an adjacency matrix, and traversals only visit
    /// real neighbours.
    /// </para>
    /// <para>
    /// Vertices are identified by an integer index. Removing a vertex marks its slot as
    /// inactive (indices of the remaining vertices never change), which keeps the indices
    /// shared with the Union-Find structure and the substation registry stable.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Developed by: Team Member B (Graph owner) - replace with the real name.
    /// </remarks>
    public sealed class Graph
    {
        private GraphEdge?[] _adjacency;
        private bool[] _active;
        private int _slotCount;
        private int _activeVertexCount;
        private int _edgeCount;

        /// <summary>Creates an empty graph.</summary>
        public Graph(int initialCapacity = 16)
        {
            int capacity = initialCapacity < 1 ? 1 : initialCapacity;
            _adjacency = new GraphEdge?[capacity];
            _active = new bool[capacity];
        }

        /// <summary>Total number of vertex slots ever created (including removed ones).</summary>
        public int VertexSlotCount => _slotCount;

        /// <summary>Number of vertices that have not been removed.</summary>
        public int ActiveVertexCount => _activeVertexCount;

        /// <summary>Number of directed edges (a two-way line counts as two).</summary>
        public int EdgeCount => _edgeCount;

        /// <summary>Adds a vertex and returns its index. Amortized O(1).</summary>
        public int AddVertex()
        {
            if (_slotCount == _adjacency.Length)
            {
                Array.Resize(ref _adjacency, _adjacency.Length * 2);
                Array.Resize(ref _active, _active.Length * 2);
            }

            int index = _slotCount++;
            _adjacency[index] = null;
            _active[index] = true;
            _activeVertexCount++;
            return index;
        }

        /// <summary>Checks whether the index refers to a vertex that exists. O(1).</summary>
        public bool IsActive(int vertex) => vertex >= 0 && vertex < _slotCount && _active[vertex];

        /// <summary>
        /// Removes a vertex together with all its incoming and outgoing edges. O(V + E).
        /// </summary>
        public void RemoveVertex(int vertex)
        {
            if (!IsActive(vertex))
            {
                return;
            }

            for (GraphEdge? edge = _adjacency[vertex]; edge != null; edge = edge.Next)
            {
                _edgeCount--;
            }

            _adjacency[vertex] = null;

            for (int other = 0; other < _slotCount; other++)
            {
                if (_active[other])
                {
                    RemoveDirectedEdge(other, vertex);
                }
            }

            _active[vertex] = false;
            _activeVertexCount--;
        }

        /// <summary>
        /// Adds a line between two vertices. O(deg(from)) because duplicates are checked.
        /// </summary>
        /// <param name="from">Origin vertex index.</param>
        /// <param name="to">Destination vertex index.</param>
        /// <param name="length">Line length in kilometres (non-negative).</param>
        /// <param name="cost">Maintenance cost (non-negative).</param>
        /// <param name="bidirectional">When <c>true</c> the opposite edge is added too.</param>
        /// <returns><c>false</c> when an equal edge already exists, a vertex is invalid or the
        /// weights are negative (Dijkstra requires non-negative weights).</returns>
        public bool AddEdge(int from, int to, double length, double cost, bool bidirectional)
        {
            if (!IsActive(from) || !IsActive(to) || from == to)
            {
                return false;
            }

            if (length < 0 || cost < 0)
            {
                return false;
            }

            if (FindEdge(from, to) != null || (bidirectional && FindEdge(to, from) != null))
            {
                return false;
            }

            AppendEdge(from, new GraphEdge(to, length, cost));
            _edgeCount++;

            if (bidirectional)
            {
                AppendEdge(to, new GraphEdge(from, length, cost));
                _edgeCount++;
            }

            return true;
        }

        /// <summary>Removes a line (and its opposite edge when requested). O(deg).</summary>
        /// <returns><c>true</c> if at least one edge was removed.</returns>
        public bool RemoveEdge(int from, int to, bool bidirectional)
        {
            bool removed = RemoveDirectedEdge(from, to);
            if (bidirectional)
            {
                removed |= RemoveDirectedEdge(to, from);
            }

            return removed;
        }

        /// <summary>Finds the directed edge from -&gt; to, or <c>null</c>. O(deg(from)).</summary>
        public GraphEdge? FindEdge(int from, int to)
        {
            if (!IsActive(from))
            {
                return null;
            }

            for (GraphEdge? edge = _adjacency[from]; edge != null; edge = edge.Next)
            {
                if (edge.Target == to)
                {
                    return edge;
                }
            }

            return null;
        }

        /// <summary>Visits every directed edge as (source, edge). O(V + E).</summary>
        public void ForEachEdge(Action<int, GraphEdge> visitor)
        {
            for (int vertex = 0; vertex < _slotCount; vertex++)
            {
                if (!_active[vertex])
                {
                    continue;
                }

                for (GraphEdge? edge = _adjacency[vertex]; edge != null; edge = edge.Next)
                {
                    visitor(vertex, edge);
                }
            }
        }

        /// <summary>
        /// Breadth-first traversal from <paramref name="start"/>. O(V + E).
        /// </summary>
        /// <remarks>
        /// <see cref="Queue{T}"/> is used only as the visiting-order control structure, which is
        /// the single native collection the project statement allows for BFS.
        /// </remarks>
        /// <returns>Vertex indices in visiting order (only vertices reachable by following edge direction).</returns>
        public int[] BreadthFirstTraversal(int start)
        {
            RequireActive(start);

            bool[] visited = new bool[_slotCount];
            ArrayBuffer<int> order = new ArrayBuffer<int>();
            Queue<int> frontier = new Queue<int>();

            visited[start] = true;
            frontier.Enqueue(start);

            while (frontier.Count > 0)
            {
                int vertex = frontier.Dequeue();
                order.Add(vertex);

                for (GraphEdge? edge = _adjacency[vertex]; edge != null; edge = edge.Next)
                {
                    if (!visited[edge.Target])
                    {
                        visited[edge.Target] = true;
                        frontier.Enqueue(edge.Target);
                    }
                }
            }

            return order.ToArray();
        }

        /// <summary>
        /// Depth-first traversal from <paramref name="start"/>. O(V + E).
        /// Iterative implementation with an explicit array-based stack (no recursion, so large
        /// grids cannot overflow the call stack).
        /// </summary>
        /// <returns>Vertex indices in pre-order visiting order.</returns>
        public int[] DepthFirstTraversal(int start)
        {
            RequireActive(start);

            bool[] visited = new bool[_slotCount];
            int[] stackVertices = new int[_slotCount];
            GraphEdge?[] stackCursors = new GraphEdge?[_slotCount];
            ArrayBuffer<int> order = new ArrayBuffer<int>();
            int top = 0;

            visited[start] = true;
            order.Add(start);
            stackVertices[top] = start;
            stackCursors[top] = _adjacency[start];
            top++;

            while (top > 0)
            {
                int index = top - 1;
                GraphEdge? edge = stackCursors[index];

                if (edge == null)
                {
                    top--; // all neighbours explored: backtrack
                    continue;
                }

                stackCursors[index] = edge.Next;

                if (!visited[edge.Target])
                {
                    visited[edge.Target] = true;
                    order.Add(edge.Target);
                    stackVertices[top] = edge.Target;
                    stackCursors[top] = _adjacency[edge.Target];
                    top++;
                }
            }

            return order.ToArray();
        }

        /// <summary>
        /// Dijkstra's single-pair shortest path. PENDING IMPLEMENTATION.
        /// </summary>
        /// <param name="source">Origin vertex.</param>
        /// <param name="target">Destination vertex.</param>
        /// <param name="weightKind">Edge attribute to minimize (<see cref="GraphEdge.WeightFor"/> returns it).</param>
        /// <returns>
        /// A <see cref="PathResult"/>: use <c>PathResult.NotFound()</c> when the target is unreachable
        /// and <c>BuildPath(...)</c> (already written below) to build the successful result.
        /// </returns>
        /// <remarks>
        /// TODO [DIJKSTRA - assigned teammate]: implement here. Requirements from the project statement:
        /// <list type="bullet">
        /// <item><description>Respect edge direction (follow <c>_adjacency</c> lists as BFS/DFS do).</description></item>
        /// <item><description>Needs a priority queue written by the team: <c>PriorityQueue&lt;TElement,TPriority&gt;</c>
        /// is forbidden. Suggested: create <c>Collections/MinHeap.cs</c> (binary heap).</description></item>
        /// <item><description>Keep arrays <c>distance[]</c>, <c>previous[]</c>, <c>settled[]</c> of size <c>_slotCount</c>.</description></item>
        /// <item><description>Finish with <c>return BuildPath(source, target, previous, distance[target]);</c>
        /// or <c>PathResult.NotFound()</c> when <c>distance[target]</c> is infinity.</description></item>
        /// <item><description>Once done, delete the <c>NotImplementedException</c> below.</description></item>
        /// </list>
        /// The caller (<c>PowerGridService.FindOptimalRoute</c>) already resolves the codes with the
        /// hash table and validates connectivity with Union-Find before calling this method.
        /// </remarks>
        public PathResult ShortestPath(int source, int target, EdgeWeightKind weightKind)
        {
            RequireActive(source);
            RequireActive(target);

            // TODO [DIJKSTRA]: replace this line with the algorithm.
            throw new NotImplementedException("Dijkstra's algorithm is pending implementation (see Graph.ShortestPath).");
        }

        /// <summary>
        /// Builds a printable adjacency-list view of the graph.
        /// </summary>
        /// <param name="labelOf">Maps a vertex index to the text shown for it.</param>
        public string Describe(Func<int, string> labelOf)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Graph | vertices: {_activeVertexCount} | directed edges: {_edgeCount}");

            for (int vertex = 0; vertex < _slotCount; vertex++)
            {
                if (!_active[vertex])
                {
                    continue;
                }

                builder.Append($"  {labelOf(vertex)} -> ");

                if (_adjacency[vertex] == null)
                {
                    builder.AppendLine("(no outgoing lines)");
                    continue;
                }

                for (GraphEdge? edge = _adjacency[vertex]; edge != null; edge = edge.Next)
                {
                    builder.Append($"{labelOf(edge.Target)} [{edge.Length:F1} km, cost {edge.Cost:F0}]");
                    if (edge.Next != null)
                    {
                        builder.Append("  |  ");
                    }
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private PathResult BuildPath(int source, int target, int[] previous, double totalWeight)
        {
            int length = 1;
            for (int vertex = target; vertex != source; vertex = previous[vertex])
            {
                length++;
            }

            int[] path = new int[length];
            int position = length - 1;
            for (int vertex = target; ; vertex = previous[vertex])
            {
                path[position--] = vertex;
                if (vertex == source)
                {
                    break;
                }
            }

            double totalLength = 0;
            double totalCost = 0;
            for (int i = 0; i < path.Length - 1; i++)
            {
                GraphEdge? edge = FindEdge(path[i], path[i + 1]);
                if (edge != null)
                {
                    totalLength += edge.Length;
                    totalCost += edge.Cost;
                }
            }

            return PathResult.Success(path, totalWeight, totalLength, totalCost);
        }

        private void AppendEdge(int from, GraphEdge edge)
        {
            GraphEdge? tail = _adjacency[from];
            if (tail == null)
            {
                _adjacency[from] = edge;
                return;
            }

            while (tail.Next != null)
            {
                tail = tail.Next;
            }

            tail.Next = edge;
        }

        private bool RemoveDirectedEdge(int from, int to)
        {
            if (!IsActive(from))
            {
                return false;
            }

            GraphEdge? previous = null;
            for (GraphEdge? edge = _adjacency[from]; edge != null; edge = edge.Next)
            {
                if (edge.Target == to)
                {
                    if (previous == null)
                    {
                        _adjacency[from] = edge.Next;
                    }
                    else
                    {
                        previous.Next = edge.Next;
                    }

                    _edgeCount--;
                    return true;
                }

                previous = edge;
            }

            return false;
        }

        private void RequireActive(int vertex)
        {
            if (!IsActive(vertex))
            {
                throw new ArgumentException($"Vertex {vertex} does not exist in the graph.", nameof(vertex));
            }
        }
    }
}
