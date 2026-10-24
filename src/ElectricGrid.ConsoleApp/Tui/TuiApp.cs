using System;
using System.Text;
using System.Threading;
using ElectricGrid.Core.IO;
using ElectricGrid.Core.Services;

namespace ElectricGrid.ConsoleApp.Tui
{
    /// <summary>
    /// Full-screen text user interface: menu on the left, interactive drawing of the grid graph on the
    /// right, an information line and a status bar. It only needs <see cref="System.Console"/>, so it runs
    /// in any terminal of at least 80x24. Like the classic menu, it contains no business logic: it talks to
    /// the Core library through <see cref="IPowerGridService"/>.
    /// <para>The class is split in three files: this one (loop and frame), TuiApp.Dialogs.cs (modal windows)
    /// and TuiApp.Actions.cs (what each menu entry does).</para>
    /// </summary>
    internal sealed partial class TuiApp
    {
        private const int MinWidth = 80;
        private const int MinHeight = 24;

        private enum Focus
        {
            Menu,
            Graph
        }

        private enum LabelMode
        {
            Auto,
            Full,
            Compact
        }

        private sealed class MenuItem
        {
            public MenuItem(string label, Action? run)
            {
                Label = label;
                Run = run;
            }

            public string Label { get; }

            public Action? Run { get; }

            public bool IsHeader => Run == null;
        }

        private readonly IPowerGridService _service;
        private readonly IStructureInspector _inspector;
        private readonly GridDataLoader _loader;
        private readonly MenuItem[] _menu;

        private Glyphs _glyphs;
        private ScreenBuffer _buffer = new ScreenBuffer(MinWidth, MinHeight);
        private int _width = MinWidth;
        private int _height = MinHeight;

        private int _menuIndex;
        private int _menuScroll;
        private Focus _focus = Focus.Menu;
        private bool _graphFullScreen;
        private bool _running;

        // Graph drawing state.
        private GraphSnapshot _snapshot;
        private bool _snapshotDirty = true;
        private bool _layoutDirty = true;
        private LabelMode _labelMode = LabelMode.Auto;
        private bool _layoutCompact;
        private int _layoutWidth = -1;
        private int _layoutHeight = -1;
        private int[] _nodeX = new int[0];
        private int[] _nodeY = new int[0];
        private string[] _labels = new string[0];
        private int _selectedNode;
        private string? _selectedCode;

        // Highlight (traversal order or route path), stored by code so it survives snapshot rebuilds.
        private string[] _highlightCodes = new string[0];
        private bool _highlightIsPath;
        private string _highlightTitle = string.Empty;

        private string _status = "Pick 'Load sample data' to begin.";
        private ConsoleColor _statusColor = ConsoleColor.Gray;

        public TuiApp(IPowerGridService service, IStructureInspector inspector, bool asciiOnly)
        {
            _service = service;
            _inspector = inspector;
            _loader = new GridDataLoader(service);
            _glyphs = asciiOnly ? Glyphs.Ascii : Glyphs.Unicode;
            _snapshot = GraphSnapshot.Build(service);
            _menu = BuildMenu();
            _menuIndex = FirstSelectable();
        }

        /// <summary>Runs the interface until the user quits. Requires an interactive terminal.</summary>
        public void Run()
        {
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                Console.WriteLine("The TUI needs an interactive terminal (input/output must not be redirected).");
                return;
            }

            _running = true;
            Console.TreatControlCAsInput = true;
            try
            {
                Console.CursorVisible = false;
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException || ex is System.IO.IOException)
            {
                // Some terminals cannot hide the cursor; the interface still works.
            }

            ResizeBuffer();

            try
            {
                while (_running)
                {
                    DrawFrame();
                    _buffer.Flush();

                    ConsoleKeyInfo? key = WaitKey();
                    if (key != null)
                    {
                        HandleKey(key.Value);
                    }
                }
            }
            finally
            {
                Console.ResetColor();
                try
                {
                    Console.CursorVisible = true;
                }
                catch (Exception ex) when (ex is PlatformNotSupportedException || ex is System.IO.IOException)
                {
                }

                Console.TreatControlCAsInput = false;
                Console.Clear();
            }
        }

        /// <summary>Renders one frame without a terminal and returns it as text (used by --tui-snapshot).</summary>
        public string RenderSnapshot(int width, int height)
        {
            _width = width;
            _height = height;
            _buffer = new ScreenBuffer(width, height);
            DrawFrame();
            return _buffer.ToPlainText();
        }

        // ------------------------------------------------------------------ Input

        /// <summary>
        /// Waits for a key. Polling (instead of a blocking read) lets the interface notice when the
        /// terminal is resized; in that case the buffer is rebuilt and <c>null</c> is returned so the
        /// caller redraws.
        /// </summary>
        private ConsoleKeyInfo? WaitKey()
        {
            while (true)
            {
                if (Console.WindowWidth != _width || Console.WindowHeight != _height)
                {
                    ResizeBuffer();
                    return null;
                }

                if (Console.KeyAvailable)
                {
                    return Console.ReadKey(intercept: true);
                }

                Thread.Sleep(20);
            }
        }

        private void ResizeBuffer()
        {
            _width = Math.Max(1, Console.WindowWidth);
            _height = Math.Max(1, Console.WindowHeight);
            _buffer = new ScreenBuffer(_width, _height);
            _layoutDirty = true;
            Console.ResetColor();
            Console.Clear();
        }

        private void HandleKey(ConsoleKeyInfo key)
        {
            bool ctrl = (key.Modifiers & ConsoleModifiers.Control) != 0;

            if (ctrl && key.Key == ConsoleKey.C)
            {
                ActionQuit();
                return;
            }

            switch (key.Key)
            {
                case ConsoleKey.Tab:
                    _focus = _focus == Focus.Menu ? Focus.Graph : Focus.Menu;
                    return;
                case ConsoleKey.F1:
                    ActionHelp();
                    return;
                case ConsoleKey.F:
                    _graphFullScreen = !_graphFullScreen;
                    _layoutDirty = true;
                    return;
            }

            if (key.KeyChar == '?')
            {
                ActionHelp();
                return;
            }

            if (_focus == Focus.Menu)
            {
                HandleMenuKey(key);
            }
            else
            {
                HandleGraphKey(key);
            }
        }

        private void HandleMenuKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    MoveMenu(-1);
                    break;
                case ConsoleKey.DownArrow:
                    MoveMenu(1);
                    break;
                case ConsoleKey.PageUp:
                    MoveMenu(-8);
                    break;
                case ConsoleKey.PageDown:
                    MoveMenu(8);
                    break;
                case ConsoleKey.Home:
                    _menuIndex = FirstSelectable();
                    break;
                case ConsoleKey.End:
                    _menuIndex = _menu.Length - 1;
                    break;
                case ConsoleKey.Enter:
                    RunMenuItem();
                    break;
                case ConsoleKey.Q:
                    ActionQuit();
                    break;
                case ConsoleKey.RightArrow:
                    _focus = Focus.Graph;
                    break;
            }
        }

        private void HandleGraphKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    MoveSelection(0, -1);
                    break;
                case ConsoleKey.DownArrow:
                    MoveSelection(0, 1);
                    break;
                case ConsoleKey.LeftArrow:
                    MoveSelection(-1, 0);
                    break;
                case ConsoleKey.RightArrow:
                    MoveSelection(1, 0);
                    break;
                case ConsoleKey.Enter:
                    ShowSelectedNodeDetails();
                    break;
                case ConsoleKey.B:
                    RunTraversalFromSelected(breadthFirst: true);
                    break;
                case ConsoleKey.D:
                    RunTraversalFromSelected(breadthFirst: false);
                    break;
                case ConsoleKey.R:
                    RunSafely(ActionRoute);
                    break;
                case ConsoleKey.C:
                    ClearHighlight();
                    SetStatus("Highlight cleared.", ConsoleColor.Gray);
                    break;
                case ConsoleKey.L:
                    _labelMode = (LabelMode)(((int)_labelMode + 1) % 3);
                    _layoutDirty = true;
                    SetStatus($"Labels: {_labelMode}", ConsoleColor.Gray);
                    break;
                case ConsoleKey.Escape:
                    _focus = Focus.Menu;
                    break;
                case ConsoleKey.Q:
                    ActionQuit();
                    break;
            }
        }

        private void MoveMenu(int delta)
        {
            int direction = delta < 0 ? -1 : 1;
            int steps = Math.Abs(delta);
            int index = _menuIndex;

            for (int s = 0; s < steps; s++)
            {
                int candidate = index + direction;
                while (candidate >= 0 && candidate < _menu.Length && _menu[candidate].IsHeader)
                {
                    candidate += direction;
                }

                if (candidate < 0 || candidate >= _menu.Length)
                {
                    break;
                }

                index = candidate;
            }

            _menuIndex = index;
        }

        private int FirstSelectable()
        {
            for (int i = 0; i < _menu.Length; i++)
            {
                if (!_menu[i].IsHeader)
                {
                    return i;
                }
            }

            return 0;
        }

        private void RunMenuItem()
        {
            Action? run = _menu[_menuIndex].Run;
            if (run != null)
            {
                RunSafely(run);
            }
        }

        /// <summary>
        /// Executes an action turning the expected failure modes into dialogs instead of crashes, and
        /// marks the graph data as possibly changed.
        /// </summary>
        private void RunSafely(Action action)
        {
            try
            {
                action();
            }
            catch (NotImplementedException ex)
            {
                // Placeholder for the pieces still to be written by teammates (Dijkstra, Trie).
                ShowMessage("Pending feature", Wrap(ex.Message, 56), ConsoleColor.Yellow);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                ShowMessage("Unexpected problem", Wrap(ex.Message, 56), ConsoleColor.Red);
            }

            _snapshotDirty = true;
        }

        /// <summary>Moves the selection to the nearest node in the given direction (screen geometry).</summary>
        private void MoveSelection(int directionX, int directionY)
        {
            int n = _snapshot.Nodes.Length;
            if (n == 0 || _nodeX.Length != n)
            {
                return;
            }

            int best = -1;
            double bestScore = double.MaxValue;

            for (int i = 0; i < n; i++)
            {
                if (i == _selectedNode)
                {
                    continue;
                }

                double vx = _nodeX[i] - _nodeX[_selectedNode];
                double vy = (_nodeY[i] - _nodeY[_selectedNode]) * 2.0; // rows are about twice as tall as columns
                double along = vx * directionX + vy * directionY;
                double across = Math.Abs(vx * directionY - vy * directionX);

                if (along <= 0)
                {
                    continue;
                }

                double score = along + 2 * across;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            if (best >= 0)
            {
                SelectNode(best);
            }
        }

        private void SelectNode(int index)
        {
            _selectedNode = index;
            _selectedCode = _snapshot.Nodes[index].Code;
        }

        private void SetStatus(string message, ConsoleColor color)
        {
            _status = message;
            _statusColor = color;
        }

        private void ClearHighlight()
        {
            _highlightCodes = new string[0];
            _highlightIsPath = false;
            _highlightTitle = string.Empty;
        }

        // ------------------------------------------------------------------ Frame

        private void DrawFrame()
        {
            _buffer.Clear();

            if (_width < MinWidth || _height < MinHeight)
            {
                DrawTooSmall();
                return;
            }

            EnsureSnapshot();
            DrawTitleBar();

            int menuWidth = _graphFullScreen ? 0 : Math.Min(30, Math.Max(24, _width / 4));
            if (menuWidth > 0)
            {
                DrawMenu(0, 1, menuWidth, _height - 4);
            }

            DrawGraphPanel(menuWidth, 1, _width - menuWidth, _height - 4);
            DrawInfoLines();
            DrawStatusBar();
        }

        private void DrawTooSmall()
        {
            string[] lines =
            {
                "Terminal too small",
                $"Current: {_width}x{_height}",
                $"Needed:  {MinWidth}x{MinHeight}",
                "Resize the window to continue."
            };

            for (int i = 0; i < lines.Length; i++)
            {
                _buffer.Text(Math.Max(0, (_width - lines[i].Length) / 2), Math.Max(0, _height / 2 - 2 + i),
                             lines[i], ConsoleColor.Yellow, ScreenBuffer.DefaultBack);
            }
        }

        private void DrawTitleBar()
        {
            _buffer.Fill(0, 0, _width, 1, ' ', ConsoleColor.White, ConsoleColor.DarkBlue);
            _buffer.Text(1, 0, "MUNICIPAL ELECTRICAL GRID", ConsoleColor.White, ConsoleColor.DarkBlue);

            string right = $"{_snapshot.Nodes.Length} substations | {_snapshot.EdgeFrom.Length} lines | {_snapshot.ZoneCount} zones ";
            _buffer.Text(_width - right.Length, 0, right, ConsoleColor.Cyan, ConsoleColor.DarkBlue);
        }

        private void DrawMenu(int x, int y, int w, int h)
        {
            bool focused = _focus == Focus.Menu;
            _buffer.Box(x, y, w, h, "Menu", _glyphs, focused ? ConsoleColor.Cyan : ConsoleColor.DarkGray, ScreenBuffer.DefaultBack);

            int rows = h - 2;
            if (_menuIndex < _menuScroll)
            {
                _menuScroll = _menuIndex;
            }

            if (_menuIndex >= _menuScroll + rows)
            {
                _menuScroll = _menuIndex - rows + 1;
            }

            // Keep the header of the section visible when scrolled just below it.
            if (_menuScroll > 0 && _menuScroll == _menuIndex && _menu[_menuScroll - 1].IsHeader)
            {
                _menuScroll--;
            }

            for (int row = 0; row < rows; row++)
            {
                int index = _menuScroll + row;
                if (index >= _menu.Length)
                {
                    break;
                }

                MenuItem item = _menu[index];
                int cy = y + 1 + row;

                if (item.IsHeader)
                {
                    _buffer.Text(x + 1, cy, item.Label.ToUpperInvariant(), ConsoleColor.DarkCyan, ScreenBuffer.DefaultBack, w - 2);
                    continue;
                }

                bool selected = index == _menuIndex;
                ConsoleColor fore = selected ? (focused ? ConsoleColor.Black : ConsoleColor.White) : ConsoleColor.Gray;
                ConsoleColor back = selected ? (focused ? ConsoleColor.Cyan : ConsoleColor.DarkGray) : ScreenBuffer.DefaultBack;

                _buffer.Fill(x + 1, cy, w - 2, 1, ' ', fore, back);
                _buffer.Text(x + 2, cy, item.Label, fore, back, w - 4);
            }

            if (_menuScroll > 0)
            {
                _buffer.Put(x + w - 2, y + 1, '^', ConsoleColor.Yellow, ScreenBuffer.DefaultBack);
            }

            if (_menuScroll + rows < _menu.Length)
            {
                _buffer.Put(x + w - 2, y + h - 2, 'v', ConsoleColor.Yellow, ScreenBuffer.DefaultBack);
            }
        }

        private void DrawGraphPanel(int x, int y, int w, int h)
        {
            bool focused = _focus == Focus.Graph;
            string title = _graphFullScreen ? "Grid graph (full screen)" : "Grid graph";
            _buffer.Box(x, y, w, h, title, _glyphs, focused ? ConsoleColor.Cyan : ConsoleColor.DarkGray, ScreenBuffer.DefaultBack);

            int canvasX = x + 1, canvasY = y + 1, canvasW = w - 2, canvasH = h - 2;
            int n = _snapshot.Nodes.Length;

            if (n == 0)
            {
                string[] hint = { "No substations loaded.", "Menu > Load sample data" };
                for (int i = 0; i < hint.Length; i++)
                {
                    _buffer.Text(canvasX + Math.Max(0, (canvasW - hint[i].Length) / 2), canvasY + canvasH / 2 - 1 + i,
                                 hint[i], ConsoleColor.DarkGray, ScreenBuffer.DefaultBack);
                }

                return;
            }

            EnsureLayout(canvasW, canvasH);
            GraphCanvas.Draw(_buffer, _glyphs, canvasX, canvasY, _snapshot, _nodeX, _nodeY, _labels,
                             _selectedNode, HighlightIndices(), _highlightIsPath);
        }

        private void DrawInfoLines()
        {
            int row = _height - 3;
            _buffer.Fill(0, row, _width, 2, ' ', ConsoleColor.Gray, ScreenBuffer.DefaultBack);

            int n = _snapshot.Nodes.Length;
            if (n > 0 && _selectedNode < n)
            {
                var node = _snapshot.Nodes[_selectedNode];
                string details = $"Selected: {node.Code}  {node.Name}  |  {node.CapacityMw:F1} MW  |  zone {_snapshot.ZoneOfNode[_selectedNode] + 1}  |  {_snapshot.LineCount(_selectedNode)} line(s)";
                _buffer.Text(1, row, details, ConsoleColor.White, ScreenBuffer.DefaultBack, _width - 2);
            }

            if (_highlightCodes.Length > 0)
            {
                StringBuilder sequence = new StringBuilder(_highlightTitle).Append(": ");
                for (int i = 0; i < _highlightCodes.Length; i++)
                {
                    if (i > 0)
                    {
                        sequence.Append(_highlightIsPath ? " > " : ", ");
                    }

                    sequence.Append(_highlightCodes[i]);
                }

                string text = sequence.Length > _width - 2 ? sequence.ToString(0, _width - 5) + "..." : sequence.ToString();
                _buffer.Text(1, row + 1, text, ConsoleColor.Yellow, ScreenBuffer.DefaultBack);
            }
            else
            {
                string legend = $"[CODE] substation  {_glyphs.Horizontal}{_glyphs.Horizontal}{_glyphs.Horizontal} two-way  {_glyphs.Horizontal}{_glyphs.Horizontal}> one-way  colors = electrical zones";
                _buffer.Text(1, row + 1, legend, ConsoleColor.DarkGray, ScreenBuffer.DefaultBack, _width - 2);
            }
        }

        private void DrawStatusBar()
        {
            int row = _height - 1;
            _buffer.Fill(0, row, _width, 1, ' ', ConsoleColor.Black, ConsoleColor.Gray);

            string keys = _focus == Focus.Menu
                ? "Up/Down Enter  Tab:graph  F:full  ?:help  Q:quit"
                : "Arrows  B:BFS D:DFS R:route C:clear L:labels  Tab:menu";

            int keysStart = _width - keys.Length - 1;
            _buffer.Text(keysStart, row, keys, ConsoleColor.DarkBlue, ConsoleColor.Gray);
            _buffer.Text(1, row, _status, _statusColor == ConsoleColor.Gray ? ConsoleColor.Black : DarkenForStatus(_statusColor),
                         ConsoleColor.Gray, Math.Max(0, keysStart - 2));
        }

        private static ConsoleColor DarkenForStatus(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Green: return ConsoleColor.DarkGreen;
                case ConsoleColor.Red: return ConsoleColor.DarkRed;
                case ConsoleColor.Yellow: return ConsoleColor.DarkYellow;
                default: return ConsoleColor.Black;
            }
        }

        // ------------------------------------------------------------------ Graph state

        private void EnsureSnapshot()
        {
            if (!_snapshotDirty)
            {
                return;
            }

            _snapshot = GraphSnapshot.Build(_service);
            _snapshotDirty = false;
            _layoutDirty = true;

            int index = _snapshot.IndexOf(_selectedCode);
            if (index < 0)
            {
                index = 0;
            }

            _selectedNode = index;
            _selectedCode = _snapshot.Nodes.Length > 0 ? _snapshot.Nodes[index].Code : null;
        }

        private void EnsureLayout(int canvasWidth, int canvasHeight)
        {
            int n = _snapshot.Nodes.Length;
            bool compact = _labelMode == LabelMode.Compact
                           || (_labelMode == LabelMode.Auto && n > (canvasWidth * canvasHeight) / 70);

            if (!_layoutDirty && canvasWidth == _layoutWidth && canvasHeight == _layoutHeight
                && compact == _layoutCompact && _nodeX.Length == n)
            {
                return;
            }

            _labels = new string[n];
            int longest = 1;
            for (int i = 0; i < n; i++)
            {
                _labels[i] = compact ? ShortCode(_snapshot.Nodes[i].Code) : _snapshot.Nodes[i].Code;
                longest = Math.Max(longest, _labels[i].Length);
            }

            GraphLayout.Compute(_snapshot, canvasWidth, canvasHeight, longest + 2, out _nodeX, out _nodeY);
            _layoutWidth = canvasWidth;
            _layoutHeight = canvasHeight;
            _layoutCompact = compact;
            _layoutDirty = false;
        }

        /// <summary>"SUB-014" becomes "014"; codes without a dash are truncated.</summary>
        private static string ShortCode(string code)
        {
            int dash = code.LastIndexOf('-');
            if (dash >= 0 && dash < code.Length - 1)
            {
                return code.Substring(dash + 1);
            }

            return code.Length > 5 ? code.Substring(0, 5) : code;
        }

        private int[] HighlightIndices()
        {
            int[] indices = new int[_highlightCodes.Length];
            int count = 0;
            for (int i = 0; i < _highlightCodes.Length; i++)
            {
                int index = _snapshot.IndexOf(_highlightCodes[i]);
                if (index >= 0)
                {
                    indices[count++] = index;
                }
            }

            Array.Resize(ref indices, count);
            return indices;
        }
    }
}
