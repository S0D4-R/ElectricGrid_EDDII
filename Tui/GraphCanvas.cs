using System;

namespace ElectricGrid.Tui
{
    // Dibuja el grafo sobre el búfer de pantalla.
    internal static class GraphCanvas
    {
        private static readonly ConsoleColor[] ZonePalette =
        {
            ConsoleColor.Cyan, ConsoleColor.Green, ConsoleColor.Magenta,
            ConsoleColor.Red, ConsoleColor.Blue, ConsoleColor.DarkYellow
        };

        // Devuelve el color que corresponde a la zona.
        public static ConsoleColor ZoneColor(int zone) => ZonePalette[zone % ZonePalette.Length];

        // Dibuja aristas, nodos y el resaltado actual.
        public static void Draw(ScreenBuffer buffer, Glyphs glyphs, int ox, int oy, GraphSnapshot graph,
                                int[] nodeX, int[] nodeY, string[] labels, int selected,
                                int[] sequence, bool sequenceIsPath)
        {
            int n = graph.Nodes.Length;
            bool highlighting = sequence.Length > 0;

            bool[] nodeHighlighted = new bool[n];
            for (int i = 0; i < sequence.Length; i++)
            {
                nodeHighlighted[sequence[i]] = true;
            }

            bool[] edgeHighlighted = new bool[graph.EdgeFrom.Length];
            if (sequenceIsPath)
            {
                for (int e = 0; e < edgeHighlighted.Length; e++)
                {
                    for (int i = 0; i + 1 < sequence.Length; i++)
                    {
                        bool forward = sequence[i] == graph.EdgeFrom[e] && sequence[i + 1] == graph.EdgeTo[e];
                        bool backward = graph.EdgeTwoWay[e] && sequence[i] == graph.EdgeTo[e] && sequence[i + 1] == graph.EdgeFrom[e];
                        if (forward || backward)
                        {
                            edgeHighlighted[e] = true;
                            break;
                        }
                    }
                }
            }

            for (int pass = 0; pass < 2; pass++)
            {
                for (int e = 0; e < graph.EdgeFrom.Length; e++)
                {
                    if (edgeHighlighted[e] != (pass == 1))
                    {
                        continue;
                    }

                    ConsoleColor color = edgeHighlighted[e]
                        ? ConsoleColor.Green
                        : (highlighting ? ConsoleColor.DarkGray : ZoneColor(graph.ZoneOfNode[graph.EdgeFrom[e]]));

                    DrawEdge(buffer, glyphs, ox, oy, nodeX, nodeY, labels, graph.EdgeFrom[e], graph.EdgeTo[e],
                             graph.EdgeTwoWay[e], color);
                }
            }

            for (int i = 0; i < n; i++)
            {
                ConsoleColor fore = highlighting ? ConsoleColor.DarkGray : ZoneColor(graph.ZoneOfNode[i]);
                ConsoleColor back = ScreenBuffer.DefaultBack;

                if (nodeHighlighted[i])
                {
                    fore = ConsoleColor.Black;
                    back = ConsoleColor.Green;
                }

                if (i == selected)
                {
                    fore = ConsoleColor.Black;
                    back = ConsoleColor.White;
                }

                buffer.Text(ox + nodeX[i], oy + nodeY[i], "[" + labels[i] + "]", fore, back);
            }
        }

        // Traza una arista con Bresenham y pone la flecha si es dirigida.
        private static void DrawEdge(ScreenBuffer buffer, Glyphs glyphs, int ox, int oy, int[] nx, int[] ny,
                                     string[] labels, int u, int v, bool twoWay, ConsoleColor color)
        {
            int widthU = labels[u].Length + 2;
            int widthV = labels[v].Length + 2;
            int x0 = nx[u] + widthU / 2, y0 = ny[u];
            int x1 = nx[v] + widthV / 2, y1 = ny[v];

            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int capacity = Math.Max(dx, dy) + 1;
            int[] pxs = new int[capacity];
            int[] pys = new int[capacity];
            int count = 0;
            int x = x0, y = y0;

            while (true)
            {
                pxs[count] = x;
                pys[count] = y;
                count++;

                if (x == x1 && y == y1)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            int arrowIndex = -1;
            if (!twoWay)
            {
                for (int k = count - 1; k >= 0; k--)
                {
                    if (!Inside(pxs[k], pys[k], nx[u], ny[u], widthU) && !Inside(pxs[k], pys[k], nx[v], ny[v], widthV))
                    {
                        arrowIndex = k;
                        break;
                    }
                }
            }

            for (int k = 0; k < count; k++)
            {
                if (Inside(pxs[k], pys[k], nx[u], ny[u], widthU) || Inside(pxs[k], pys[k], nx[v], ny[v], widthV))
                {
                    continue;
                }

                int next = k < count - 1 ? k + 1 : k - 1;
                int stepX = next >= 0 ? (k < count - 1 ? pxs[next] - pxs[k] : pxs[k] - pxs[next]) : 0;
                int stepY = next >= 0 ? (k < count - 1 ? pys[next] - pys[k] : pys[k] - pys[next]) : 0;

                char glyph = k == arrowIndex
                    ? ArrowFor(glyphs, stepX, stepY, x1 - x0, y1 - y0)
                    : LineFor(glyphs, stepX, stepY);

                int cellX = ox + pxs[k], cellY = oy + pys[k];
                char existing = buffer.CharAt(cellX, cellY);

                if (k != arrowIndex && glyphs.IsLineGlyph(existing) && existing != glyph)
                {
                    glyph = glyphs.Cross;
                }

                buffer.Put(cellX, cellY, glyph, color, ScreenBuffer.DefaultBack);
            }
        }

        // Indica si la celda cae dentro de la etiqueta del nodo.
        private static bool Inside(int x, int y, int nodeX, int nodeY, int width)
            => y == nodeY && x >= nodeX && x < nodeX + width;

        // Elige el carácter de línea según la dirección del trazo.
        private static char LineFor(Glyphs glyphs, int stepX, int stepY)
        {
            if (stepY == 0)
            {
                return glyphs.Horizontal;
            }

            if (stepX == 0)
            {
                return glyphs.Vertical;
            }

            return (stepX > 0) == (stepY > 0) ? glyphs.DiagonalDown : glyphs.DiagonalUp;
        }

        // Elige la flecha según la dirección final de la arista.
        private static char ArrowFor(Glyphs glyphs, int stepX, int stepY, int totalX, int totalY)
        {
            bool horizontal = stepY == 0 || (stepX != 0 && Math.Abs(totalX) >= 2 * Math.Abs(totalY));
            if (horizontal)
            {
                return (stepX != 0 ? stepX : totalX) >= 0 ? glyphs.ArrowRight : glyphs.ArrowLeft;
            }

            return (stepY != 0 ? stepY : totalY) >= 0 ? glyphs.ArrowDown : glyphs.ArrowUp;
        }
    }
}
