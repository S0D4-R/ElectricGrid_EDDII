namespace ElectricGrid.ConsoleApp.Tui
{
    /// <summary>
    /// Character set used to draw boxes and graph edges. A pure ASCII set is provided for
    /// terminals or fonts without box-drawing support.
    /// </summary>
    internal sealed class Glyphs
    {
        private Glyphs(bool unicode, char h, char v, char tl, char tr, char bl, char br,
                       char diagDown, char diagUp, char cross)
        {
            IsUnicode = unicode;
            Horizontal = h;
            Vertical = v;
            TopLeft = tl;
            TopRight = tr;
            BottomLeft = bl;
            BottomRight = br;
            DiagonalDown = diagDown;
            DiagonalUp = diagUp;
            Cross = cross;
        }

        public bool IsUnicode { get; }
        public char Horizontal { get; }
        public char Vertical { get; }
        public char TopLeft { get; }
        public char TopRight { get; }
        public char BottomLeft { get; }
        public char BottomRight { get; }

        /// <summary>Line going from top-left to bottom-right.</summary>
        public char DiagonalDown { get; }

        /// <summary>Line going from bottom-left to top-right.</summary>
        public char DiagonalUp { get; }

        /// <summary>Drawn where two edges cross.</summary>
        public char Cross { get; }

        // Arrow heads are plain ASCII in both sets: they have a single-cell width in every terminal.
        public char ArrowRight => '>';
        public char ArrowLeft => '<';
        public char ArrowUp => '^';
        public char ArrowDown => 'v';

        public static Glyphs Unicode { get; } = new Glyphs(true, '─', '│', '┌', '┐', '└', '┘', '╲', '╱', '┼');

        public static Glyphs Ascii { get; } = new Glyphs(false, '-', '|', '+', '+', '+', '+', '\\', '/', '+');

        public bool IsLineGlyph(char c)
            => c == Horizontal || c == Vertical || c == DiagonalDown || c == DiagonalUp || c == Cross;
    }
}
