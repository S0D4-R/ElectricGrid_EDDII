# Municipal Electrical Grid Management

Console application in C# (.NET 8) for Laboratory Project No. 2 – *Estructura de Datos II*.
Scenario: **Platform for managing a municipal electrical grid** (substations and distribution lines).

> **PARTIAL VERSION.** The **Trie** (name autocomplete) and **Dijkstra's algorithm** (optimal route) are
> intentionally left as clearly marked empty slots for teammates. Everything else (Hash Table, Graph with
> BFS/DFS, Union-Find, menu, file loading, reports) is implemented and working. See
> [Pending work](#pending-work-handoff-for-teammates).

Hash Table, Graph and Union-Find are implemented from scratch and work in one coordinated flow:

| Structure | File | Responsibility in the problem | Owner |
|---|---|---|---|
| Hash Table (separate chaining, own djb2 hash, auto-rehash) | `DataStructures/HashTable.cs` | Locate a substation by code in O(1) average | Member A |
| Graph (adjacency lists, BFS, DFS) - **Dijkstra pending** | `DataStructures/Graph.cs` | Lines, directed/undirected relations, traversals; route calculation once Dijkstra exists | Member B + Dijkstra owner |
| Union-Find (path compression + union by rank) | `DataStructures/DisjointSet.cs` | Electrical zones; validate connectivity before Dijkstra / before approving a line | Member C |
| Trie - **PENDING (skeleton only)** | `DataStructures/Trie.cs` | Suggest substation names while typing | Trie owner |

> Replace the "Developed by" placeholders in each structure's XML comment with the real team member names.

## Integrated flow (`Services/PowerGridService.cs`)

```
Operator types "sur"
  Trie ............. suggests names + codes (prefix search)        [PENDING]
  Hash Table ....... code -> complete Substation record
  Union-Find ....... same electrical zone?  no -> stop (Dijkstra is never executed)
  Graph/Dijkstra ... cheapest or shortest directed route          [Dijkstra PENDING]
```

Proposing a new line follows the scenario rule: Union-Find first reports whether both substations
already belong to the same zone (the line would only add redundancy) or belong to different zones
(the line would merge them); the operator then approves or rejects it.

## Project layout

```
ElectricGrid.sln
src/
  ElectricGrid.Core/              <- UI-independent library (reusable by a GUI)
    Collections/                  ArrayBuffer<T> (own helper; no List<T>). MinHeap to be added by the Dijkstra owner
    DataStructures/               HashTable, Graph, DisjointSet, Trie (skeleton)
    Models/                       Substation, PowerLine and result DTOs
    Services/                     IPowerGridService, IStructureInspector, PowerGridService
    IO/                           GridDataLoader (CSV/TXT import, path resolution)
    Reporting/                    GridReportBuilder (text report, export)
  ElectricGrid.ConsoleApp/        <- Console front end only
    UI/                           MainMenu, ConsoleInput (live autocomplete), ConsolePresenter
    Data/                         substations.csv, lines.csv (sample data, copied to the output folder)
```

### Ready for a future GUI
* `ElectricGrid.Core` has **no** `Console` calls and no UI dependency.
* A front end only needs `IPowerGridService` (operations) and optionally `IStructureInspector` (diagnostics).
* Every operation returns a result object (`OperationResult`, `RouteResult`, `LineProposalResult`, ...)
  instead of printing, so WPF / WinForms / MAUI / Avalonia / ASP.NET can render them freely.
* To add a GUI: create a new project, reference `ElectricGrid.Core`, instantiate `new PowerGridService()`.

## Build and run

```bash
dotnet build
dotnet run --project src/ElectricGrid.ConsoleApp
```

Choose option **3** in the menu to load the sample data from the `Data` folder.
While the Trie is pending, substations are selected by **code** (e.g. `SUB-004`); options 7 and 13 report
that their feature is pending instead of failing.

## Input files (.csv / .txt)

`substations.csv`
```
code,name,capacity_mw
SUB-001,Central Principal,250
```

`lines.csv`
```
source_code,target_code,length_km,maintenance_cost,bidirectional
SUB-001,SUB-002,4.5,1800,false     <- one-way: SUB-001 -> SUB-002
SUB-001,SUB-014,1.8,900,true       <- two-way
```
* Header row optional; blank lines and lines starting with `#` are ignored.
* `bidirectional` is optional (default `true`); accepts `true/false/1/0/yes/no/si`.
* Use `.` as decimal separator; commas inside names are not supported.
* Paths are searched as typed, then next to the executable, then in its `Data` folder.
* Invalid rows are skipped and reported with file name and line number.

## Menu overview

| # | Option | Structures involved |
|---|---|---|
| 1-3 | Load substations / lines / sample data | Hash, Graph, Union-Find, Trie |
| 4-8 | Register, find, remove, list substations; option 7 autocomplete (**pending**) | Hash (+ Trie pending) |
| 9-12 | Register, propose (zone check), remove, list lines | Graph, Union-Find |
| 13 | Optimal route (cost or length) - **pending Dijkstra**; Hash and Union-Find steps already run | Hash -> Union-Find -> Dijkstra |
| 14-15 | BFS / DFS from a substation | Graph |
| 16-17 | Same-zone check / list zones | Union-Find |
| 18 | Summary report and text export | all |
| 19-22 | Inspect Hash / Graph / Trie (pending) / Union-Find internals | all |

## Requirement checklist

* **Own implementation**: no `List<T>`, `Dictionary`, `HashSet`, `SortedSet` or `PriorityQueue` anywhere.
  Native types are used only for files, strings and `Queue<T>` as the BFS visiting-order queue.
* **Operations done**: insert / search / delete / traverse / print for Hash Table, Graph and Union-Find,
  BFS, DFS, `Find` with path compression and `Union` by rank.
* **Operations pending**: Dijkstra's algorithm and the Trie prefix search (see below).
* **Dynamic data**: loaded from files or typed in the menu; nothing hard-coded.
* **Interactive menu** covering all structures.

## Complexity analysis (Big-O)

V = vertices, E = edges, n = stored items, k = key length, m = matches returned, alpha = inverse Ackermann.

### Hash Table
| Operation | Average | Worst | .NET `Dictionary<K,V>` |
|---|---|---|---|
| Insert | O(1) amortized | O(n) | O(1) amortized / O(n) |
| Search / Contains | O(1) | O(n) | O(1) / O(n) |
| Delete | O(1) | O(n) | O(1) / O(n) |
| Traverse / print | O(capacity + n) | same | O(n) |
| Rehash | O(n), rare | - | similar |

Comparable to the native class; versus `List<T>` search O(n) it is much faster for code lookups.

### Graph (adjacency lists)
| Operation | Cost | Note |
|---|---|---|
| Add vertex | O(1) amortized | |
| Add edge | O(deg(u)) | duplicate check |
| Remove edge | O(deg(u)) | |
| Remove vertex | O(V + E) | scans incoming edges |
| BFS / DFS | O(V + E) | adjacency matrix would be O(V^2) |
| Dijkstra | *pending* | expected O((V + E) log V) with a binary heap; O(V^2) with an array scan |
| Memory | O(V + E) | matrix: O(V^2) |

Electrical grids are sparse, so lists are the right trade-off. .NET has no graph class;
the usual native alternative is `Dictionary<int, List<Edge>>` with `PriorityQueue`, same asymptotics.

### Union-Find
| Operation | Cost |
|---|---|
| MakeSet | O(1) amortized |
| Find (path compression) | O(alpha(n)) amortized |
| Union (by rank) | O(alpha(n)) amortized |
| Connected? | O(alpha(n)) amortized |
| Rebuild after deletion | O(V + E * alpha(V)) |

Alternative without Union-Find: BFS/DFS per query, O(V + E) every time. Union-Find answers
connectivity in practically constant time after incremental unions.
`HashSet<int>`-per-component schemes cost O(n) to merge.

### Trie
*Pending - to be written by the Trie owner.* Expected: insert / contains / delete O(k * sigma) and prefix
search O(p * sigma + m), independent of the number of stored names, versus O(n * p) when scanning a
collection. Compare against `List<string>` / `SortedSet<string>` in the technical manual.

## Design notes worth knowing for the defense

* Union-Find models **undirected** connectivity. A one-way line still puts two substations in the
  same zone; only the graph can tell whether a *directed* path exists, which is why
  `RouteStatus.NoDirectedPath` exists besides `RouteStatus.DifferentZones`.
* Union-Find cannot split sets, so deleting a line or substation resets it and re-applies the
  unions from the remaining edges (`RebuildZones`).
* Removed vertices keep their index slot (marked inactive) so the indices shared by the graph,
  Union-Find and registry never shift.
* Negative weights are rejected when a line is created, so Dijkstra can assume non-negative weights.
* Substation codes are normalized (trim + upper-case) before reaching the hash table.

## Pending work (handoff for teammates)

Search the whole solution for `TODO [` to find every spot. Nothing else needs to change.

### Dijkstra - `TODO [DIJKSTRA]`
| Where | What to do |
|---|---|
| `Graph.cs` -> `ShortestPath(...)` | Implement the algorithm and remove the `NotImplementedException`. Helpers already available: `_adjacency`, `GraphEdge.WeightFor(kind)`, `BuildPath(...)`, `PathResult.NotFound()` |
| `Collections/MinHeap.cs` (new file) | Write your own priority queue (`PriorityQueue<T,P>` is forbidden). Operations needed: insert, extract-min |
| `PowerGridService.FindOptimalRoute` | **No change needed.** Hash lookup and Union-Find validation already run before the call, and the result is already mapped to substations |
| Console menu option 13 | **No change needed.** Currently shows "Feature pending implementation" |

### Trie - `TODO [TRIE]`
| Where | What to do |
|---|---|
| `Trie.cs` | Implement `Insert`, `Contains`, `Remove`, `SearchByPrefix`, `GetAll`, `Describe`, `Count`, `NodeCount` (signatures are fixed) |
| `PowerGridService.cs` | Uncomment the `_nameIndex` field; uncomment `Insert` in `RegisterSubstation`, `Remove` in `RemoveSubstation`, `new Trie()` in `Clear`; replace the body of `SuggestSubstations` with the commented two-step code; set `SupportsAutocomplete` to `true`; make `DescribeTrie` return `_nameIndex.Describe()` |
| Console | **No change needed.** Live suggestions (type + TAB), name-based selection and menu options 7 / 21 switch on automatically when `SupportsAutocomplete` is `true` |
