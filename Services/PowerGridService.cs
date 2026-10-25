using System;
using System.Text;
using ElectricGrid.Collections;
using ElectricGrid.DataStructures;
using ElectricGrid.Models;

namespace ElectricGrid.Services
{
    /// <summary>
    /// Fachada que coordina las estructuras de datos en un mismo flujo: el Trie sugiere nombres, la tabla hash
    /// localiza el registro, Union-Find valida la conectividad y el grafo calcula rutas y recorridos.
    /// No contiene código de interfaz de usuario.
    /// </summary>
    /// <remarks>
    /// Capa de integración desarrollada por todo el equipo. El índice de vértice del grafo, el elemento de
    /// Union-Find y la posición en el arreglo de subestaciones son siempre el mismo número.
    /// </remarks>
    public sealed class PowerGridService : IPowerGridService, IStructureInspector
    {
        private const int DefaultSuggestionLimit = 10;

        private HashTable<Substation> _registry = new HashTable<Substation>();
        
        private Graph _graph = new Graph();
        private DisjointSet _zones = new DisjointSet();
        // TODO [TRIE]: descomentar al implementar Trie.cs.
        private Trie _nameIndex = new Trie();
        private ArrayBuffer<Substation?> _substationByVertex = new ArrayBuffer<Substation?>();

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public Substation[] SearchSubstations(string text, int maxResults)
        {
            int limit = maxResults <= 0 ? DefaultSuggestionLimit : maxResults;

            if (SupportsAutocomplete)
            {
                // TODO [TRIE]: bloque de búsqueda por prefijo; se activa cuando SupportsAutocomplete sea true.
                return SuggestSubstations(text, limit);
            }

            return LinearSearch(text, limit);
        }

        /// <inheritdoc />
        public bool SupportsAutocomplete => true; 

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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        

        /// <inheritdoc />
        public RouteResult FindOptimalRoute(string startCode, string endCode, EdgeWeightKind criterion)
        {
            // 1. Validar que ambas subestaciones existan usando TryGetValue de la tabla hash
            if (!_registry.TryGetValue(startCode, out _))
            {
                return new RouteResult(RouteStatus.SourceNotFound, $"La subestación de origen '{startCode}' no existe.", Array.Empty<Substation>(), 0, 0, 0, criterion);
            }
            
            if (!_registry.TryGetValue(endCode, out _))
            {
                return new RouteResult(RouteStatus.TargetNotFound, $"La subestación de destino '{endCode}' no existe.", Array.Empty<Substation>(), 0, 0, 0, criterion);
            }

            // 2. Instanciar y poblar el grafo de Dijkstra
            // (Usamos GetAllSubstations().Length si _registry.Count no está disponible en tu HashTable)
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

            // 3. Ejecutar el algoritmo
            ShortestPathResult shortestPath = graph.FindShortestPath(startCode, endCode);

            // 4. Evaluar el resultado
            if (!shortestPath.IsReachable)
            {
                return new RouteResult(RouteStatus.NoDirectedPath, $"No existe una ruta posible entre {startCode} y {endCode}.", Array.Empty<Substation>(), 0, 0, 0, criterion);
            }

            // 5. Reconstruir los objetos Substation y calcular totales
            Substation[] pathSubstations = new Substation[shortestPath.Path.Length];
            double totalLength = 0;
            double totalCost = 0;

            for (int i = 0; i < shortestPath.Path.Length; i++)
            {
                string nodeCode = shortestPath.Path[i];
                
                // Extraer la subestación
                _registry.TryGetValue(nodeCode, out Substation sub);
                pathSubstations[i] = sub;

                // Sumar costos y longitudes (excepto en el nodo inicial)
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

            // 6. Retornar éxito con todos los parámetros que exige tu constructor
            return new RouteResult(RouteStatus.Found, "Ruta óptima calculada exitosamente.", pathSubstations, shortestPath.TotalDistance, totalLength, totalCost, criterion);
        }

        /// <inheritdoc />
        public TraversalResult TraverseBreadthFirst(string startCode)
        {
            Substation? start = FindSubstation(startCode);
            if (start == null)
            {
                return new TraversalResult(false, $"No se encontró la subestación '{NormalizeCode(startCode)}'.", new Substation[0]);
            }

            return new TraversalResult(true, "Recorrido BFS completado.", MapVertices(_graph.BreadthFirstTraversal(start.VertexIndex)));
        }

        /// <inheritdoc />
        public TraversalResult TraverseDepthFirst(string startCode)
        {
            Substation? start = FindSubstation(startCode);
            if (start == null)
            {
                return new TraversalResult(false, $"No se encontró la subestación '{NormalizeCode(startCode)}'.", new Substation[0]);
            }

            return new TraversalResult(true, "Recorrido DFS completado.", MapVertices(_graph.DepthFirstTraversal(start.VertexIndex)));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public string DescribeHashTable() => _registry.Describe();

        /// <inheritdoc />
        public string DescribeGraph() => _graph.Describe(LabelOf);

        /// <inheritdoc />
        public string DescribeTrie() => "El Trie está pendiente de implementación.\n"; // TODO [TRIE]: devolver _nameIndex.Describe().

        /// <inheritdoc />
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

        /// <summary>Búsqueda lineal por coincidencia parcial, sin distinguir mayúsculas ni tildes.</summary>
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

        /// <summary>Convierte el texto a minúsculas y sin tildes para comparar.</summary>
        private static string Fold(string text)
        {
            StringBuilder builder = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                builder.Append(FoldChar(text[i]));
            }

            return builder.ToString();
        }

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
