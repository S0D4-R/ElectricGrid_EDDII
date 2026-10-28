# Gestión de Red Eléctrica Municipal

**Estructura de Datos II – Proyecto de Laboratorio No. 2** · C# · Interfaz de texto en pantalla completa (TUI)

Aplicación de consola que administra las subestaciones y las líneas de distribución de un municipio. Permite
registrar y localizar subestaciones, calcular rutas de menor costo o menor longitud, recorrer la red, conocer qué
subestaciones pertenecen a la misma zona eléctrica y validar una línea nueva antes de aprobarla. La red se dibuja
como un grafo dentro de la propia terminal.

## Funcionalidades

- **Registro de subestaciones** con código único, nombre y capacidad en MW; búsqueda por código y por nombre.
- **Registro de líneas** entre subestaciones, con longitud, costo de mantenimiento y sentido (doble sentido o un
  solo sentido, por ejemplo de la subestación principal hacia las secundarias).
- **Ruta óptima** entre dos subestaciones, por menor costo de mantenimiento o por menor longitud, respetando el
  sentido de las líneas. El camino se resalta en el grafo.
- **Recorridos BFS y DFS** desde cualquier subestación, siguiendo el sentido de las líneas.
- **Zonas eléctricas:** saber si dos subestaciones están en la misma zona conectada y listar todas las zonas.
- **Validación de líneas nuevas:** antes de aprobar una línea se indica si uniría dos zonas distintas o solo
  añadiría una ruta redundante dentro de la misma zona.
- **Carga de datos desde archivos** `.csv` o `.txt`, con reporte de las filas rechazadas, o ingreso manual por menú.
- **Reporte de la red** (resumen, zonas y líneas) que puede verse en pantalla y exportarse a un archivo de texto.
- **Dibujo del grafo** con colores por zona, flechas en las líneas de un solo sentido y resaltado de rutas y recorridos.
- **Inspección de las estructuras internas** (tabla hash, grafo, Trie y Union-Find) desde el menú.

## Ejecución

Requisito: **SDK de .NET 8 o superior** (`dotnet --version`). El proyecto toma la versión de tu SDK y no usa
paquetes de NuGet.

```bash
dotnet run
```

Se ejecuta desde la carpeta raíz del repositorio (donde está `ElectricGrid.csproj`) y no necesita parámetros.
Funciona en cualquier terminal de **80×24 o más**. Si los recuadros se ven mal, usar la opción «ASCII / Unicode» del
menú o iniciar con `dotnet run -- --ascii`. Si cambias de versión de .NET y aparecen errores, borra las carpetas
generadas con `rm -rf bin obj`.

## Estructuras de datos

| Estructura | Archivo | Qué resuelve en el problema | Responsable |
|---|---|---|---|
| Tabla Hash | `DataStructures/HashTable.cs` | Localiza una subestación por su código en O(1) promedio | Integrante A |
| Grafo (BFS, DFS, Dijkstra) | `DataStructures/Graph.cs` | Guarda las líneas dirigidas, hace los recorridos y calcula la ruta óptima | Integrante B |
| Union-Find | `DataStructures/DisjointSet.cs` | Mantiene las zonas eléctricas y valida la conectividad | Integrante C |
| Trie | `DataStructures/Trie.cs` | Busca subestaciones por prefijo del nombre | Integrante D |

Cada estructura se eligió por la operación más frecuente que atiende:

- **Tabla hash:** toda consulta empieza convirtiendo un código en el registro completo, y esa búsqueda debe ser inmediata.
- **Grafo con listas de adyacencia:** una red eléctrica es dispersa (pocas líneas por subestación), por lo que
  las listas ahorran memoria frente a una matriz y los recorridos solo visitan vecinos reales.
- **Union-Find:** responder «¿misma zona?» en tiempo casi constante evita ejecutar el algoritmo de ruta cuando es
  imposible que exista una, y permite analizar una línea nueva sin recorrer el grafo.
- **Trie:** el costo de una búsqueda por prefijo depende del largo del texto y de las coincidencias, no de cuántas
  subestaciones haya registradas.

## Cómo trabajan juntas

Las cuatro estructuras participan en un mismo flujo, por ejemplo al pedir una ruta:

1. **Trie:** la búsqueda por nombre devuelve los códigos de las subestaciones que empiezan con el texto escrito.
2. **Tabla hash:** cada código se convierte en el registro completo de la subestación.
3. **Union-Find:** se verifica que origen y destino estén en la misma zona; si no lo están, se informa y el
   algoritmo de ruta ni siquiera se ejecuta.
4. **Grafo:** Dijkstra calcula la ruta óptima sobre las líneas dirigidas. Si comparten zona pero el sentido de las
   líneas impide llegar, se informa «sin camino dirigido».

Al registrar una subestación se crean a la vez su registro en la tabla hash, su vértice en el grafo, su elemento
en Union-Find y su entrada en el Trie; el índice de vértice, el elemento de Union-Find y la posición del registro
son siempre el mismo número. Al registrar una línea se agrega la arista y se unen las zonas de sus extremos.

## Uso de la aplicación

La pantalla tiene el **menú a la izquierda**, el **grafo a la derecha**, una línea con los datos de la
subestación seleccionada y una barra de estado con las teclas disponibles. `Tab` cambia el foco entre ambos paneles.

### Menú

| Sección | Opciones |
|---|---|
| Datos | Cargar ejemplo, cargar subestaciones, cargar líneas, borrar todo |
| Subestaciones | Registrar, buscar por código, buscar por nombre, eliminar, listar |
| Líneas | Registrar, proponer línea nueva, eliminar, listar |
| Análisis | Ruta óptima, recorrido BFS, recorrido DFS, ¿misma zona eléctrica?, listar zonas, reporte / exportar |
| Herramientas | Ver estructuras internas, ASCII / Unicode, ayuda, salir |

Los formularios aceptan el código de la subestación o su nombre, y muestran en verde la subestación que
corresponde a lo escrito. Escribir sobre un valor precargado lo reemplaza.

### Teclas

| Tecla | Acción |
|---|---|
| `Tab` | Cambiar el foco entre menú y grafo |
| `↑ ↓` en el menú, `Enter` | Moverse y ejecutar la opción |
| Flechas en el grafo | Seleccionar la subestación más cercana en esa dirección |
| `Enter` en el grafo | Detalle de la subestación seleccionada y sus líneas |
| `B` / `D` | BFS / DFS desde la seleccionada (se resalta en amarillo) |
| `R` | Ruta óptima desde la seleccionada |
| `/` | Buscar subestación por nombre (desde cualquier panel) |
| `C` / `L` / `F` | Quitar resaltado / cambiar etiquetas (completas o cortas) / grafo a pantalla completa |
| `?` / `Q` | Ayuda / salir |

### Lectura del dibujo

- `[SUB-001]` es una subestación, con el color de su zona eléctrica.
- `───` es una línea de doble sentido; `──>` es una línea de un solo sentido, con la flecha junto al destino.
- Donde dos líneas se cruzan aparece una cruz.
- El amarillo marca el recorrido o la ruta resaltados; el nodo con fondo blanco es el seleccionado.

La posición de los nodos se calcula automáticamente con un algoritmo de fuerzas (Fruchterman-Reingold). La
interfaz se adapta al redimensionar la ventana y usa etiquetas cortas cuando hay muchos nodos. Con más de unas 40
subestaciones o muchas líneas cruzadas el dibujo se vuelve difícil de leer; en ese caso conviene usar las listas.

## Reglas y validaciones

- Los códigos se normalizan (sin espacios en los extremos y en mayúsculas). No puede haber dos subestaciones con el mismo código.
- El nombre no puede estar vacío y la capacidad debe ser positiva.
- Una línea requiere dos subestaciones existentes y distintas, longitud positiva y costo de mantenimiento no negativo.
- No se permite repetir una línea en el mismo sentido. Los pesos negativos se rechazan, lo que garantiza que Dijkstra funcione.
- Al eliminar una subestación se eliminan también sus líneas, y las zonas se recalculan.

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

- El encabezado es opcional (en español o inglés). Se ignoran las líneas vacías y las que empiezan con `#`.
- Los decimales llevan punto. Los nombres no pueden contener comas.
- `doble_sentido` es opcional (por defecto `true`) y acepta `true`, `false`, `si`, `no`, `1` y `0`.
- Se carga primero el archivo de subestaciones y después el de líneas.
- El archivo se busca en la ruta indicada, junto al ejecutable y en su carpeta `Data`.
- Las filas inválidas se omiten y se reportan con el nombre del archivo y el número de línea.

## Demostración con los datos de ejemplo

La opción **Cargar ejemplo** carga 17 subestaciones y 17 líneas que forman **3 zonas**: una red principal
(SUB-001 a SUB-014), un par de subestaciones solares (SUB-015 y SUB-016) y una subestación aislada (SUB-017).

| Qué probar | Resultado esperado |
|---|---|
| Ruta SUB-001 → SUB-005 optimizando **costo** | SUB-001 → SUB-004 → SUB-005, costo 3400 |
| Ruta SUB-001 → SUB-005 optimizando **longitud** | SUB-001 → SUB-014 → SUB-004 → SUB-005, 7.5 km |
| Ruta SUB-001 → SUB-015 | «Zonas distintas»: no se ejecuta el algoritmo de ruta |
| Ruta SUB-005 → SUB-001 | Misma zona, pero «sin camino dirigido» (SUB-005 no tiene líneas de salida) |
| Proponer línea SUB-017 → SUB-001 | Uniría dos zonas distintas |
| Proponer línea SUB-005 → SUB-006 | Misma zona: solo añade redundancia |
| BFS desde SUB-001 | Alcanza 14 subestaciones |

## Arquitectura

Un solo proyecto, organizado en carpetas:

```
ElectricGrid.csproj
Program.cs                  Único punto de entrada
Collections/                SinglyLinkedList<T> y ArrayUtils (lista enlazada y utilidades de arreglos propias)
DataStructures/             HashTable, Graph, DisjointSet, Trie
Models/                     Substation, PowerLine y objetos de resultado
Services/                   IPowerGridService, PowerGridService (integra las estructuras)
IO/                         GridDataLoader (carga de CSV/TXT)
Reporting/                  GridReportBuilder (reporte en texto)
Tui/                        Pantalla, menú, ventanas y dibujo del grafo
Data/                       subestaciones.csv, lineas.csv (ejemplos)
```

Las carpetas de lógica (`Collections`, `DataStructures`, `Models`, `Services`, `IO`, `Reporting`) no usan la
consola ni dependen de `Tui`: toda operación devuelve objetos de resultado. Solo `Tui/` y `Program.cs` interactúan
con la terminal. Para una interfaz gráfica futura bastaría reemplazar `Tui/` y usar `IPowerGridService`.

## Implementación propia

- No se usa `List<T>`, `Dictionary`, `HashSet`, `SortedSet`, `PriorityQueue` ni LINQ.
- Las estructuras solo usan arreglos y nodos propios. `SinglyLinkedList<T>` reemplaza a `List<T>` para acumular
  resultados y `ArrayUtils` reemplaza a `Array.Resize` y `Array.Clear`.
- El único tipo de colección nativo es `Queue<T>`, exclusivamente como cola de visita en el recorrido BFS
  (`Graph.BreadthFirstTraversal`).
- Lo nativo se limita a soporte auxiliar: lectura de archivos, manejo de cadenas y consola.

## Complejidad (Big-O)

n = elementos, V = vértices, E = aristas, k = largo del texto, p = largo del prefijo, m = coincidencias,
α = inversa de Ackermann.

| Estructura | Operación | Complejidad | Alternativa nativa |
|---|---|---|---|
| Tabla hash | insertar / buscar / eliminar | O(1) promedio, O(n) peor caso | `Dictionary`: igual |
| Grafo (listas de adyacencia) | agregar arista | O(grado) | — |
| | BFS / DFS | O(V + E) | — |
| | memoria | O(V + E) (una matriz usaría O(V²)) | — |
| | Dijkstra con cola de prioridad binaria | O((V + E) log V) | `PriorityQueue`: igual |
| Union-Find | Find / Union (compresión + rango) | O(α(n)) amortizado | BFS por consulta: O(V + E) |
| Trie | insertar | O(k) | — |
| | búsqueda por prefijo | O(p + m) | recorrer una colección: O(n·p) |
| Lista enlazada simple | agregar al final / copiar a arreglo | O(1) / O(n) | `List<T>`: O(1) amortizado / O(n) |

## Notas de diseño

- Union-Find modela conectividad **no dirigida**: una línea de un solo sentido igual une dos subestaciones en una
  zona. Solo el grafo decide si existe un camino **dirigido**; por eso hay dos fallos distintos: «zonas distintas» y
  «sin camino dirigido».
- Union-Find no puede separar conjuntos: al eliminar una línea o una subestación se reconstruyen las zonas a partir
  de las aristas restantes.
- Los vértices eliminados conservan su índice (quedan inactivos), así los índices compartidos entre grafo,
  Union-Find y registro nunca cambian.
- Los resultados que solo se acumulan y luego se recorren (órdenes de BFS y DFS, líneas, errores de carga) usan la
  lista enlazada con referencia a la cola: insertar al final cuesta O(1). El registro de subestaciones por vértice
  es en cambio un arreglo, porque se consulta por índice en cada ruta y una lista enlazada costaría O(n) por consulta.
- El recorrido DFS es iterativo, con una pila basada en arreglos, para que una red grande no desborde la pila de llamadas.
