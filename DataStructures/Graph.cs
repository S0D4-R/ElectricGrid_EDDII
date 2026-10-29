//Jorge Rivera -> 1511425

using System;
using System.Collections.Generic;
using System.Text;
using ElectricGrid.Collections;

namespace ElectricGrid.DataStructures
{
    public enum EdgeWeightKind
    {
        MaintenanceCost,

        Length
    }

    public sealed class GraphEdge
    {
        // Guarda el destino, la longitud y el costo de la arista.
        internal GraphEdge(int target, double length, double cost)
        {
            Target = target;
            Length = length;
            Cost = cost;
        }

        public int Target { get; }

        public double Length { get; }

        public double Cost { get; }

        internal GraphEdge? Next { get; set; }

        // Devuelve el atributo que se va a minimizar.
        internal double WeightFor(EdgeWeightKind kind) => kind == EdgeWeightKind.Length ? Length : Cost;
    }

    public sealed class PathResult
    {
        // Guarda el camino y los totales calculados.
        private PathResult(bool found, int[] vertices, double weight, double length, double cost)
        {
            Found = found;
            Vertices = vertices;
            TotalWeight = weight;
            TotalLength = length;
            TotalCost = cost;
        }

        public bool Found { get; }

        public int[] Vertices { get; }

        public double TotalWeight { get; }

        public double TotalLength { get; }

        public double TotalCost { get; }

        // Resultado para una ruta que no existe.
        internal static PathResult NotFound() => new PathResult(false, new int[0], 0, 0, 0);

        // Resultado para una ruta encontrada.
        internal static PathResult Success(int[] vertices, double weight, double length, double cost)
            => new PathResult(true, vertices, weight, length, cost);
    }

    // Grafo dirigido con listas de adyacencia enlazadas.
    public sealed class Graph
    {
        private GraphEdge?[] _adjacency;
        private bool[] _active;
        private int _slotCount;
        private int _activeVertexCount;
        private int _edgeCount;

        // Reserva la capacidad inicial del grafo.
        public Graph(int initialCapacity = 16)
        {
            int capacity = initialCapacity < 1 ? 1 : initialCapacity;
            _adjacency = new GraphEdge?[capacity];
            _active = new bool[capacity];
        }

        public int VertexSlotCount => _slotCount;

        public int ActiveVertexCount => _activeVertexCount;

        public int EdgeCount => _edgeCount;

        // Agrega un vértice y devuelve su índice.
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

        // Indica si el índice corresponde a un vértice activo.
        public bool IsActive(int vertex) => vertex >= 0 && vertex < _slotCount && _active[vertex];

        // Elimina el vértice y todas sus aristas entrantes y salientes.
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

        // Agrega una arista y también la opuesta si es bidireccional.
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

        // Quita la arista indicada y, si aplica, la de vuelta.
        public bool RemoveEdge(int from, int to, bool bidirectional)
        {
            bool removed = RemoveDirectedEdge(from, to);
            if (bidirectional)
            {
                removed |= RemoveDirectedEdge(to, from);
            }

            return removed;
        }

        // Busca una arista dirigida entre dos vértices.
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

        // Recorre todas las aristas e informa el vértice de origen.
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

        // Recorre el grafo en anchura desde el vértice indicado.
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

        // Recorre el grafo en profundidad sin recursión.
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

        // Ruta mínima entre dos vértices; todavía no está implementada aquí.
        public PathResult ShortestPath(int source, int target, EdgeWeightKind weightKind)
        {
            RequireActive(source);
            RequireActive(target);

            throw new NotImplementedException("El algoritmo de Dijkstra está pendiente de implementación (Graph.ShortestPath).");
        }

        // Genera el texto de las listas de adyacencia.
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

        // Reconstruye el camino y suma longitud y costo.
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

        // Agrega la arista al final de la lista del vértice.
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

        // Quita una arista dirigida concreta.
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

        // Lanza un error si el vértice no existe o fue eliminado.
        private void RequireActive(int vertex)
        {
            if (!IsActive(vertex))
            {
                throw new ArgumentException($"El vértice {vertex} no existe en el grafo.", nameof(vertex));
            }
        }
    }
}
