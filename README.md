# Gestión de Red Eléctrica Municipal

**Estructura de Datos II – Proyecto de Laboratorio No. 2** · C# · Interfaz de texto en pantalla completa (TUI)

Administra las subestaciones y líneas de distribución de un municipio: registro, búsqueda por código,
recorridos, zonas eléctricas conectadas, validación de líneas nuevas y dibujo del grafo en la terminal.

> **Estado:** el **Trie** y el algoritmo de **Dijkstra** están como espacios marcados para otros integrantes
> (ver [Pendientes](#pendientes)). Todo lo demás funciona.

## Ejecución

Requisito: **SDK de .NET 8 o superior** (`dotnet --version`). El proyecto toma la versión de tu SDK y no usa paquetes de NuGet.

```bash
dotnet run
```

Se ejecuta desde la carpeta raíz del repositorio (donde está `ElectricGrid.csproj`) y no necesita parámetros.
Funciona en cualquier terminal de **80×24 o más**; si los recuadros se ven mal, usar la opción «ASCII / Unicode»
del menú o iniciar con `dotnet run -- --ascii`. Si cambias de versión de .NET y hay errores, borra las carpetas
generadas con `rm -rf bin obj`.

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

Un solo proyecto, organizado en carpetas:

```
ElectricGrid.csproj
Program.cs                  Único punto de entrada
Collections/                ArrayBuffer<T> y ArrayUtils (arreglo dinámico y utilidades propias)
DataStructures/             HashTable, Graph, DisjointSet, Trie
Models/                     Substation, PowerLine y objetos de resultado
Services/                   IPowerGridService, PowerGridService (integra las estructuras)
IO/                         GridDataLoader (carga de CSV/TXT)
Reporting/                  GridReportBuilder (reporte en texto)
Tui/                        Pantalla, menú, ventanas y dibujo del grafo
Data/                       subestaciones.csv, lineas.csv (ejemplos)
```

Las carpetas de lógica (`Collections`, `DataStructures`, `Models`, `Services`, `IO`, `Reporting`) no usan la consola ni
dependen de `Tui`: toda operación devuelve objetos de resultado. Solo `Tui/` y `Program.cs` interactúan con
la terminal. Para una GUI futura bastaría reemplazar `Tui/` y usar `IPowerGridService`.

## Cumplimiento de la implementación propia

- No se usa `List<T>`, `Dictionary`, `HashSet`, `SortedSet`, `PriorityQueue` ni LINQ.
- Las estructuras obligatorias solo usan arreglos y nodos propios; `ArrayBuffer<T>` y `ArrayUtils` reemplazan
  a `List<T>` y a `Array.Resize/Clear`.
- El único tipo de colección nativo es `Queue<T>`, exclusivamente como cola de visita en el recorrido BFS
  (`Graph.BreadthFirstTraversal`).
- Lo nativo se limita a soporte auxiliar: lectura de archivos, manejo de cadenas y consola.

## Interfaz TUI

Menú a la izquierda y, a la derecha, el **grafo dibujado**: cada subestación es un nodo `[SUB-001]` coloreado por
zona eléctrica (componente de Union-Find), las líneas de doble sentido son `───` y las de un solo sentido
terminan en flecha `──>`. La posición de los nodos se calcula con un algoritmo de fuerzas (Fruchterman-Reingold).

| Teclas | Acción |
|---|---|
| `Tab` | Cambiar el foco entre menú y grafo |
| Flechas (en el grafo) | Seleccionar la subestación más cercana en esa dirección |
| `Enter` (en el grafo) | Detalle de la subestación seleccionada |
| `B` / `D` | BFS / DFS desde la seleccionada (se resalta en amarillo) |
| `R` | Ruta óptima; el camino se resalta cuando exista Dijkstra |
| `C` / `L` / `F` | Quitar resaltado / cambiar etiquetas (completas o cortas) / grafo a pantalla completa |
| `/` | Buscar subestación por parte del nombre o del código (desde cualquier panel) |
| `?` / `Q` | Ayuda / salir |

Se adapta al redimensionar la ventana y pasa a etiquetas cortas cuando hay muchos nodos. Con más de ~40
subestaciones o muchas líneas cruzadas el dibujo se vuelve difícil de leer; para esos casos usar las listas del menú.

## Archivos de datos (.csv / .txt)

`subestaciones.csv`: `codigo,nombre,capacidad_mw`
```
SUB-001,Central Principal,250
```

`lineas.csv`: `origen,destino,longitud_km,costo_mantenimiento,doble_sentido`
```
SUB-001,SUB-002,4.5,1800,false
SUB-001,SUB-014,1.8,900,true
```
Encabezado opcional (en español o inglés); se ignoran líneas vacías y las que empiezan con `#`; decimales con
punto; `doble_sentido` es opcional (por defecto `true`) y acepta `true/false/si/no/1/0`. El archivo se busca en la
ruta indicada, junto al ejecutable y en su carpeta `Data`. Las filas inválidas se omiten y se reportan con su
número de línea.

## Menú

| Sección | Opciones |
|---|---|
| Datos | Cargar ejemplo, cargar subestaciones, cargar líneas, borrar todo |
| Subestaciones | Registrar, buscar por código, buscar por nombre *(tecla `/`)*, eliminar, listar |
| Líneas | Registrar, proponer línea nueva (validación de zona), eliminar, listar |
| Análisis | Ruta óptima *(pendiente Dijkstra)*, BFS, DFS, ¿misma zona?, listar zonas, reporte/exportar |
| Herramientas | Ver estructuras internas, ASCII/Unicode, ayuda, salir |

Mientras el Trie esté pendiente, las subestaciones se seleccionan por **código** (por ejemplo `SUB-004`); los
formularios muestran una vista previa de la subestación escrita.

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
- `FindOptimalRoute` y la opción «Ruta óptima» ya están listos y no cambian; el camino se resalta solo en el grafo.

**Trie** (`TODO [TRIE]`)
- Implementar los métodos de `Trie.cs` (las firmas ya están definidas).
- En `PowerGridService.cs`: descomentar el campo `_nameIndex` y sus usos en `RegisterSubstation`,
  `RemoveSubstation` y `Clear`; reemplazar el cuerpo de `SuggestSubstations` por el código comentado;
  poner `SupportsAutocomplete` en `true` y hacer que `DescribeTrie` devuelva `_nameIndex.Describe()`.
- Los formularios de la TUI aceptarán nombres automáticamente cuando `SupportsAutocomplete` sea `true`.
- La búsqueda por nombre (opción del menú y tecla `/`) ya funciona con una búsqueda lineal provisional
  (`PowerGridService.LinearSearch`, por coincidencia parcial). El bloque `TODO [TRIE]` dentro de
  `SearchSubstations` la reemplaza por la búsqueda por prefijo en cuanto `SupportsAutocomplete` sea `true`;
  después se puede borrar `LinearSearch`. La TUI no necesita cambios.
