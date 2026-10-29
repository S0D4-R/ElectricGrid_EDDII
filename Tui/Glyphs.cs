namespace ElectricGrid.Tui
{
    // Caracteres para dibujar los paneles y las aristas.
    internal sealed class Glyphs
    {
        // Guarda el juego de caracteres elegido.
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
        public char DiagonalDown { get; }
        public char DiagonalUp { get; }
        public char Cross { get; }

        public char ArrowRight => '>';
        public char ArrowLeft => '<';
        public char ArrowUp => '^';
        public char ArrowDown => 'v';

        public static Glyphs Unicode { get; } = new Glyphs(true, '─', '│', '┌', '┐', '└', '┘', '╲', '╱', '┼');

        public static Glyphs Ascii { get; } = new Glyphs(false, '-', '|', '+', '+', '+', '+', '\\', '/', '+');

        // Indica si el carácter sirve para dibujar una arista.
        public bool IsLineGlyph(char c)
            => c == Horizontal || c == Vertical || c == DiagonalDown || c == DiagonalUp || c == Cross;
    }
}
