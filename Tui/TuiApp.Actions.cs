using System;
using System.Globalization;
using ElectricGrid.Collections;
using ElectricGrid.DataStructures;
using ElectricGrid.Models;
using ElectricGrid.Reporting;

namespace ElectricGrid.Tui
{
    internal sealed partial class TuiApp
    {
        private const string DefaultSubstationsFile = "subestaciones.csv";
        private const string DefaultLinesFile = "lineas.csv";
        private const string DefaultReportFile = "reporte_red.txt";
        private const int SearchResultLimit = 10;

        private MenuItem[] BuildMenu()
        {
            return new[]
            {
                new MenuItem("Datos", null),
                new MenuItem("Cargar ejemplo", ActionLoadSample),
                new MenuItem("Cargar subestaciones", ActionLoadSubstations),
                new MenuItem("Cargar líneas", ActionLoadLines),
                new MenuItem("Borrar todo", ActionClear),
                new MenuItem("Subestaciones", null),
                new MenuItem("Registrar subestación", ActionRegisterSubstation),
                new MenuItem("Buscar por código", ActionFindSubstation),
                new MenuItem("Buscar por nombre", ActionSearch),
                new MenuItem("Eliminar subestación", ActionRemoveSubstation),
                new MenuItem("Listar subestaciones", ActionListSubstations),
                new MenuItem("Líneas", null),
                new MenuItem("Registrar línea", ActionRegisterLine),
                new MenuItem("Proponer línea nueva", ActionProposeLine),
                new MenuItem("Eliminar línea", ActionRemoveLine),
                new MenuItem("Listar líneas", ActionListLines),
                new MenuItem("Análisis", null),
                new MenuItem("Ruta óptima", ActionRoute),
                new MenuItem("Recorrido BFS", () => ActionTraversal(true)),
                new MenuItem("Recorrido DFS", () => ActionTraversal(false)),
                new MenuItem("¿Misma zona eléctrica?", ActionSameZone),
                new MenuItem("Listar zonas", ActionListZones),
                new MenuItem("Reporte / exportar", ActionReport),
                new MenuItem("Herramientas", null),
                new MenuItem("Ver estructuras", ActionInspect),
                new MenuItem("ASCII / Unicode", ActionToggleGlyphs),
                new MenuItem("Ayuda", ActionHelp),
                new MenuItem("Salir", ActionQuit)
            };
        }

        private void ActionLoadSample()
        {
            AfterLoad(_loader.LoadAll(DefaultSubstationsFile, DefaultLinesFile));
        }

        private void ActionLoadSubstations()
        {
            string[]? values = ShowForm("Cargar subestaciones", new[]
            {
                new FormField("Archivo", DefaultSubstationsFile, FieldKind.Text, "formato: codigo,nombre,capacidad_mw")
            });

            if (values != null)
            {
                AfterLoad(_loader.LoadSubstations(values[0]));
            }
        }

        private void ActionLoadLines()
        {
            string[]? values = ShowForm("Cargar líneas", new[]
            {
                new FormField("Archivo", DefaultLinesFile, FieldKind.Text, "formato: origen,destino,longitud_km,costo,doble_sentido")
            });

            if (values != null)
            {
                AfterLoad(_loader.LoadLines(values[0]));
            }
        }

        /// <summary>Muestra el resultado de una carga de archivo y actualiza el grafo.</summary>
        private void AfterLoad(LoadReport report)
        {
            _snapshotDirty = true;
            ClearHighlight();

            ArrayBuffer<string> lines = new ArrayBuffer<string>();
            lines.Add($"Subestaciones cargadas: {report.SubstationsLoaded}");
            lines.Add($"Líneas cargadas:        {report.LinesLoaded}");
            lines.Add($"Filas omitidas:         {report.RowsSkipped}");

            int shown = Math.Min(report.Errors.Length, 8);
            for (int i = 0; i < shown; i++)
            {
                lines.Add("! " + report.Errors[i]);
            }

            if (report.Errors.Length > shown)
            {
                lines.Add($"... y {report.Errors.Length - shown} más");
            }

            bool ok = report.FileFound && report.RowsSkipped == 0;
            bool loaded = report.SubstationsLoaded + report.LinesLoaded > 0;
            ConsoleColor color = ok ? ConsoleColor.Green : (loaded ? ConsoleColor.Yellow : ConsoleColor.Red);
            ShowMessage(ok ? "Carga completa" : (loaded ? "Carga con problemas" : "Falló la carga"), lines.ToArray(), color);
            SetStatus($"Se cargaron {report.SubstationsLoaded} subestación(es) y {report.LinesLoaded} línea(s).", ok ? ConsoleColor.Green : ConsoleColor.Yellow);
        }

        private void ActionClear()
        {
            if (Confirm("Se eliminarán todas las subestaciones y líneas. ¿Continuar?", false))
            {
                _service.Clear();
                ClearHighlight();
                _selectedCode = null;
                _snapshotDirty = true;
                SetStatus("Se borraron todos los datos.", ConsoleColor.Green);
            }
        }

        private void ActionRegisterSubstation()
        {
            string[]? v = ShowForm("Registrar subestación", new[]
            {
                new FormField("Código", string.Empty, FieldKind.Text, "debe ser único"),
                new FormField("Nombre", string.Empty, FieldKind.Text),
                new FormField("Capacidad (MW)", string.Empty, FieldKind.Text, "número positivo")
            });

            if (v == null)
            {
                return;
            }

            if (!TryNumber(v[2], out double capacity))
            {
                ShowMessage("Capacidad inválida", new[] { "La capacidad debe ser un número." }, ConsoleColor.Red);
                return;
            }

            ReportResult(_service.RegisterSubstation(v[0], v[1], capacity));
            _snapshotDirty = true;
        }

        private void ActionFindSubstation()
        {
            string[]? v = ShowForm("Buscar subestación", new[] { SubstationField("Código") });
            if (v == null)
            {
                return;
            }

            Substation? found = _service.FindSubstation(v[0]);
            if (found == null)
            {
                ShowMessage("No encontrada", new[] { $"No existe una subestación con el código '{v[0].ToUpperInvariant()}'." }, ConsoleColor.Yellow);
                return;
            }

            _snapshotDirty = true;
            _selectedCode = found.Code;
            _focus = Focus.Graph;
            ShowNodeDetails(found);
        }

        /// <summary>Busca subestaciones por parte del nombre o del código y selecciona en el grafo la que se elija.</summary>
        private void ActionSearch()
        {
            string[]? v = ShowForm("Buscar subestación", new[]
            {
                new FormField("Texto", string.Empty, FieldKind.Text, "parte del nombre o del código")
            });

            if (v == null)
            {
                return;
            }

            if (v[0].Length == 0)
            {
                ShowMessage("Búsqueda vacía", new[] { "Escriba al menos un carácter." }, ConsoleColor.Yellow);
                return;
            }

            Substation[] results = _service.SearchSubstations(v[0], SearchResultLimit);
            if (results.Length == 0)
            {
                ShowMessage("Sin coincidencias", Wrap($"No se encontraron subestaciones para '{v[0]}'.", 56), ConsoleColor.Yellow);
                return;
            }

            string[] options = new string[results.Length];
            for (int i = 0; i < results.Length; i++)
            {
                options[i] = $"{results[i].Code,-9} {results[i].Name}";
            }

            string title = results.Length == SearchResultLimit
                ? $"Resultados (primeros {SearchResultLimit})"
                : $"Resultados ({results.Length})";

            int choice = Pick(title, options);
            if (choice < 0)
            {
                return;
            }

            _selectedCode = results[choice].Code;
            _snapshotDirty = true;
            _focus = Focus.Graph;
            SetStatus($"Seleccionada {results[choice].Code}.", ConsoleColor.Green);
        }

        private void ActionRemoveSubstation()
        {
            string[]? v = ShowForm("Eliminar subestación", new[] { SubstationField("Subestación") });
            if (v == null)
            {
                return;
            }

            string? code = ResolveCode(v[0]);
            if (code == null)
            {
                ShowMessage("No encontrada", new[] { "Esa subestación no existe." }, ConsoleColor.Yellow);
                return;
            }

            if (Confirm($"¿Eliminar {code} y todas las líneas conectadas a ella?", false))
            {
                ReportResult(_service.RemoveSubstation(code));
                ClearHighlight();
                _snapshotDirty = true;
            }
        }

        private void ActionListSubstations()
        {
            Substation[] all = _service.GetAllSubstations();
            ArrayBuffer<string> lines = new ArrayBuffer<string>();
            lines.Add($"{"CÓDIGO",-10} {"NOMBRE",-32} {"CAPACIDAD (MW)",14}");
            lines.Add(new string('-', 58));
            for (int i = 0; i < all.Length; i++)
            {
                lines.Add($"{all[i].Code,-10} {all[i].Name,-32} {all[i].CapacityMw,14:F1}");
            }

            lines.Add(string.Empty);
            lines.Add($"Total: {all.Length}");
            ShowText("Subestaciones", lines.ToArray());
        }

        private void ActionRegisterLine()
        {
            string[]? v = ShowForm("Registrar línea", new[]
            {
                SubstationField("Origen"),
                SubstationField("Destino", string.Empty),
                new FormField("Longitud (km)", string.Empty, FieldKind.Text, "número positivo"),
                new FormField("Costo mantenim.", string.Empty, FieldKind.Text, "cero o más"),
                new FormField("¿Doble sentido?", "s", FieldKind.Text, "s = ambos sentidos, n = solo origen -> destino")
            });

            if (v != null)
            {
                RegisterLineFromValues(v[0], v[1], v[2], v[3], v[4]);
            }
        }

        private void RegisterLineFromValues(string origin, string destination, string length, string cost, string twoWay)
        {
            string? a = ResolveCode(origin);
            string? b = ResolveCode(destination);
            if (a == null || b == null)
            {
                ShowMessage("Subestación desconocida", new[] { "El origen y el destino deben ser subestaciones existentes." }, ConsoleColor.Red);
                return;
            }

            if (!TryNumber(length, out double lengthKm) || !TryNumber(cost, out double maintenance))
            {
                ShowMessage("Número inválido", new[] { "La longitud y el costo deben ser números." }, ConsoleColor.Red);
                return;
            }

            ReportResult(_service.RegisterLine(new PowerLine(a, b, lengthKm, maintenance, IsAffirmative(twoWay))));
            _snapshotDirty = true;
        }

        /// <summary>Verifica con Union-Find si ambas subestaciones ya comparten zona antes de aprobar una línea nueva.</summary>
        private void ActionProposeLine()
        {
            string[]? pair = ShowForm("Proponer línea nueva", new[]
            {
                SubstationField("Origen"),
                SubstationField("Destino", string.Empty)
            });

            if (pair == null)
            {
                return;
            }

            string? a = ResolveCode(pair[0]);
            string? b = ResolveCode(pair[1]);
            if (a == null || b == null)
            {
                ShowMessage("Subestación desconocida", new[] { "El origen y el destino deben ser subestaciones existentes." }, ConsoleColor.Red);
                return;
            }

            LineProposalResult proposal = _service.EvaluateProposedLine(a, b);
            if (!proposal.IsValid)
            {
                ShowMessage("Propuesta inválida", Wrap(proposal.Message, 56), ConsoleColor.Red);
                return;
            }

            string text = proposal.Message + (proposal.DirectLineExists ? " Nota: ya existe una línea directa en algún sentido." : string.Empty);
            ConsoleColor accent = proposal.Verdict == LineProposalVerdict.JoinsDifferentZones ? ConsoleColor.Green : ConsoleColor.Yellow;
            ShowMessage("Análisis de zonas (Union-Find)", Wrap(text, 56), accent);

            if (!Confirm("¿Aprobar y registrar esta línea?", false))
            {
                SetStatus("Línea no aprobada.", ConsoleColor.Gray);
                return;
            }

            string[]? details = ShowForm($"Línea nueva {a} - {b}", new[]
            {
                new FormField("Longitud (km)", string.Empty, FieldKind.Text, "número positivo"),
                new FormField("Costo mantenim.", string.Empty, FieldKind.Text, "cero o más"),
                new FormField("¿Doble sentido?", "s", FieldKind.Text, "s = ambos sentidos, n = solo origen -> destino")
            });

            if (details != null)
            {
                RegisterLineFromValues(a, b, details[0], details[1], details[2]);
            }
        }

        private void ActionRemoveLine()
        {
            string[]? v = ShowForm("Eliminar línea", new[]
            {
                SubstationField("Origen"),
                SubstationField("Destino", string.Empty),
                new FormField("¿Ambos sentidos?", "s", FieldKind.Text)
            });

            if (v == null)
            {
                return;
            }

            string? a = ResolveCode(v[0]);
            string? b = ResolveCode(v[1]);
            if (a == null || b == null)
            {
                ShowMessage("Subestación desconocida", new[] { "Ambas subestaciones deben existir." }, ConsoleColor.Red);
                return;
            }

            ReportResult(_service.RemoveLine(a, b, IsAffirmative(v[2])));
            ClearHighlight();
            _snapshotDirty = true;
        }

        private void ActionListLines()
        {
            PowerLine[] all = _service.GetAllLines();
            ArrayBuffer<string> lines = new ArrayBuffer<string>();
            lines.Add($"{"DESDE",-10} {"DIR",-4} {"HASTA",-10} {"LONGITUD (km)",13} {"COSTO MANT.",12}");
            lines.Add(new string('-', 54));
            for (int i = 0; i < all.Length; i++)
            {
                string arrow = all[i].IsBidirectional ? "<->" : "->";
                lines.Add($"{all[i].SourceCode,-10} {arrow,-4} {all[i].TargetCode,-10} {all[i].LengthKm,13:F1} {all[i].MaintenanceCost,12:F0}");
            }

            lines.Add(string.Empty);
            lines.Add($"Total: {all.Length}");
            ShowText("Líneas", lines.ToArray());
        }

        /// <summary>Calcula la ruta óptima (tabla hash, Union-Find y Dijkstra) y resalta el camino en el grafo.</summary>
        private void ActionRoute()
        {
            string[]? v = ShowForm("Ruta óptima", new[]
            {
                SubstationField("Origen"),
                SubstationField("Destino", string.Empty),
                new FormField("Optimizar (1/2)", "1", FieldKind.Text, "1 = menor costo, 2 = menor longitud")
            });

            if (v == null)
            {
                return;
            }

            string? a = ResolveCode(v[0]);
            string? b = ResolveCode(v[1]);
            if (a == null || b == null)
            {
                ShowMessage("Subestación desconocida", new[] { "El origen y el destino deben ser subestaciones existentes." }, ConsoleColor.Red);
                return;
            }

            EdgeWeightKind criterion = v[2] == "2" ? EdgeWeightKind.Length : EdgeWeightKind.MaintenanceCost;
            RouteResult route = _service.FindOptimalRoute(a, b, criterion);

            if (!route.Found)
            {
                ClearHighlight();
                ShowMessage("Sin ruta", Wrap(route.Message, 56), ConsoleColor.Yellow);
                return;
            }

            _highlightCodes = new string[route.Path.Length];
            for (int i = 0; i < route.Path.Length; i++)
            {
                _highlightCodes[i] = route.Path[i].Code;
            }

            _highlightIsPath = true;
            _highlightTitle = criterion == EdgeWeightKind.Length ? "Ruta más corta" : "Ruta más económica";
            _focus = Focus.Graph;

            SetStatus($"{route.Path.Length - 1} tramo(s) | {route.TotalLengthKm:F1} km | costo {route.TotalMaintenanceCost:F0}", ConsoleColor.Green);
        }

        private void ActionTraversal(bool breadthFirst)
        {
            string[]? v = ShowForm(breadthFirst ? "Recorrido BFS" : "Recorrido DFS", new[] { SubstationField("Inicio") });
            if (v == null)
            {
                return;
            }

            string? start = ResolveCode(v[0]);
            if (start == null)
            {
                ShowMessage("Subestación desconocida", new[] { "La subestación inicial debe existir." }, ConsoleColor.Red);
                return;
            }

            ApplyTraversal(start, breadthFirst);
            _focus = Focus.Graph;
        }

        private void RunTraversalFromSelected(bool breadthFirst)
        {
            if (_selectedCode != null)
            {
                ApplyTraversal(_selectedCode, breadthFirst);
            }
        }

        private void ApplyTraversal(string startCode, bool breadthFirst)
        {
            TraversalResult result = breadthFirst ? _service.TraverseBreadthFirst(startCode) : _service.TraverseDepthFirst(startCode);
            if (!result.Success)
            {
                ShowMessage("Falló el recorrido", Wrap(result.Message, 56), ConsoleColor.Red);
                return;
            }

            _highlightCodes = new string[result.Order.Length];
            for (int i = 0; i < result.Order.Length; i++)
            {
                _highlightCodes[i] = result.Order[i].Code;
            }

            _highlightIsPath = false;
            string kind = breadthFirst ? "BFS" : "DFS";
            _highlightTitle = $"{kind} desde {startCode}";
            SetStatus($"{kind} alcanzó {result.Order.Length} nodos.", ConsoleColor.Green);
        }

        private void ActionSameZone()
        {
            string[]? v = ShowForm("¿Misma zona eléctrica?", new[]
            {
                SubstationField("Primera"),
                SubstationField("Segunda", string.Empty)
            });

            if (v == null)
            {
                return;
            }

            string? a = ResolveCode(v[0]);
            string? b = ResolveCode(v[1]);
            if (a == null || b == null)
            {
                ShowMessage("Subestación desconocida", new[] { "Ambas subestaciones deben existir." }, ConsoleColor.Red);
                return;
            }

            ConnectivityResult result = _service.CheckSameZone(a, b);
            ShowMessage("Zona eléctrica (Union-Find)", Wrap(result.Message, 56), result.Connected ? ConsoleColor.Green : ConsoleColor.Yellow);
        }

        private void ActionListZones()
        {
            ElectricalZone[] zones = _service.GetZones();
            ArrayBuffer<string> lines = new ArrayBuffer<string>();

            for (int z = 0; z < zones.Length; z++)
            {
                lines.Add($"Zona {zones[z].Id}: {zones[z].Members.Length} subestación(es), {zones[z].TotalCapacityMw:F1} MW");
                for (int m = 0; m < zones[z].Members.Length; m++)
                {
                    lines.Add($"    {zones[z].Members[m].Code,-10} {zones[z].Members[m].Name}");
                }

                lines.Add(string.Empty);
            }

            if (zones.Length == 0)
            {
                lines.Add("(sin zonas)");
            }

            ShowText("Zonas eléctricas", lines.ToArray());
        }

        private void ActionReport()
        {
            string report = GridReportBuilder.Build(_service);
            ShowText("Reporte de la red", report.Replace("\r", string.Empty).Split('\n'));

            if (!Confirm("¿Exportar este reporte a un archivo de texto?", false))
            {
                return;
            }

            string[]? v = ShowForm("Exportar reporte", new[] { new FormField("Archivo", DefaultReportFile, FieldKind.Text) });
            if (v == null)
            {
                return;
            }

            try
            {
                string path = GridReportBuilder.Export(_service, v[0]);
                ShowMessage("Reporte exportado", Wrap(path, 56), ConsoleColor.Green);
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                ShowMessage("Falló la exportación", Wrap(ex.Message, 56), ConsoleColor.Red);
            }
        }

        private void ActionInspect()
        {
            int choice = Pick("Ver estructura", new[] { "Tabla hash", "Grafo (listas de adyacencia)", "Trie", "Union-Find" });
            if (choice < 0)
            {
                return;
            }

            string[] titles = { "Tabla hash", "Grafo", "Trie", "Union-Find" };
            string text = choice == 0 ? _inspector.DescribeHashTable()
                        : choice == 1 ? _inspector.DescribeGraph()
                        : choice == 2 ? _inspector.DescribeTrie()
                        : _inspector.DescribeDisjointSets();

            ShowText(titles[choice], text.Replace("\r", string.Empty).Split('\n'));
        }

        private void ActionToggleGlyphs()
        {
            _glyphs = _glyphs.IsUnicode ? Glyphs.Ascii : Glyphs.Unicode;
            _layoutDirty = true;
            SetStatus(_glyphs.IsUnicode ? "Dibujo con caracteres Unicode." : "Dibujo con ASCII simple.", ConsoleColor.Gray);
        }

        private void ActionHelp()
        {
            ShowText("Ayuda", new[]
            {
                "MENÚ (izquierda)",
                "  Flechas arriba/abajo  moverse      Enter  ejecutar la opción",
                "  Tab                   pasar al panel del grafo",
                "",
                "BÚSQUEDA",
                "  /  buscar una subestación por parte del nombre o del código (desde cualquier lugar)",
                "",
                "GRAFO (derecha)",
                "  Flechas   seleccionar la subestación más cercana en esa dirección",
                "  Enter     detalle de la subestación seleccionada",
                "  B / D     recorrido BFS / DFS desde la seleccionada (se resalta)",
                "  R         ruta óptima (requiere Dijkstra)",
                "  C         quitar resaltado        L  cambiar estilo de etiquetas",
                "  Esc       volver al menú",
                "",
                "EN CUALQUIER LUGAR",
                "  F  grafo a pantalla completa    ?/F1  esta ayuda    Q o Ctrl+C  salir",
                "",
                "CÓMO LEER EL DIBUJO",
                "  [SUB-001]  subestación, con el color de su zona eléctrica",
                "  ---------  línea de doble sentido",
                "  ------->   línea de un solo sentido (flecha en el destino)",
                "  Amarillo   recorrido o ruta resaltados",
                "  La distribución es automática; donde se cruzan líneas aparece una cruz.",
                "",
                "Terminal: cualquiera de 80x24 o más. Si los recuadros se ven mal,",
                "use la opción 'ASCII / Unicode' del menú o inicie con --ascii."
            });
        }

        private void ActionQuit()
        {
            if (Confirm("¿Salir de la aplicación?", true))
            {
                _running = false;
            }
        }

        private void ShowSelectedNodeDetails()
        {
            if (_snapshot.Nodes.Length > 0)
            {
                ShowNodeDetails(_snapshot.Nodes[_selectedNode]);
            }
        }

        private void ShowNodeDetails(Substation node)
        {
            int index = _snapshot.IndexOf(node.Code);
            ArrayBuffer<string> lines = new ArrayBuffer<string>();
            lines.Add($"Código:     {node.Code}");
            lines.Add($"Nombre:     {node.Name}");
            lines.Add($"Capacidad:  {node.CapacityMw:F1} MW");

            if (index >= 0)
            {
                lines.Add($"Zona:       {_snapshot.ZoneOfNode[index] + 1}");
                lines.Add(string.Empty);
                lines.Add("Líneas:");

                for (int e = 0; e < _snapshot.EdgeFrom.Length; e++)
                {
                    if (_snapshot.EdgeFrom[e] == index)
                    {
                        string arrow = _snapshot.EdgeTwoWay[e] ? "<->" : "-> ";
                        lines.Add($"  {arrow} {_snapshot.Nodes[_snapshot.EdgeTo[e]].Code}  {_snapshot.Nodes[_snapshot.EdgeTo[e]].Name}");
                    }
                    else if (_snapshot.EdgeTo[e] == index)
                    {
                        string arrow = _snapshot.EdgeTwoWay[e] ? "<->" : "<- ";
                        lines.Add($"  {arrow} {_snapshot.Nodes[_snapshot.EdgeFrom[e]].Code}  {_snapshot.Nodes[_snapshot.EdgeFrom[e]].Name}");
                    }
                }
            }

            ShowMessage("Detalle de la subestación", lines.ToArray(), ConsoleColor.Cyan);
        }

        private FormField SubstationField(string label) => SubstationField(label, _selectedCode ?? string.Empty);

        private static FormField SubstationField(string label, string value) => new FormField(label, value, FieldKind.Substation);

        private void ReportResult(OperationResult result)
        {
            SetStatus(result.Message, result.Success ? ConsoleColor.Green : ConsoleColor.Red);
            if (!result.Success)
            {
                ShowMessage("La operación falló", Wrap(result.Message, 56), ConsoleColor.Red);
            }
        }

        private static bool IsAffirmative(string text) => !(text.Length > 0 && (text[0] == 'n' || text[0] == 'N'));

        private static bool TryNumber(string text, out double value)
            => double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
