using System;
using System.Text;
using System.Threading;
using ElectricGrid.Collections;
using ElectricGrid.IO;
using ElectricGrid.Services;

namespace ElectricGrid.Tui
{
    /// <summary>
    /// Interfaz de texto en pantalla completa: menú a la izquierda, dibujo interactivo del grafo a la derecha,
    /// una línea de información y una barra de estado. Solo usa System.Console y se comunica con el núcleo
    /// a través de IPowerGridService. Se divide en TuiApp.cs (ciclo y marco), TuiApp.Dialogs.cs (ventanas
    /// modales) y TuiApp.Actions.cs (acciones del menú).
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

        private string[] _highlightCodes = new string[0];
        private bool _highlightIsPath;
        private string _highlightTitle = string.Empty;

        private string _status = "Elija 'Cargar ejemplo' para comenzar.";
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

        /// <summary>Ejecuta la interfaz hasta que el usuario sale; requiere una terminal interactiva.</summary>
        public void Run()
        {
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                Console.WriteLine("La interfaz necesita una terminal interactiva (la entrada y la salida no deben estar redirigidas).");
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

        /// <summary>Espera una tecla sondeando la terminal; si cambió el tamaño de la ventana reconstruye el búfer y devuelve nulo.</summary>
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

        /// <summary>Procesa una tecla según el panel que tiene el foco.</summary>
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

            if (key.KeyChar == '/')
            {
                RunSafely(ActionSearch);
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
                    SetStatus("Resaltado quitado.", ConsoleColor.Gray);
                    break;
                case ConsoleKey.L:
                    _labelMode = (LabelMode)(((int)_labelMode + 1) % 3);
                    _layoutDirty = true;
                    SetStatus($"Etiquetas: {LabelModeName(_labelMode)}.", ConsoleColor.Gray);
                    break;
                case ConsoleKey.Escape:
                    _focus = Focus.Menu;
                    break;
                case ConsoleKey.Q:
                    ActionQuit();
                    break;
            }
        }

        private static string LabelModeName(LabelMode mode)
        {
            switch (mode)
            {
                case LabelMode.Full: return "completas";
                case LabelMode.Compact: return "cortas";
                default: return "automáticas";
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

        /// <summary>Ejecuta una acción convirtiendo los fallos esperados en ventanas de aviso y marca el grafo para actualizarse.</summary>
        private void RunSafely(Action action)
        {
            try
            {
                action();
            }
            catch (NotImplementedException ex)
            {
                ShowMessage("Función pendiente", Wrap(ex.Message, 56), ConsoleColor.Yellow);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                ShowMessage("Problema inesperado", Wrap(ex.Message, 56), ConsoleColor.Red);
            }

            _snapshotDirty = true;
        }

        /// <summary>Mueve la selección al nodo más cercano en la dirección indicada, según la geometría de pantalla.</summary>
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
                double vy = (_nodeY[i] - _nodeY[_selectedNode]) * 2.0;
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

        /// <summary>Compone el cuadro completo en el búfer: título, menú, grafo, información y barra de estado.</summary>
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

            int menuWidth = _graphFullScreen ? 0 : Math.Min(30, Math.Max(26, _width / 4));
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
                "Terminal demasiado pequeña",
                $"Actual: {_width}x{_height}",
                $"Mínimo: {MinWidth}x{MinHeight}",
                "Amplíe la ventana para continuar."
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
            _buffer.Text(1, 0, "GESTIÓN DE RED ELÉCTRICA MUNICIPAL", ConsoleColor.White, ConsoleColor.DarkBlue);

            string right = $"{_snapshot.Nodes.Length} subestaciones | {_snapshot.EdgeFrom.Length} líneas | {_snapshot.ZoneCount} zonas ";
            _buffer.Text(_width - right.Length, 0, right, ConsoleColor.Cyan, ConsoleColor.DarkBlue);
        }

        private void DrawMenu(int x, int y, int w, int h)
        {
            bool focused = _focus == Focus.Menu;
            _buffer.Box(x, y, w, h, "Menú", _glyphs, focused ? ConsoleColor.Cyan : ConsoleColor.DarkGray, ScreenBuffer.DefaultBack);

            int rows = h - 2;
            if (_menuIndex < _menuScroll)
            {
                _menuScroll = _menuIndex;
            }

            if (_menuIndex >= _menuScroll + rows)
            {
                _menuScroll = _menuIndex - rows + 1;
            }

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
            string title = _graphFullScreen ? "Grafo de la red (pantalla completa)" : "Grafo de la red";
            _buffer.Box(x, y, w, h, title, _glyphs, focused ? ConsoleColor.Cyan : ConsoleColor.DarkGray, ScreenBuffer.DefaultBack);

            int canvasX = x + 1, canvasY = y + 1, canvasW = w - 2, canvasH = h - 2;
            int n = _snapshot.Nodes.Length;

            if (n == 0)
            {
                string[] hint = { "No hay subestaciones cargadas.", "Menú > Cargar ejemplo" };
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
                string details = $"Seleccionada: {node.Code}  {node.Name}  |  {node.CapacityMw:F1} MW  |  zona {_snapshot.ZoneOfNode[_selectedNode] + 1}  |  líneas: {_snapshot.LineCount(_selectedNode)}";
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
                string legend = $"[COD] subestación  {_glyphs.Horizontal}{_glyphs.Horizontal}{_glyphs.Horizontal} doble sentido  {_glyphs.Horizontal}{_glyphs.Horizontal}> un sentido  color = zona";
                _buffer.Text(1, row + 1, legend, ConsoleColor.DarkGray, ScreenBuffer.DefaultBack, _width - 2);
            }
        }

        private void DrawStatusBar()
        {
            int row = _height - 1;
            _buffer.Fill(0, row, _width, 1, ' ', ConsoleColor.Black, ConsoleColor.Gray);

            string keys = _focus == Focus.Menu
                ? "Enter  /:buscar  Tab:grafo  ?:ayuda  Q:salir"
                : "/:buscar B:BFS D:DFS R:ruta C:limpiar  Tab:menú";

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

        /// <summary>Recalcula etiquetas y posiciones de los nodos cuando cambian los datos, el tamaño o el estilo de etiqueta.</summary>
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
            ArrayBuffer<int> indices = new ArrayBuffer<int>(_highlightCodes.Length + 1);
            for (int i = 0; i < _highlightCodes.Length; i++)
            {
                int index = _snapshot.IndexOf(_highlightCodes[i]);
                if (index >= 0)
                {
                    indices.Add(index);
                }
            }

            return indices.ToArray();
        }
    }
}
