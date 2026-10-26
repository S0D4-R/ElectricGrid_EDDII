//Jorge Rivera -> 1511425

using System;
using System.Collections.Generic;
using System.Text;
using ElectricGrid.Collections;

namespace ElectricGrid.DataStructures
{
    /// <summary>Atributo de la arista que minimiza el cálculo de ruta mínima.</summary>
    public enum EdgeWeightKind
    {
        /// <summary>Costo de mantenimiento acumulado.</summary>
        MaintenanceCost,

        /// <summary>Longitud acumulada en kilómetros.</summary>
        Length
    }

    /// <summary>Arista saliente almacenada en una lista de adyacencia enlazada.</summary>
    public sealed class GraphEdge
    {
        internal GraphEdge(int target, double length, double cost)
        {
            Target = target;
            Length = length;
            Cost = cost;
        }

        /// <summary>Índice del vértice destino.</summary>
        public int Target { get; }

        /// <summary>Longitud de la línea en kilómetros.</summary>
        public double Length { get; }

        /// <summary>Costo de mantenimiento de la línea.</summary>
        public double Cost { get; }

        internal GraphEdge? Next { get; set; }

        internal double WeightFor(EdgeWeightKind kind) => kind == EdgeWeightKind.Length ? Length : Cost;
    }

    /// <summary>Resultado de un cálculo de ruta mínima sobre el grafo.</summary>
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

        /// <summary>Indica si existe un camino.</summary>
        public bool Found { get; }

        /// <summary>Índices de los vértices desde el origen hasta el destino.</summary>
        public int[] Vertices { get; }

        /// <summary>Valor acumulado de la métrica minimizada.</summary>
        public double TotalWeight { get; }

        /// <summary>Longitud total del camino en kilómetros.</summary>
        public double TotalLength { get; }

        /// <summary>Costo total de mantenimiento del camino.</summary>
        public double TotalCost { get; }

        internal static PathResult NotFound() => new PathResult(false, new int[0], 0, 0, 0);

        internal static PathResult Success(int[] vertices, double weight, double length, double cost)
            => new PathResult(true, vertices, weight, length, cost);
    }

    /// <summary>
    /// Grafo dirigido y ponderado con listas de adyacencia enlazadas.
    /// Los vértices son subestaciones y las aristas son líneas; una línea de doble sentido se guarda como dos aristas opuestas.
    /// Incluye los recorridos BFS y DFS, y el espacio para el algoritmo de Dijkstra.
    /// </summary>
    /// <remarks>Responsable: Integrante B (Grafo).</remarks>
    public sealed class Graph
    {
        private GraphEdge?[] _adjacency;
        private bool[] _active;
        private int _slotCount;
        private int _activeVertexCount;
        private int _edgeCount;

        /// <summary>Crea un grafo vacío con la capacidad inicial indicada.</summary>
        public Graph(int initialCapacity = 16)
        {
            int capacity = initialCapacity < 1 ? 1 : initialCapacity;
            _adjacency = new GraphEdge?[capacity];
            _active = new bool[capacity];
        }

        /// <summary>Cantidad de posiciones de vértice creadas, incluidas las eliminadas.</summary>
        public int VertexSlotCount => _slotCount;

        /// <summary>Cantidad de vértices que no han sido eliminados.</summary>
        public int ActiveVertexCount => _activeVertexCount;

        /// <summary>Cantidad de aristas dirigidas.</summary>
        public int EdgeCount => _edgeCount;

        /// <summary>Agrega un vértice y devuelve su índice.</summary>
        public int AddVertex()
        {
            if (_slotCount == _adjacency.Length)
            {
                ArrayUtils.Resize(ref _adjacency, _adjacency.Length * 2);
                ArrayUtils.Resize(ref _active, _active.Length * 2);
            }

            int index = _slotCount++;
            _adjacency[index] = null;
            _active[index] = true;
            _activeVertexCount++;
            return index;
        }

        /// <summary>Indica si el índice corresponde a un vértice existente.</summary>
        public bool IsActive(int vertex) => vertex >= 0 && vertex < _slotCount && _active[vertex];

        /// <summary>Elimina un vértice junto con todas sus aristas entrantes y salientes.</summary>
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
        /// Agrega una línea entre dos vértices; si es bidireccional agrega también la arista opuesta.
        /// Devuelve falso si la arista ya existe, algún vértice es inválido o los pesos son negativos.
        /// </summary>
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

        /// <summary>Elimina una línea y, si se indica, también su arista opuesta; devuelve verdadero si se eliminó alguna.</summary>
        public bool RemoveEdge(int from, int to, bool bidirectional)
        {
            bool removed = RemoveDirectedEdge(from, to);
            if (bidirectional)
            {
                removed |= RemoveDirectedEdge(to, from);
            }

            return removed;
        }

        /// <summary>Busca la arista dirigida de un vértice a otro; devuelve nulo si no existe.</summary>
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

        /// <summary>Recorre todas las aristas dirigidas informando su vértice origen.</summary>
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
        /// Recorrido en anchura desde un vértice, respetando el sentido de las aristas.
        /// Queue&lt;T&gt; se usa únicamente como estructura de control del orden de visita.
        /// </summary>
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

        /// <summary>Recorrido en profundidad iterativo desde un vértice, con una pila propia basada en arreglos.</summary>
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
                    top--;
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

        /// <summary>Calcula la ruta mínima entre dos vértices con el algoritmo de Dijkstra (pendiente de implementar).</summary>
        public PathResult ShortestPath(int source, int target, EdgeWeightKind weightKind)
        {
            RequireActive(source);
            RequireActive(target);

            // TODO [DIJKSTRA]: implementar el algoritmo con una cola de prioridad propia y retirar la excepción.
            throw new NotImplementedException("El algoritmo de Dijkstra está pendiente de implementación (Graph.ShortestPath).");
        }

        /// <summary>Genera una descripción imprimible de las listas de adyacencia.</summary>
        public string Describe(Func<int, string> labelOf)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Grafo | vértices: {_activeVertexCount} | aristas dirigidas: {_edgeCount}");

            for (int vertex = 0; vertex < _slotCount; vertex++)
            {
                if (!_active[vertex])
                {
                    continue;
                }

                builder.Append($"  {labelOf(vertex)} -> ");

                if (_adjacency[vertex] == null)
                {
                    builder.AppendLine("(sin líneas de salida)");
                    continue;
                }

                for (GraphEdge? edge = _adjacency[vertex]; edge != null; edge = edge.Next)
                {
                    builder.Append($"{labelOf(edge.Target)} [{edge.Length:F1} km, costo {edge.Cost:F0}]");
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
                throw new ArgumentException($"El vértice {vertex} no existe en el grafo.", nameof(vertex));
            }
        }
    }
}
