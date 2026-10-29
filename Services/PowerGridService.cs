using System;
using System.Text;
using ElectricGrid.Collections;
using ElectricGrid.DataStructures;
using ElectricGrid.Models;

namespace ElectricGrid.Services
{
    // Coordina la tabla hash, el grafo, Union-Find y el Trie.
    public sealed class PowerGridService : IPowerGridService, IStructureInspector
    {
        private const int DefaultSuggestionLimit = 10;

        private HashTable<Substation> _registry = new HashTable<Substation>();

        private Graph _graph = new Graph();
        private DisjointSet _zones = new DisjointSet();
        private Trie _nameIndex = new Trie();
        private ArrayBuffer<Substation?> _substationByVertex = new ArrayBuffer<Substation?>();

        // Valida los datos y registra la subestación en las cuatro estructuras.
        public OperationResult RegisterSubstation(string code, string name, double capacityMw)
        {
            string normalizedCode = NormalizeCode(code);
            string cleanName = name?.Trim() ?? string.Empty;

            if (normalizedCode.Length == 0)
            {
                return OperationResult.Fail("El código de la subestación no puede estar vacío.");
            }

            if (cleanName.Length == 0)
            {
                return OperationResult.Fail("El nombre de la subestación no puede estar vacío.");
            }

            if (capacityMw <= 0 || double.IsNaN(capacityMw) || double.IsInfinity(capacityMw))
            {
                return OperationResult.Fail("La capacidad debe ser un número positivo de MW.");
            }

            if (_registry.ContainsKey(normalizedCode))
            {
                return OperationResult.Fail($"Ya existe una subestación con el código '{normalizedCode}'.");
            }

            int vertex = _graph.AddVertex();
            int element = _zones.MakeSet();
            if (vertex != element || vertex != _substationByVertex.Count)
            {
                throw new InvalidOperationException("Los índices internos del grafo y de Union-Find no coinciden.");
            }

            Substation substation = new Substation(normalizedCode, cleanName, capacityMw, vertex);
            _registry.Insert(normalizedCode, substation);
            _nameIndex.Insert(cleanName, normalizedCode);
            _substationByVertex.Add(substation);

            return OperationResult.Ok($"Subestación '{normalizedCode}' ({cleanName}) registrada.");
        }

        // Elimina la subestación, sus líneas y reconstruye las zonas.
        public OperationResult RemoveSubstation(string code)
        {
            Substation? substation = FindSubstation(code);
            if (substation == null)
            {
                return OperationResult.Fail($"No se encontró la subestación '{NormalizeCode(code)}'.");
            }

            _nameIndex.Remove(substation.Name, substation.Code);
            _registry.Remove(substation.Code);
            _graph.RemoveVertex(substation.VertexIndex);
            _substationByVertex[substation.VertexIndex] = null;
            RebuildZones();

            return OperationResult.Ok($"Subestación '{substation.Code}' y sus líneas fueron eliminadas.");
        }

        // Busca por código normalizado en la tabla hash.
        public Substation? FindSubstation(string code)
        {
            string normalized = NormalizeCode(code);
            if (normalized.Length == 0)
            {
                return null;
            }

            return _registry.TryGetValue(normalized, out Substation substation) ? substation : null;
        }

        // Usa el Trie para devolver subestaciones cuyo nombre empieza con el prefijo.
        public Substation[] SuggestSubstations(string prefix, int maxResults)
        {
            int limit = maxResults <= 0 ? DefaultSuggestionLimit : maxResults;
            TrieMatch[] matches = _nameIndex.SearchByPrefix(prefix, limit);
            ArrayBuffer<Substation> suggestions = new ArrayBuffer<Substation>(matches.Length + 1);
            for (int i = 0; i < matches.Length; i++)
            {
                if (_registry.TryGetValue(matches[i].Payload, out Substation substation))
                {
                    suggestions.Add(substation);
                }
            }
            return suggestions.ToArray();
        }

        // Busca por prefijo si el Trie está activo; si no, recorre la lista.
        public Substation[] SearchSubstations(string text, int maxResults)
        {
            int limit = maxResults <= 0 ? DefaultSuggestionLimit : maxResults;

            if (SupportsAutocomplete)
            {
                return SuggestSubstations(text, limit);
            }

            return LinearSearch(text, limit);
        }

        public bool SupportsAutocomplete => true;

        // Devuelve las subestaciones en el orden en que se registraron.
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

        // Crea la línea en el grafo y une las zonas de sus extremos.
        public OperationResult RegisterLine(PowerLine line)
        {
            Substation? source = FindSubstation(line.SourceCode);
            Substation? target = FindSubstation(line.TargetCode);

            if (source == null)
            {
                return OperationResult.Fail($"La subestación de origen '{NormalizeCode(line.SourceCode)}' no existe.");
            }

            if (target == null)
            {
                return OperationResult.Fail($"La subestación de destino '{NormalizeCode(line.TargetCode)}' no existe.");
            }

            if (source.Code == target.Code)
            {
                return OperationResult.Fail("Una línea debe conectar dos subestaciones distintas.");
            }

            if (line.LengthKm <= 0 || line.MaintenanceCost < 0)
            {
                return OperationResult.Fail("La longitud debe ser positiva y el costo de mantenimiento no puede ser negativo.");
            }

            bool added = _graph.AddEdge(source.VertexIndex, target.VertexIndex,
                                        line.LengthKm, line.MaintenanceCost, line.IsBidirectional);
            if (!added)
            {
                return OperationResult.Fail($"Ya existe una línea {source.Code} -> {target.Code}.");
            }

            _zones.Union(source.VertexIndex, target.VertexIndex);

            string direction = line.IsBidirectional ? "<->" : "->";
            return OperationResult.Ok($"Línea {source.Code} {direction} {target.Code} registrada.");
        }

        // Quita la línea del grafo y recalcula las zonas.
        public OperationResult RemoveLine(string sourceCode, string targetCode, bool bidirectional)
        {
            Substation? source = FindSubstation(sourceCode);
            Substation? target = FindSubstation(targetCode);

            if (source == null || target == null)
            {
                return OperationResult.Fail("Ambas subestaciones deben existir.");
            }

            if (!_graph.RemoveEdge(source.VertexIndex, target.VertexIndex, bidirectional))
            {
                return OperationResult.Fail($"No se encontró una línea {source.Code} -> {target.Code}.");
            }

            RebuildZones();
            return OperationResult.Ok($"Línea entre {source.Code} y {target.Code} eliminada.");
        }

        // Recorre las aristas y devuelve cada línea una sola vez.
        public PowerLine[] GetAllLines()
        {
            ArrayBuffer<PowerLine> lines = new ArrayBuffer<PowerLine>();

            _graph.ForEachEdge((from, edge) =>
            {
                GraphEdge? reverse = _graph.FindEdge(edge.Target, from);
                bool mirrored = reverse != null && reverse.Length == edge.Length && reverse.Cost == edge.Cost;

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

        // Consulta Union-Find para saber si la línea uniría zonas o sería redundante.
        public LineProposalResult EvaluateProposedLine(string sourceCode, string targetCode)
        {
            Substation? source = FindSubstation(sourceCode);
            Substation? target = FindSubstation(targetCode);

            if (source == null || target == null)
            {
                return new LineProposalResult(LineProposalVerdict.Invalid,
                    "Ambas subestaciones deben existir para proponer una línea.", false);
            }

            if (source.Code == target.Code)
            {
                return new LineProposalResult(LineProposalVerdict.Invalid,
                    "Una línea debe conectar dos subestaciones distintas.", false);
            }

            bool directExists = _graph.FindEdge(source.VertexIndex, target.VertexIndex) != null
                             || _graph.FindEdge(target.VertexIndex, source.VertexIndex) != null;

            bool sameZone = _zones.AreConnected(source.VertexIndex, target.VertexIndex);

            if (sameZone)
            {
                return new LineProposalResult(LineProposalVerdict.AddsRedundancyInSameZone,
                    $"{source.Code} y {target.Code} ya están en la misma zona eléctrica. " +
                    "La nueva línea solo agregaría una ruta redundante (un anillo): mejora la resiliencia, pero aumenta el costo de mantenimiento.",
                    directExists);
            }

            return new LineProposalResult(LineProposalVerdict.JoinsDifferentZones,
                $"{source.Code} y {target.Code} pertenecen a zonas eléctricas distintas. " +
                "La nueva línea unificaría ambas zonas.",
                directExists);
        }

        // Comprueba los códigos, arma un grafo con las líneas y ejecuta Dijkstra.
        public RouteResult FindOptimalRoute(string startCode, string endCode, EdgeWeightKind criterion)
        {
            if (!_registry.TryGetValue(startCode, out _))
            {
                return new RouteResult(RouteStatus.SourceNotFound, $"La subestación de origen '{startCode}' no existe.", Array.Empty<Substation>(), 0, 0, 0, criterion);
            }

            if (!_registry.TryGetValue(endCode, out _))
            {
                return new RouteResult(RouteStatus.TargetNotFound, $"La subestación de destino '{endCode}' no existe.", Array.Empty<Substation>(), 0, 0, 0, criterion);
            }

            Substation[] allSubstations = GetAllSubstations();
            DijkstraGraph graph = new DijkstraGraph(allSubstations.Length);
            PowerLine[] allLines = GetAllLines();

            for (int i = 0; i < allLines.Length; i++)
            {
                PowerLine line = allLines[i];
                double weight = criterion == EdgeWeightKind.Length ? line.LengthKm : line.MaintenanceCost;

                graph.AddEdge(line.SourceCode, line.TargetCode, weight);
                if (line.IsBidirectional)
                {
                    graph.AddEdge(line.TargetCode, line.SourceCode, weight);
                }
            }

            ShortestPathResult shortestPath = graph.FindShortestPath(startCode, endCode);

            if (!shortestPath.IsReachable)
            {
                return new RouteResult(RouteStatus.NoDirectedPath, $"No existe una ruta posible entre {startCode} y {endCode}.", Array.Empty<Substation>(), 0, 0, 0, criterion);
            }

            Substation[] pathSubstations = new Substation[shortestPath.Path.Length];
            double totalLength = 0;
            double totalCost = 0;

            for (int i = 0; i < shortestPath.Path.Length; i++)
            {
                string nodeCode = shortestPath.Path[i];

                _registry.TryGetValue(nodeCode, out Substation sub);
                pathSubstations[i] = sub;

                if (i > 0)
                {
                    string prevCode = shortestPath.Path[i - 1];

                    for (int j = 0; j < allLines.Length; j++)
                    {
                        PowerLine line = allLines[j];
                        if ((line.SourceCode == prevCode && line.TargetCode == nodeCode) ||
                            (line.IsBidirectional && line.TargetCode == prevCode && line.SourceCode == nodeCode))
                        {
                            totalLength += line.LengthKm;
                            totalCost += line.MaintenanceCost;
                            break;
                        }
                    }
                }
            }

            return new RouteResult(RouteStatus.Found, "Ruta óptima calculada exitosamente.", pathSubstations, shortestPath.TotalDistance, totalLength, totalCost, criterion);
        }

        // Recorre la red en anchura desde la subestación indicada.
        public TraversalResult TraverseBreadthFirst(string startCode)
        {
            Substation? start = FindSubstation(startCode);
            if (start == null)
            {
                return new TraversalResult(false, $"No se encontró la subestación '{NormalizeCode(startCode)}'.", new Substation[0]);
            }

            return new TraversalResult(true, "Recorrido BFS completado.", MapVertices(_graph.BreadthFirstTraversal(start.VertexIndex)));
        }

        // Recorre la red en profundidad desde la subestación indicada.
        public TraversalResult TraverseDepthFirst(string startCode)
        {
            Substation? start = FindSubstation(startCode);
            if (start == null)
            {
                return new TraversalResult(false, $"No se encontró la subestación '{NormalizeCode(startCode)}'.", new Substation[0]);
            }

            return new TraversalResult(true, "Recorrido DFS completado.", MapVertices(_graph.DepthFirstTraversal(start.VertexIndex)));
        }

        // Compara las raíces de Union-Find de ambas subestaciones.
        public ConnectivityResult CheckSameZone(string codeA, string codeB)
        {
            Substation? a = FindSubstation(codeA);
            Substation? b = FindSubstation(codeB);

            if (a == null || b == null)
            {
                return new ConnectivityResult(false, false, "Ambas subestaciones deben existir.");
            }

            bool connected = _zones.AreConnected(a.VertexIndex, b.VertexIndex);
            string message = connected
                ? $"{a.Code} y {b.Code} pertenecen a la MISMA zona eléctrica."
                : $"{a.Code} y {b.Code} pertenecen a zonas eléctricas DISTINTAS.";

            return new ConnectivityResult(true, connected, message);
        }

        // Agrupa los vértices activos según su raíz de Union-Find.
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

        // Suma capacidades, longitudes y costos para el resumen.
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
                length += lines[i].LengthKm;
                cost += lines[i].MaintenanceCost;
            }

            return new GridSummary(substations.Length, lines.Length, GetZones().Length, capacity, length, cost);
        }

        // Reemplaza todas las estructuras por unas vacías.
        public void Clear()
        {
            _registry = new HashTable<Substation>();
            _graph = new Graph();
            _zones = new DisjointSet();
            _substationByVertex = new ArrayBuffer<Substation?>();
        }

        // Texto con las cubetas y colisiones de la tabla hash.
        public string DescribeHashTable() => _registry.Describe();

        // Texto con las listas de adyacencia del grafo.
        public string DescribeGraph() => _graph.Describe(LabelOf);

        // Texto con la estructura del Trie.
        public string DescribeTrie() => _nameIndex.ObtenerEstructuraVisual();

        // Texto con el padre y el rango de cada vértice.
        public string DescribeDisjointSets()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Union-Find | elementos: {_zones.ElementCount} | conjuntos (incluye posiciones eliminadas): {_zones.SetCount}");

            for (int vertex = 0; vertex < _graph.VertexSlotCount; vertex++)
            {
                if (!_graph.IsActive(vertex))
                {
                    continue;
                }

                int parent = _zones.PeekParent(vertex);
                string role = parent == vertex ? "RAÍZ" : $"padre = {LabelOf(parent)}";
                builder.AppendLine($"  {LabelOf(vertex)}  ({role}, rango {_zones.PeekRank(vertex)})");
            }

            return builder.ToString();
        }

        // Recorre las subestaciones buscando el texto en el nombre o el código.
        private Substation[] LinearSearch(string text, int limit)
        {
            string pattern = Fold(text.Trim());
            ArrayBuffer<Substation> found = new ArrayBuffer<Substation>();
            if (pattern.Length == 0)
            {
                return found.ToArray();
            }

            for (int i = 0; i < _substationByVertex.Count && found.Count < limit; i++)
            {
                Substation? substation = _substationByVertex[i];
                if (substation == null)
                {
                    continue;
                }

                if (Fold(substation.Name).IndexOf(pattern, StringComparison.Ordinal) >= 0
                    || Fold(substation.Code).IndexOf(pattern, StringComparison.Ordinal) >= 0)
                {
                    found.Add(substation);
                }
            }

            return found.ToArray();
        }

        // Pasa el texto a minúsculas y quita tildes para comparar.
        private static string Fold(string text)
        {
            StringBuilder builder = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                builder.Append(FoldChar(text[i]));
            }

            return builder.ToString();
        }

        // Equivalencia sin tilde de una letra.
        private static char FoldChar(char c)
        {
            switch (char.ToLowerInvariant(c))
            {
                case 'á': case 'à': case 'ä': case 'â': return 'a';
                case 'é': case 'è': case 'ë': case 'ê': return 'e';
                case 'í': case 'ì': case 'ï': case 'î': return 'i';
                case 'ó': case 'ò': case 'ö': case 'ô': return 'o';
                case 'ú': case 'ù': case 'ü': case 'û': return 'u';
                case 'ñ': return 'n';
                default: return char.ToLowerInvariant(c);
            }
        }

        // Vuelve a formar las zonas con las líneas que quedan.
        private void RebuildZones()
        {
            _zones.ResetToSingletons();
            _graph.ForEachEdge((from, edge) => _zones.Union(from, edge.Target));
        }

        // Convierte índices de vértice en subestaciones.
        private Substation[] MapVertices(int[] vertices)
        {
            Substation[] result = new Substation[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                result[i] = _substationByVertex[vertices[i]]!;
            }

            return result;
        }

        // Etiqueta legible para mostrar en las estructuras.
        private string LabelOf(int vertex)
        {
            Substation? substation = _substationByVertex[vertex];
            return substation == null ? $"#{vertex}" : $"{substation.Code} ({substation.Name})";
        }

        // Crea un resultado de ruta fallido.
        private static RouteResult Failed(RouteStatus status, string message, EdgeWeightKind criterion)
            => new RouteResult(status, message, new Substation[0], 0, 0, 0, criterion);

        // Deja el código en mayúsculas y sin espacios externos.
        private static string NormalizeCode(string? code)
            => string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();
    }
}
