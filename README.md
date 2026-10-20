# Gestión de Red Eléctrica Municipal

**Estructura de Datos II – Proyecto de Laboratorio No. 2** · C# (.NET 8) · Aplicación de consola

Administra las subestaciones y líneas de distribución de un municipio: registro, búsqueda por código,
recorridos, zonas eléctricas conectadas y validación de líneas nuevas. Todas las estructuras están
implementadas desde cero (sin `List`, `Dictionary`, `HashSet`, `SortedSet` ni `PriorityQueue`;
solo `Queue<T>` como cola de visita en BFS, según el enunciado).

> **Estado:** el **Trie** y el algoritmo de **Dijkstra** están como espacios marcados para otros integrantes
> (ver [Pendientes](#pendientes)). Todo lo demás funciona.

## Estructuras y responsables

| Estructura | Archivo | Función en el problema | Responsable |
|---|---|---|---|
| Tabla Hash | `DataStructures/HashTable.cs` | Localizar una subestación por código en O(1) promedio | *(nombre)* |
| Grafo (BFS, DFS) | `DataStructures/Graph.cs` | Líneas de distribución dirigidas y recorridos | *(nombre)* |
| Union-Find | `DataStructures/DisjointSet.cs` | Zonas eléctricas; validar conectividad antes de Dijkstra y antes de aprobar una línea | *(nombre)* |
| Trie *(pendiente)* | `DataStructures/Trie.cs` | Sugerir nombres de subestación al escribir | *(nombre)* |
| Dijkstra *(pendiente)* | `Graph.cs` → `ShortestPath` | Ruta de menor costo o menor longitud | *(nombre)* |

## Flujo integrado

1. **Trie** sugiere el nombre mientras se escribe *(pendiente)*.
2. **Tabla Hash** convierte el código en el registro completo.
3. **Union-Find** verifica que ambas subestaciones estén en la misma zona; si no, se detiene sin ejecutar Dijkstra.
4. **Grafo** calcula la ruta con Dijkstra *(pendiente)* o los recorridos BFS/DFS.

Al proponer una línea nueva, Union-Find indica si uniría dos zonas distintas o solo añadiría redundancia
dentro de la misma zona, y el operador decide si la aprueba.

## Arquitectura

```
ElectricGrid.sln
src/
├── ElectricGrid.Core/          Lógica sin interfaz (reutilizable por una GUI)
│   ├── Collections/            ArrayBuffer<T> (arreglo dinámico propio)
│   ├── DataStructures/         HashTable, Graph, DisjointSet, Trie
│   ├── Models/                 Substation, PowerLine y objetos de resultado
│   ├── Services/               IPowerGridService, PowerGridService (integra las estructuras)
│   ├── IO/                     GridDataLoader (carga de CSV/TXT)
│   └── Reporting/              GridReportBuilder (reporte en texto)
└── ElectricGrid.ConsoleApp/    Interfaz de consola
    ├── Program.cs              Único punto de entrada
    ├── UI/                     Menú, lectura de datos, presentación
    └── Data/                   substations.csv, lines.csv (ejemplos)
```

`Core` no usa `Console`; toda operación devuelve objetos de resultado. Para una GUI futura basta crear un
proyecto nuevo que referencie `Core` y use `IPowerGridService`.

## Ejecución

```bash
dotnet build
dotnet run --project src/ElectricGrid.ConsoleApp
```

Opción **3** del menú: carga los datos de ejemplo. Mientras el Trie esté pendiente, las subestaciones se
seleccionan por **código** (por ejemplo `SUB-004`).

## Archivos de datos (.csv / .txt)

`substations.csv`: `code,name,capacity_mw`
```
SUB-001,Central Principal,250
```

`lines.csv`: `source_code,target_code,length_km,maintenance_cost,bidirectional`
```
SUB-001,SUB-002,4.5,1800,false    ← un solo sentido: SUB-001 → SUB-002
SUB-001,SUB-014,1.8,900,true      ← doble sentido
```
Encabezado opcional; se ignoran líneas vacías y las que empiezan con `#`; decimales con punto; `bidirectional`
es opcional (por defecto `true`). El archivo se busca en la ruta indicada, junto al ejecutable y en su carpeta
`Data`. Las filas inválidas se omiten y se reportan con su número de línea.

## Menú

| Opciones | Función |
|---|---|
| 1–3, 23 | Cargar subestaciones, líneas o datos de ejemplo; borrar todo |
| 4–8 | Registrar, buscar por código, eliminar, autocompletar *(pendiente)* y listar subestaciones |
| 9–12 | Registrar, proponer (validación de zona), eliminar y listar líneas |
| 13 | Ruta óptima *(pendiente Dijkstra; ya valida con Hash y Union-Find)* |
| 14–15 | Recorrido BFS y DFS |
| 16–17 | ¿Misma zona eléctrica? / listar zonas |
| 18 | Resumen y exportación del reporte |
| 19–22 | Inspección interna de Hash, Grafo, Trie y Union-Find |

## Complejidad (Big-O)

n = elementos, V = vértices, E = aristas, α = inversa de Ackermann.

| Estructura | Operación | Complejidad | Alternativa nativa |
|---|---|---|---|
| Tabla Hash | insertar / buscar / eliminar | O(1) promedio, O(n) peor caso | `Dictionary`: igual |
| Grafo (listas de adyacencia) | agregar arista | O(grado) | — |
| | BFS / DFS | O(V + E) | — |
| | memoria | O(V + E) (matriz: O(V²)) | — |
| Union-Find | Find / Union (compresión + rango) | O(α(n)) amortizado | BFS por consulta: O(V + E) |
| Trie *(pendiente)* | insertar / prefijo | O(k) / O(p + m) | recorrer colección: O(n·p) |
| Dijkstra *(pendiente)* | ruta mínima | O((V + E) log V) con heap | — |

## Notas de diseño

- Union-Find modela conectividad **no dirigida**: una línea de un solo sentido igual une dos subestaciones en
  una zona. Solo el grafo decide si existe un camino **dirigido**; por eso hay dos fallos distintos:
  «zonas diferentes» y «sin camino dirigido».
- Union-Find no puede separar conjuntos: al eliminar una línea o subestación se reconstruyen las zonas desde
  las aristas restantes.
- Los vértices eliminados conservan su índice (inactivo), así los índices compartidos entre grafo,
  Union-Find y registro nunca cambian.
- No se aceptan pesos negativos al crear líneas, para que Dijkstra pueda asumir pesos no negativos.

## Pendientes

Buscar `TODO [` en la solución para ubicar cada punto. Nada más requiere cambios.

**Dijkstra** (`TODO [DIJKSTRA]`)
- Implementar `Graph.ShortestPath` y quitar el `NotImplementedException`. Ya existen `BuildPath(...)`,
  `PathResult.NotFound()` y `GraphEdge.WeightFor(...)`.
- Crear `Collections/MinHeap.cs` (cola de prioridad propia; `PriorityQueue` está prohibida).
- `FindOptimalRoute` y la opción 13 ya están listos y no cambian.

**Trie** (`TODO [TRIE]`)
- Implementar los métodos de `Trie.cs` (las firmas ya están definidas).
- En `PowerGridService.cs`: descomentar el campo `_nameIndex` y sus usos en `RegisterSubstation`,
  `RemoveSubstation` y `Clear`; reemplazar el cuerpo de `SuggestSubstations` por el código comentado;
  poner `SupportsAutocomplete` en `true` y hacer que `DescribeTrie` devuelva `_nameIndex.Describe()`.
- La consola activa sola el autocompletado en vivo (escribir + TAB) y las opciones 7 y 21.
