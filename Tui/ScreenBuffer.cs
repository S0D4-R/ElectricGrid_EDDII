using System;
using System.Text;

namespace ElectricGrid.Tui
{
    /// <summary>
    /// Cuadrícula de caracteres con colores que se dibuja fuera de pantalla. Al volcarla solo se escriben
    /// en la terminal las celdas que cambiaron desde el cuadro anterior, lo que evita parpadeo.
    /// </summary>
    internal sealed class ScreenBuffer
    {
        private readonly char[] _chars;
        private readonly ConsoleColor[] _fore;
        private readonly ConsoleColor[] _back;
        private readonly char[] _shownChars;
        private readonly ConsoleColor[] _shownFore;
        private readonly ConsoleColor[] _shownBack;

        public ScreenBuffer(int width, int height)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            int size = Width * Height;
            _chars = new char[size];
            _fore = new ConsoleColor[size];
            _back = new ConsoleColor[size];
            _shownChars = new char[size];
            _shownFore = new ConsoleColor[size];
            _shownBack = new ConsoleColor[size];
            Clear();
        }

        public int Width { get; }

        public int Height { get; }

        public static ConsoleColor DefaultFore => ConsoleColor.Gray;

        public static ConsoleColor DefaultBack => ConsoleColor.Black;

        /// <summary>Rellena todo el búfer con espacios.</summary>
        public void Clear()
        {
            Fill(0, 0, Width, Height, ' ', DefaultFore, DefaultBack);
        }

        /// <summary>Devuelve el carácter de una celda, o un espacio si está fuera de rango.</summary>
        public char CharAt(int x, int y) => InBounds(x, y) ? _chars[y * Width + x] : ' ';

        /// <summary>Escribe un carácter con colores en una celda.</summary>
        public void Put(int x, int y, char c, ConsoleColor fore, ConsoleColor back)
        {
            if (!InBounds(x, y))
            {
                return;
            }

            int i = y * Width + x;
            _chars[i] = char.IsControl(c) ? ' ' : c;
            _fore[i] = fore;
            _back[i] = back;
        }

        /// <summary>Escribe un texto horizontal, recortado al ancho máximo indicado.</summary>
        public void Text(int x, int y, string text, ConsoleColor fore, ConsoleColor back, int maxWidth = int.MaxValue)
        {
            int length = Math.Min(text.Length, maxWidth);
            for (int i = 0; i < length; i++)
            {
                Put(x + i, y, text[i], fore, back);
            }
        }

        /// <summary>Rellena un rectángulo con un carácter y colores.</summary>
        public void Fill(int x, int y, int w, int h, char c, ConsoleColor fore, ConsoleColor back)
        {
            for (int row = y; row < y + h; row++)
            {
                for (int col = x; col < x + w; col++)
                {
                    Put(col, row, c, fore, back);
                }
            }
        }

        /// <summary>Dibuja un recuadro de línea simple con título opcional y limpia su interior.</summary>
        public void Box(int x, int y, int w, int h, string title, Glyphs g, ConsoleColor border, ConsoleColor fill)
        {
            if (w < 2 || h < 2)
            {
                return;
            }

            Fill(x, y, w, h, ' ', border, fill);

            for (int col = x + 1; col < x + w - 1; col++)
            {
                Put(col, y, g.Horizontal, border, fill);
                Put(col, y + h - 1, g.Horizontal, border, fill);
            }

            for (int row = y + 1; row < y + h - 1; row++)
            {
                Put(x, row, g.Vertical, border, fill);
                Put(x + w - 1, row, g.Vertical, border, fill);
            }

            Put(x, y, g.TopLeft, border, fill);
            Put(x + w - 1, y, g.TopRight, border, fill);
            Put(x, y + h - 1, g.BottomLeft, border, fill);
            Put(x + w - 1, y + h - 1, g.BottomRight, border, fill);

            if (title.Length > 0 && w > 6)
            {
                Text(x + 2, y, " " + title + " ", ConsoleColor.White, fill, w - 4);
            }
        }

        /// <summary>Envía a la terminal únicamente las celdas que difieren de lo que ya se muestra.</summary>
        public void Flush()
        {
            StringBuilder run = new StringBuilder();

            for (int y = 0; y < Height; y++)
            {
                int x = 0;
                while (x < Width)
                {
                    if (!IsDirty(x, y) || IsLastCell(x, y))
                    {
                        x++;
                        continue;
                    }

                    int start = x;
                    int first = y * Width + x;
                    ConsoleColor fore = _fore[first];
                    ConsoleColor back = _back[first];
                    run.Clear();

                    while (x < Width && IsDirty(x, y) && !IsLastCell(x, y)
                           && _fore[y * Width + x] == fore && _back[y * Width + x] == back)
                    {
                        int i = y * Width + x;
                        run.Append(_chars[i]);
                        _shownChars[i] = _chars[i];
                        _shownFore[i] = fore;
                        _shownBack[i] = back;
                        x++;
                    }

                    Console.SetCursorPosition(start, y);
                    Console.ForegroundColor = fore;
                    Console.BackgroundColor = back;
                    Console.Write(run.ToString());
                }
            }
        }

        private bool IsDirty(int x, int y)
        {
            int i = y * Width + x;
            return _chars[i] != _shownChars[i] || _fore[i] != _shownFore[i] || _back[i] != _shownBack[i];
        }

        private bool IsLastCell(int x, int y) => x == Width - 1 && y == Height - 1;

        private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
    }
}
