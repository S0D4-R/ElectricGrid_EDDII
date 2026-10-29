//Rodrigo Herman

using System;

namespace ElectricGrid.DataStructures
{
    // Arista dirigida con el nodo destino y su peso.
    public readonly struct Edge(int targetNode, double weight)
    {
        public int TargetNode { get; } = targetNode;
        public double Weight { get; } = weight;
    }

    // Resultado de una búsqueda: distancia total y camino de nombres.
    public readonly struct ShortestPathResult(double totalDistance, string[] path)
    {
        public double TotalDistance { get; } = totalDistance;
        public string[] Path { get; } = path ?? Array.Empty<string>();
        public bool IsReachable => Path.Length > 0;
    }

    // Grafo dirigido usado por Dijkstra, con arreglos y un min-heap propio.
    public sealed class DijkstraGraph
    {
        private string[] _nodeNames;
        private Edge[][] _edges;
        private int[] _edgeCounts;
        private int _nodeCount;

        // Reserva la capacidad inicial de nodos y aristas.
        public DijkstraGraph(int initialCapacity = 16)
        {
            _nodeNames = new string[initialCapacity];
            _edges = new Edge[initialCapacity][];
            _edgeCounts = new int[initialCapacity];
            _nodeCount = 0;
        }

        // Devuelve el id del nodo y lo crea si todavía no existe.
        public int AddNode(string name)
        {
            for (int i = 0; i < _nodeCount; i++)
            {
                if (string.Equals(_nodeNames[i], name, StringComparison.Ordinal))
                    return i;
            }

            if (_nodeCount == _nodeNames.Length)
            {
                int newSize = _nodeNames.Length * 2;

                string[] newNames = new string[newSize];
                Array.Copy(_nodeNames, newNames, _nodeCount);
                _nodeNames = newNames;

                Edge[][] newEdges = new Edge[newSize][];
                Array.Copy(_edges, newEdges, _nodeCount);
                _edges = newEdges;

                int[] newCounts = new int[newSize];
                Array.Copy(_edgeCounts, newCounts, _nodeCount);
                _edgeCounts = newCounts;
            }

            int id = _nodeCount++;
            _nodeNames[id] = name;
            _edges[id] = new Edge[2];
            _edgeCounts[id] = 0;

            return id;
        }

        // Agrega una arista dirigida entre dos nombres de nodo.
        public void AddEdge(string source, string target, double weight)
        {
            int u = AddNode(source);
            int v = AddNode(target);

            int count = _edgeCounts[u];
            if (count == _edges[u].Length)
            {
                Edge[] newArr = new Edge[_edges[u].Length * 2];
                Array.Copy(_edges[u], newArr, count);
                _edges[u] = newArr;
            }

            _edges[u][count] = new Edge(v, weight);
            _edgeCounts[u]++;
        }

        // Calcula el camino de menor peso entre dos nodos.
        public ShortestPathResult FindShortestPath(string startNode, string endNode)
        {
            int startId = GetNodeId(startNode);
            int endId = GetNodeId(endNode);

            if (startId == -1 || endId == -1)
                return new ShortestPathResult(double.PositiveInfinity, Array.Empty<string>());

            double[] distances = new double[_nodeCount];
            int[] previous = new int[_nodeCount];

            for (int i = 0; i < _nodeCount; i++)
            {
                distances[i] = double.PositiveInfinity;
                previous[i] = -1;
            }

            distances[startId] = 0;
            MinHeap priorityQueue = new MinHeap(_nodeCount * 2);
            priorityQueue.Push(startId, 0);

            while (!priorityQueue.IsEmpty)
            {
                HeapNode current = priorityQueue.Pop();
                int u = current.NodeId;

                if (u == endId) break;

                if (current.Distance > distances[u]) continue;

                for (int i = 0; i < _edgeCounts[u]; i++)
                {
                    Edge edge = _edges[u][i];
                    int v = edge.TargetNode;
                    double newDist = distances[u] + edge.Weight;

                    if (newDist < distances[v])
                    {
                        distances[v] = newDist;
                        previous[v] = u;
                        priorityQueue.Push(v, newDist);
                    }
                }
            }

            if (distances[endId] == double.PositiveInfinity)
                return new ShortestPathResult(double.PositiveInfinity, Array.Empty<string>());

            int pathLength = 0;
            int curr = endId;
            while (curr != -1)
            {
                pathLength++;
                curr = previous[curr];
            }

            string[] path = new string[pathLength];
            curr = endId;
            for (int i = pathLength - 1; i >= 0; i--)
            {
                path[i] = _nodeNames[curr];
                curr = previous[curr];
            }

            return new ShortestPathResult(distances[endId], path);
        }

        // Busca un nodo por nombre y devuelve -1 si no está.
        private int GetNodeId(string name)
        {
            for (int i = 0; i < _nodeCount; i++)
            {
                if (string.Equals(_nodeNames[i], name, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        // Entrada del heap con el nodo y su distancia acumulada.
        private readonly struct HeapNode(int nodeId, double distance)
        {
            public int NodeId { get; } = nodeId;
            public double Distance { get; } = distance;
        }

        // Cola de prioridad mínima implementada sobre un arreglo.
        private sealed class MinHeap
        {
            private HeapNode[] _data;
            private int _count;

            // Crea el heap con la capacidad indicada.
            public MinHeap(int capacity)
            {
                _data = new HeapNode[capacity == 0 ? 4 : capacity];
                _count = 0;
            }

            public bool IsEmpty => _count == 0;

            // Inserta un nodo y restaura el orden del heap.
            public void Push(int nodeId, double distance)
            {
                if (_count == _data.Length)
                {
                    HeapNode[] newArr = new HeapNode[_data.Length * 2];
                    Array.Copy(_data, newArr, _count);
                    _data = newArr;
                }

                _data[_count] = new HeapNode(nodeId, distance);
                HeapifyUp(_count);
                _count++;
            }

            // Saca el nodo con la menor distancia.
            public HeapNode Pop()
            {
                if (_count == 0) throw new InvalidOperationException("Heap vacío");

                HeapNode root = _data[0];
                _count--;
                _data[0] = _data[_count];
                _data[_count] = default;

                if (_count > 0)
                {
                    HeapifyDown(0);
                }

                return root;
            }

            // Sube un elemento hasta que su padre sea menor.
            private void HeapifyUp(int index)
            {
                int parent = (index - 1) / 2;
                while (index > 0 && _data[index].Distance < _data[parent].Distance)
                {
                    Swap(index, parent);
                    index = parent;
                    parent = (index - 1) / 2;
                }
            }

            // Baja un elemento hasta que sus hijos sean mayores.
            private void HeapifyDown(int index)
            {
                int lastIndex = _count - 1;
                while (true)
                {
                    int leftChild = 2 * index + 1;
                    int rightChild = 2 * index + 2;
                    int smallest = index;

                    if (leftChild <= lastIndex && _data[leftChild].Distance < _data[smallest].Distance)
                        smallest = leftChild;

                    if (rightChild <= lastIndex && _data[rightChild].Distance < _data[smallest].Distance)
                        smallest = rightChild;

                    if (smallest == index) break;

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            // Intercambia dos posiciones del arreglo.
            private void Swap(int i, int j)
            {
                HeapNode temp = _data[i];
                _data[i] = _data[j];
                _data[j] = temp;
            }
        }
    }
}
