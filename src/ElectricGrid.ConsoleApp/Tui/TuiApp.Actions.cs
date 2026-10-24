using System;
using System.Globalization;
using ElectricGrid.Core.Collections;
using ElectricGrid.Core.DataStructures;
using ElectricGrid.Core.Models;
using ElectricGrid.Core.Reporting;

namespace ElectricGrid.ConsoleApp.Tui
{
    internal sealed partial class TuiApp
    {
        private const string DefaultSubstationsFile = "substations.csv";
        private const string DefaultLinesFile = "lines.csv";
        private const string DefaultReportFile = "grid_report.txt";

        private MenuItem[] BuildMenu()
        {
            return new[]
            {
                new MenuItem("Data", null),
                new MenuItem("Load sample data", ActionLoadSample),
                new MenuItem("Load substations file", ActionLoadSubstations),
                new MenuItem("Load lines file", ActionLoadLines),
                new MenuItem("Clear all data", ActionClear),
                new MenuItem("Substations", null),
                new MenuItem("Register substation", ActionRegisterSubstation),
                new MenuItem("Find by code", ActionFindSubstation),
                new MenuItem("Remove substation", ActionRemoveSubstation),
                new MenuItem("List substations", ActionListSubstations),
                new MenuItem("Lines", null),
                new MenuItem("Register line", ActionRegisterLine),
                new MenuItem("Propose new line", ActionProposeLine),
                new MenuItem("Remove line", ActionRemoveLine),
                new MenuItem("List lines", ActionListLines),
                new MenuItem("Analysis", null),
                new MenuItem("Optimal route", ActionRoute),
                new MenuItem("BFS traversal", () => ActionTraversal(true)),
                new MenuItem("DFS traversal", () => ActionTraversal(false)),
                new MenuItem("Same electrical zone?", ActionSameZone),
                new MenuItem("List zones", ActionListZones),
                new MenuItem("Report / export", ActionReport),
                new MenuItem("Tools", null),
                new MenuItem("Inspect structures", ActionInspect),
                new MenuItem("Toggle ASCII / Unicode", ActionToggleGlyphs),
                new MenuItem("Help", ActionHelp),
                new MenuItem("Quit", ActionQuit)
            };
        }

        // ------------------------------------------------------------------ Data

        private void ActionLoadSample()
        {
            LoadReport report = _loader.LoadAll(DefaultSubstationsFile, DefaultLinesFile);
            AfterLoad(report);
        }

        private void ActionLoadSubstations()
        {
            string[]? values = ShowForm("Load substations", new[]
            {
                new FormField("File", DefaultSubstationsFile, FieldKind.Text, "format: code,name,capacity_mw")
            });

            if (values != null)
            {
                AfterLoad(_loader.LoadSubstations(values[0]));
            }
        }

        private void ActionLoadLines()
        {
            string[]? values = ShowForm("Load lines", new[]
            {
                new FormField("File", DefaultLinesFile, FieldKind.Text, "format: source,target,length_km,cost,bidirectional")
            });

            if (values != null)
            {
                AfterLoad(_loader.LoadLines(values[0]));
            }
        }

        private void AfterLoad(LoadReport report)
        {
            _snapshotDirty = true;
            ClearHighlight();

            ArrayBuffer<string> lines = new ArrayBuffer<string>();
            lines.Add($"Substations loaded: {report.SubstationsLoaded}");
            lines.Add($"Lines loaded:       {report.LinesLoaded}");
            lines.Add($"Rows skipped:       {report.RowsSkipped}");

            int shown = Math.Min(report.Errors.Length, 8);
            for (int i = 0; i < shown; i++)
            {
                lines.Add("! " + report.Errors[i]);
            }

            if (report.Errors.Length > shown)
            {
                lines.Add($"... and {report.Errors.Length - shown} more");
            }

            bool ok = report.FileFound && report.RowsSkipped == 0;
            bool loaded = report.SubstationsLoaded + report.LinesLoaded > 0;
            ConsoleColor color = ok ? ConsoleColor.Green : (loaded ? ConsoleColor.Yellow : ConsoleColor.Red);
            ShowMessage(ok ? "Load complete" : (loaded ? "Loaded with problems" : "Load failed"), lines.ToArray(), color);
            SetStatus($"Loaded {report.SubstationsLoaded} substation(s) and {report.LinesLoaded} line(s).", ok ? ConsoleColor.Green : ConsoleColor.Yellow);
        }

        private void ActionClear()
        {
            if (Confirm("This removes every substation and line. Continue?", false))
            {
                _service.Clear();
                ClearHighlight();
                _selectedCode = null;
                _snapshotDirty = true;
                SetStatus("All data cleared.", ConsoleColor.Green);
            }
        }

        // ------------------------------------------------------------------ Substations

        private void ActionRegisterSubstation()
        {
            string[]? v = ShowForm("Register substation", new[]
            {
                new FormField("Code", string.Empty, FieldKind.Text, "unique, e.g. SUB-050"),
                new FormField("Name", string.Empty, FieldKind.Text),
                new FormField("Capacity (MW)", string.Empty, FieldKind.Text, "positive number")
            });

            if (v == null)
            {
                return;
            }

            if (!TryNumber(v[2], out double capacity))
            {
                ShowMessage("Invalid capacity", new[] { "Capacity must be a number (use '.' for decimals)." }, ConsoleColor.Red);
                return;
            }

            ReportResult(_service.RegisterSubstation(v[0], v[1], capacity));
            _snapshotDirty = true;
        }

        private void ActionFindSubstation()
        {
            string[]? v = ShowForm("Find substation", new[] { SubstationField("Code") });
            if (v == null)
            {
                return;
            }

            Substation? found = _service.FindSubstation(v[0]);
            if (found == null)
            {
                ShowMessage("Not found", new[] { $"No substation with code '{v[0].ToUpperInvariant()}'." }, ConsoleColor.Yellow);
                return;
            }

            _snapshotDirty = true;
            _selectedCode = found.Code;
            _focus = Focus.Graph;
            ShowNodeDetails(found);
        }

        private void ActionRemoveSubstation()
        {
            string[]? v = ShowForm("Remove substation", new[] { SubstationField("Substation") });
            if (v == null)
            {
                return;
            }

            string? code = ResolveCode(v[0]);
            if (code == null)
            {
                ShowMessage("Not found", new[] { "That substation does not exist." }, ConsoleColor.Yellow);
                return;
            }

            if (Confirm($"Remove {code} and every line attached to it?", false))
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
            lines.Add($"{"CODE",-10} {"NAME",-32} {"CAPACITY (MW)",13}");
            lines.Add(new string('-', 57));
            for (int i = 0; i < all.Length; i++)
            {
                lines.Add($"{all[i].Code,-10} {all[i].Name,-32} {all[i].CapacityMw,13:F1}");
            }

            lines.Add(string.Empty);
            lines.Add($"Total: {all.Length}");
            ShowText("Substations", lines.ToArray());
        }

        // ------------------------------------------------------------------ Lines

        private void ActionRegisterLine()
        {
            string[]? v = ShowForm("Register line", new[]
            {
                SubstationField("Origin"),
                SubstationField("Destination", string.Empty),
                new FormField("Length (km)", string.Empty, FieldKind.Text, "positive number"),
                new FormField("Maintenance cost", string.Empty, FieldKind.Text, "zero or more"),
                new FormField("Two-way? (y/n)", "y", FieldKind.Text, "n = one-way: origin -> destination")
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
                ShowMessage("Unknown substation", new[] { "Origin and destination must be existing substations." }, ConsoleColor.Red);
                return;
            }

            if (!TryNumber(length, out double lengthKm) || !TryNumber(cost, out double maintenance))
            {
                ShowMessage("Invalid number", new[] { "Length and cost must be numbers (use '.' for decimals)." }, ConsoleColor.Red);
                return;
            }

            bool bidirectional = !(twoWay.Length > 0 && (twoWay[0] == 'n' || twoWay[0] == 'N'));
            ReportResult(_service.RegisterLine(new PowerLine(a, b, lengthKm, maintenance, bidirectional)));
            _snapshotDirty = true;
        }

        /// <summary>
        /// Scenario rule: check with Union-Find whether both substations are already in the same zone
        /// before approving a new line between them.
        /// </summary>
        private void ActionProposeLine()
        {
            string[]? pair = ShowForm("Propose line", new[]
            {
                SubstationField("Origin"),
                SubstationField("Destination", string.Empty)
            });

            if (pair == null)
            {
                return;
            }

            string? a = ResolveCode(pair[0]);
            string? b = ResolveCode(pair[1]);
            if (a == null || b == null)
            {
                ShowMessage("Unknown substation", new[] { "Origin and destination must be existing substations." }, ConsoleColor.Red);
                return;
            }

            LineProposalResult proposal = _service.EvaluateProposedLine(a, b);
            if (!proposal.IsValid)
            {
                ShowMessage("Invalid proposal", Wrap(proposal.Message, 56), ConsoleColor.Red);
                return;
            }

            string text = proposal.Message + (proposal.DirectLineExists ? " Note: a direct line already exists in some direction." : string.Empty);
            ConsoleColor accent = proposal.Verdict == LineProposalVerdict.JoinsDifferentZones ? ConsoleColor.Green : ConsoleColor.Yellow;
            ShowMessage("Zone analysis (Union-Find)", Wrap(text, 56), accent);

            if (!Confirm("Approve and register this line?", false))
            {
                SetStatus("Line not approved.", ConsoleColor.Gray);
                return;
            }

            string[]? details = ShowForm($"New line {a} - {b}", new[]
            {
                new FormField("Length (km)", string.Empty, FieldKind.Text, "positive number"),
                new FormField("Maintenance cost", string.Empty, FieldKind.Text, "zero or more"),
                new FormField("Two-way? (y/n)", "y", FieldKind.Text, "n = one-way: origin -> destination")
            });

            if (details != null)
            {
                RegisterLineFromValues(a, b, details[0], details[1], details[2]);
            }
        }

        private void ActionRemoveLine()
        {
            string[]? v = ShowForm("Remove line", new[]
            {
                SubstationField("Origin"),
                SubstationField("Destination", string.Empty),
                new FormField("Both directions? (y/n)", "y", FieldKind.Text)
            });

            if (v == null)
            {
                return;
            }

            string? a = ResolveCode(v[0]);
            string? b = ResolveCode(v[1]);
            if (a == null || b == null)
            {
                ShowMessage("Unknown substation", new[] { "Both substations must exist." }, ConsoleColor.Red);
                return;
            }

            bool both = !(v[2].Length > 0 && (v[2][0] == 'n' || v[2][0] == 'N'));
            ReportResult(_service.RemoveLine(a, b, both));
            ClearHighlight();
            _snapshotDirty = true;
        }

        private void ActionListLines()
        {
            PowerLine[] all = _service.GetAllLines();
            ArrayBuffer<string> lines = new ArrayBuffer<string>();
            lines.Add($"{"FROM",-10} {"DIR",-4} {"TO",-10} {"LENGTH (km)",11} {"MAINT. COST",12}");
            lines.Add(new string('-', 52));
            for (int i = 0; i < all.Length; i++)
            {
                string arrow = all[i].IsBidirectional ? "<->" : "->";
                lines.Add($"{all[i].SourceCode,-10} {arrow,-4} {all[i].TargetCode,-10} {all[i].LengthKm,11:F1} {all[i].MaintenanceCost,12:F0}");
            }

            lines.Add(string.Empty);
            lines.Add($"Total: {all.Length}");
            ShowText("Lines", lines.ToArray());
        }

        // ------------------------------------------------------------------ Analysis

        /// <summary>Hash table -> Union-Find -> Dijkstra. The path is highlighted on the graph when found.</summary>
        private void ActionRoute()
        {
            string[]? v = ShowForm("Optimal route", new[]
            {
                SubstationField("Origin"),
                SubstationField("Destination", string.Empty),
                new FormField("Optimize (1/2)", "1", FieldKind.Text, "1 = lowest maintenance cost, 2 = shortest length")
            });

            if (v == null)
            {
                return;
            }

            string? a = ResolveCode(v[0]);
            string? b = ResolveCode(v[1]);
            if (a == null || b == null)
            {
                ShowMessage("Unknown substation", new[] { "Origin and destination must be existing substations." }, ConsoleColor.Red);
                return;
            }

            EdgeWeightKind criterion = v[2] == "2" ? EdgeWeightKind.Length : EdgeWeightKind.MaintenanceCost;
            RouteResult route = _service.FindOptimalRoute(a, b, criterion); // may throw NotImplementedException (Dijkstra pending)

            if (!route.Found)
            {
                ClearHighlight();
                ShowMessage("No route", Wrap(route.Message, 56), ConsoleColor.Yellow);
                return;
            }

            _highlightCodes = new string[route.Path.Length];
            for (int i = 0; i < route.Path.Length; i++)
            {
                _highlightCodes[i] = route.Path[i].Code;
            }

            _highlightIsPath = true;
            _highlightTitle = criterion == EdgeWeightKind.Length ? "Shortest route" : "Cheapest route";
            _focus = Focus.Graph;

            SetStatus($"{route.Path.Length - 1} hop(s) | {route.TotalLengthKm:F1} km | cost {route.TotalMaintenanceCost:F0}", ConsoleColor.Green);
        }

        private void ActionTraversal(bool breadthFirst)
        {
            string[]? v = ShowForm(breadthFirst ? "BFS traversal" : "DFS traversal", new[] { SubstationField("Start") });
            if (v == null)
            {
                return;
            }

            string? start = ResolveCode(v[0]);
            if (start == null)
            {
                ShowMessage("Unknown substation", new[] { "The start substation must exist." }, ConsoleColor.Red);
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
                ShowMessage("Traversal failed", Wrap(result.Message, 56), ConsoleColor.Red);
                return;
            }

            _highlightCodes = new string[result.Order.Length];
            for (int i = 0; i < result.Order.Length; i++)
            {
                _highlightCodes[i] = result.Order[i].Code;
            }

            _highlightIsPath = false;
            string kind = breadthFirst ? "BFS" : "DFS";
            _highlightTitle = $"{kind} from {startCode}";
            SetStatus($"{kind} reached {result.Order.Length} substation(s) following line direction.", ConsoleColor.Green);
        }

        private void ActionSameZone()
        {
            string[]? v = ShowForm("Same electrical zone?", new[]
            {
                SubstationField("First"),
                SubstationField("Second", string.Empty)
            });

            if (v == null)
            {
                return;
            }

            string? a = ResolveCode(v[0]);
            string? b = ResolveCode(v[1]);
            if (a == null || b == null)
            {
                ShowMessage("Unknown substation", new[] { "Both substations must exist." }, ConsoleColor.Red);
                return;
            }

            ConnectivityResult result = _service.CheckSameZone(a, b);
            ShowMessage("Zone check (Union-Find)", Wrap(result.Message, 56), result.Connected ? ConsoleColor.Green : ConsoleColor.Yellow);
        }

        private void ActionListZones()
        {
            ElectricalZone[] zones = _service.GetZones();
            ArrayBuffer<string> lines = new ArrayBuffer<string>();

            for (int z = 0; z < zones.Length; z++)
            {
                lines.Add($"Zone {zones[z].Id}: {zones[z].Members.Length} substation(s), {zones[z].TotalCapacityMw:F1} MW");
                for (int m = 0; m < zones[z].Members.Length; m++)
                {
                    lines.Add($"    {zones[z].Members[m].Code,-10} {zones[z].Members[m].Name}");
                }

                lines.Add(string.Empty);
            }

            if (zones.Length == 0)
            {
                lines.Add("(no zones)");
            }

            ShowText("Electrical zones", lines.ToArray());
        }

        private void ActionReport()
        {
            string report = GridReportBuilder.Build(_service);
            ShowText("Grid report", report.Replace("\r", string.Empty).Split('\n'));

            if (!Confirm("Export this report to a text file?", false))
            {
                return;
            }

            string[]? v = ShowForm("Export report", new[] { new FormField("File", DefaultReportFile, FieldKind.Text) });
            if (v == null)
            {
                return;
            }

            try
            {
                string path = GridReportBuilder.Export(_service, v[0]);
                ShowMessage("Report exported", Wrap(path, 56), ConsoleColor.Green);
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                ShowMessage("Export failed", Wrap(ex.Message, 56), ConsoleColor.Red);
            }
        }

        // ------------------------------------------------------------------ Tools

        private void ActionInspect()
        {
            int choice = Pick("Inspect structure", new[] { "Hash table", "Graph (adjacency lists)", "Trie", "Union-Find" });
            if (choice < 0)
            {
                return;
            }

            string[] titles = { "Hash table", "Graph", "Trie", "Union-Find" };
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
            SetStatus(_glyphs.IsUnicode ? "Drawing with Unicode box characters." : "Drawing with plain ASCII.", ConsoleColor.Gray);
        }

        private void ActionHelp()
        {
            ShowText("Help", new[]
            {
                "MENU (left)",
                "  Up/Down  move          Enter  run the entry",
                "  Tab      switch to the graph panel",
                "",
                "GRAPH (right)",
                "  Arrows   select the nearest substation in that direction",
                "  Enter    details of the selected substation",
                "  B / D    BFS / DFS from the selected substation (highlighted)",
                "  R        optimal route (needs Dijkstra)",
                "  C        clear highlight       L  cycle label style",
                "  Esc      back to the menu",
                "",
                "ANYWHERE",
                "  F  full-screen graph    ?/F1  this help    Q or Ctrl+C  quit",
                "",
                "READING THE DRAWING",
                "  [SUB-001]  substation, colored by electrical zone",
                "  ---------  two-way line",
                "  ------->   one-way line (arrow at the destination)",
                "  Yellow     highlighted traversal or route",
                "  Layout is automatic (force-directed); crossing lines show a cross mark.",
                "",
                "Terminal: any 80x24 or larger. If box characters look wrong, use",
                "'Toggle ASCII / Unicode' or start with --ascii."
            });
        }

        private void ActionQuit()
        {
            if (Confirm("Quit the application?", true))
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
            lines.Add($"Code:      {node.Code}");
            lines.Add($"Name:      {node.Name}");
            lines.Add($"Capacity:  {node.CapacityMw:F1} MW");

            if (index >= 0)
            {
                lines.Add($"Zone:      {_snapshot.ZoneOfNode[index] + 1}");
                lines.Add(string.Empty);
                lines.Add("Lines:");

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

            ShowMessage("Substation details", lines.ToArray(), ConsoleColor.Cyan);
        }

        // ------------------------------------------------------------------ Helpers

        /// <summary>Substation field preselected with the node currently chosen on the graph.</summary>
        private FormField SubstationField(string label) => SubstationField(label, _selectedCode ?? string.Empty);

        private static FormField SubstationField(string label, string value) => new FormField(label, value, FieldKind.Substation);

        private void ReportResult(OperationResult result)
        {
            SetStatus(result.Message, result.Success ? ConsoleColor.Green : ConsoleColor.Red);
            if (!result.Success)
            {
                ShowMessage("Operation failed", Wrap(result.Message, 56), ConsoleColor.Red);
            }
        }

        private static bool TryNumber(string text, out double value)
            => double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
