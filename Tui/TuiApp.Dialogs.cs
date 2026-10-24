using System;
using System.Text;
using ElectricGrid.Collections;
using ElectricGrid.Models;

namespace ElectricGrid.Tui
{
    internal sealed partial class TuiApp
    {
        private const ConsoleColor DialogBack = ConsoleColor.DarkBlue;

        private enum FieldKind
        {
            Text,
            Substation
        }

        private sealed class FormField
        {
            public FormField(string label, string value, FieldKind kind, string help = "")
            {
                Label = label;
                Value = value;
                Kind = kind;
                Help = help;
            }

            public string Label { get; }

            public string Value { get; set; }

            public FieldKind Kind { get; }

            public string Help { get; }
        }

        /// <summary>Muestra un cuadro de mensaje y espera cualquier tecla.</summary>
        private void ShowMessage(string title, string[] lines, ConsoleColor accent)
        {
            int width = Math.Min(_width - 4, Math.Max(36, MaxLength(lines) + 4));
            int height = Math.Min(_height - 2, lines.Length + 4);

            while (true)
            {
                DrawFrame();
                int x = (_width - width) / 2, y = (_height - height) / 2;
                _buffer.Box(x, y, width, height, title, _glyphs, accent, DialogBack);

                for (int i = 0; i < lines.Length && i < height - 4; i++)
                {
                    _buffer.Text(x + 2, y + 1 + i, lines[i], ConsoleColor.White, DialogBack, width - 4);
                }

                _buffer.Text(x + 2, y + height - 2, "Presione una tecla...", ConsoleColor.Gray, DialogBack, width - 4);
                _buffer.Flush();

                if (WaitKey() != null)
                {
                    return;
                }
            }
        }

        /// <summary>Muestra una ventana de texto de solo lectura con desplazamiento.</summary>
        private void ShowText(string title, string[] lines)
        {
            int top = 0;

            while (true)
            {
                DrawFrame();
                int width = _width - 6, height = _height - 4;
                int x = 3, y = 2;
                int rows = height - 3;
                int maxTop = Math.Max(0, lines.Length - rows);
                top = Math.Max(0, Math.Min(top, maxTop));

                _buffer.Box(x, y, width, height, title, _glyphs, ConsoleColor.Cyan, DialogBack);
                for (int i = 0; i < rows && top + i < lines.Length; i++)
                {
                    _buffer.Text(x + 2, y + 1 + i, lines[top + i], ConsoleColor.White, DialogBack, width - 4);
                }

                string footer = lines.Length > rows
                    ? $"Flechas/RePág/AvPág desplazar  |  Esc cerrar  |  {top + 1}-{Math.Min(lines.Length, top + rows)} de {lines.Length}"
                    : "Esc cerrar";
                _buffer.Text(x + 2, y + height - 2, footer, ConsoleColor.Gray, DialogBack, width - 4);
                _buffer.Flush();

                ConsoleKeyInfo? key = WaitKey();
                if (key == null)
                {
                    continue;
                }

                switch (key.Value.Key)
                {
                    case ConsoleKey.UpArrow: top--; break;
                    case ConsoleKey.DownArrow: top++; break;
                    case ConsoleKey.PageUp: top -= rows; break;
                    case ConsoleKey.PageDown: top += rows; break;
                    case ConsoleKey.Home: top = 0; break;
                    case ConsoleKey.End: top = maxTop; break;
                    case ConsoleKey.Escape:
                    case ConsoleKey.Enter:
                    case ConsoleKey.Q:
                        return;
                }
            }
        }

        /// <summary>Hace una pregunta de sí o no; Enter elige la opción por defecto.</summary>
        private bool Confirm(string question, bool defaultYes)
        {
            string[] lines = Wrap(question, 50);
            int width = Math.Min(_width - 4, Math.Max(34, MaxLength(lines) + 4));
            int height = lines.Length + 5;

            while (true)
            {
                DrawFrame();
                int x = (_width - width) / 2, y = (_height - height) / 2;
                _buffer.Box(x, y, width, height, "Confirmar", _glyphs, ConsoleColor.Yellow, DialogBack);

                for (int i = 0; i < lines.Length; i++)
                {
                    _buffer.Text(x + 2, y + 1 + i, lines[i], ConsoleColor.White, DialogBack, width - 4);
                }

                string choices = defaultYes ? "[S]í (Enter)   [N]o" : "[S]í   [N]o (Enter)";
                _buffer.Text(x + 2, y + height - 2, choices, ConsoleColor.Cyan, DialogBack, width - 4);
                _buffer.Flush();

                ConsoleKeyInfo? key = WaitKey();
                if (key == null)
                {
                    continue;
                }

                switch (key.Value.Key)
                {
                    case ConsoleKey.S:
                    case ConsoleKey.Y:
                        return true;
                    case ConsoleKey.N:
                    case ConsoleKey.Escape:
                        return false;
                    case ConsoleKey.Enter:
                        return defaultYes;
                }
            }
        }

        /// <summary>Permite elegir una opción de una lista; devuelve su índice, o -1 si se cancela.</summary>
        private int Pick(string title, string[] options)
        {
            int selected = 0;
            int width = Math.Min(_width - 4, Math.Max(52, MaxLength(options) + 8));
            int height = options.Length + 4;

            while (true)
            {
                DrawFrame();
                int x = (_width - width) / 2, y = (_height - height) / 2;
                _buffer.Box(x, y, width, height, title, _glyphs, ConsoleColor.Cyan, DialogBack);

                for (int i = 0; i < options.Length; i++)
                {
                    bool active = i == selected;
                    ConsoleColor fore = active ? ConsoleColor.Black : ConsoleColor.White;
                    ConsoleColor back = active ? ConsoleColor.Cyan : DialogBack;
                    _buffer.Fill(x + 1, y + 1 + i, width - 2, 1, ' ', fore, back);
                    _buffer.Text(x + 2, y + 1 + i, options[i], fore, back, width - 4);
                }

                _buffer.Text(x + 2, y + height - 2, "Flechas elegir  Enter seleccionar  Esc cancelar", ConsoleColor.Gray, DialogBack, width - 4);
                _buffer.Flush();

                ConsoleKeyInfo? key = WaitKey();
                if (key == null)
                {
                    continue;
                }

                switch (key.Value.Key)
                {
                    case ConsoleKey.UpArrow: selected = (selected + options.Length - 1) % options.Length; break;
                    case ConsoleKey.DownArrow: selected = (selected + 1) % options.Length; break;
                    case ConsoleKey.Enter: return selected;
                    case ConsoleKey.Escape: return -1;
                }
            }
        }

        /// <summary>
        /// Muestra un formulario de varios campos y devuelve los valores, o nulo si se cancela.
        /// Escribir sobre un valor precargado lo reemplaza; los campos de subestación muestran una vista previa.
        /// </summary>
        private string[]? ShowForm(string title, FormField[] fields)
        {
            int active = 0;
            bool fresh = true;
            int labelWidth = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                labelWidth = Math.Max(labelWidth, fields[i].Label.Length);
            }

            int width = Math.Min(_width - 4, 66);
            int height = fields.Length * 2 + 4;

            while (true)
            {
                DrawFrame();
                int x = (_width - width) / 2, y = (_height - height) / 2;
                _buffer.Box(x, y, width, height, title, _glyphs, ConsoleColor.Cyan, DialogBack);

                int inputX = x + 3 + labelWidth;
                int inputWidth = Math.Max(4, width - labelWidth - 6);

                for (int i = 0; i < fields.Length; i++)
                {
                    int rowY = y + 1 + i * 2;
                    bool isActive = i == active;

                    _buffer.Text(x + 2, rowY, fields[i].Label + ":", isActive ? ConsoleColor.Yellow : ConsoleColor.Gray, DialogBack);

                    bool replaceOnType = isActive && fresh && fields[i].Value.Length > 0;
                    string shown = fields[i].Value;
                    if (isActive && !replaceOnType)
                    {
                        shown += "_";
                    }

                    if (shown.Length > inputWidth)
                    {
                        shown = shown.Substring(shown.Length - inputWidth);
                    }

                    ConsoleColor inputFore = isActive ? ConsoleColor.Black : ConsoleColor.White;
                    ConsoleColor inputBack = replaceOnType ? ConsoleColor.Cyan : (isActive ? ConsoleColor.Gray : ConsoleColor.DarkGray);
                    _buffer.Fill(inputX, rowY, inputWidth, 1, ' ', inputFore, inputBack);
                    _buffer.Text(inputX, rowY, shown, inputFore, inputBack, inputWidth);

                    DrawFieldHint(fields[i], inputX, rowY + 1, inputWidth);
                }

                _buffer.Text(x + 2, y + height - 2, "Enter siguiente/aceptar  Tab/Flechas mover  Esc cancelar", ConsoleColor.Gray, DialogBack, width - 4);
                _buffer.Flush();

                ConsoleKeyInfo? key = WaitKey();
                if (key == null)
                {
                    continue;
                }

                ConsoleKeyInfo pressed = key.Value;
                bool shift = (pressed.Modifiers & ConsoleModifiers.Shift) != 0;
                int previousField = active;

                switch (pressed.Key)
                {
                    case ConsoleKey.Escape:
                        return null;
                    case ConsoleKey.Enter:
                        if (active == fields.Length - 1)
                        {
                            return CollectValues(fields);
                        }

                        active++;
                        break;
                    case ConsoleKey.Tab:
                        active = shift ? (active + fields.Length - 1) % fields.Length : (active + 1) % fields.Length;
                        break;
                    case ConsoleKey.DownArrow:
                        active = (active + 1) % fields.Length;
                        break;
                    case ConsoleKey.UpArrow:
                        active = (active + fields.Length - 1) % fields.Length;
                        break;
                    case ConsoleKey.Backspace:
                        if (fresh)
                        {
                            fields[active].Value = string.Empty;
                            fresh = false;
                        }
                        else if (fields[active].Value.Length > 0)
                        {
                            fields[active].Value = fields[active].Value.Substring(0, fields[active].Value.Length - 1);
                        }

                        break;
                    default:
                        if (!char.IsControl(pressed.KeyChar))
                        {
                            if (fresh)
                            {
                                fields[active].Value = string.Empty;
                                fresh = false;
                            }

                            fields[active].Value += pressed.KeyChar;
                        }

                        break;
                }

                if (active != previousField)
                {
                    fresh = true;
                }
            }
        }

        private void DrawFieldHint(FormField field, int x, int y, int width)
        {
            if (field.Kind == FieldKind.Substation)
            {
                string text = field.Value.Trim();
                if (text.Length == 0)
                {
                    _buffer.Text(x, y, "escriba el código de una subestación", ConsoleColor.DarkGray, DialogBack, width);
                    return;
                }

                string? code = ResolveCode(text);
                if (code != null)
                {
                    Substation? found = _service.FindSubstation(code);
                    string description = found == null ? code : $"{found.Code}  {found.Name}  ({found.CapacityMw:F1} MW)";
                    _buffer.Text(x, y, "ok: " + description, ConsoleColor.Green, DialogBack, width);
                }
                else
                {
                    string message = _service.SupportsAutocomplete ? "sin coincidencia única" : "no existe una subestación con ese código";
                    _buffer.Text(x, y, message, ConsoleColor.Red, DialogBack, width);
                }
            }
            else if (field.Help.Length > 0)
            {
                _buffer.Text(x, y, field.Help, ConsoleColor.DarkGray, DialogBack, width);
            }
        }

        private static string[] CollectValues(FormField[] fields)
        {
            string[] values = new string[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                values[i] = fields[i].Value.Trim();
            }

            return values;
        }

        /// <summary>Convierte el texto escrito (código o, si hay autocompletado, nombre) en un código de subestación; nulo si no hay coincidencia única.</summary>
        private string? ResolveCode(string text)
        {
            Substation? byCode = _service.FindSubstation(text);
            if (byCode != null)
            {
                return byCode.Code;
            }

            if (!_service.SupportsAutocomplete)
            {
                return null;
            }

            Substation[] candidates = _service.SuggestSubstations(text, 10);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (string.Equals(candidates[i].Name, text, StringComparison.OrdinalIgnoreCase))
                {
                    return candidates[i].Code;
                }
            }

            return candidates.Length == 1 ? candidates[0].Code : null;
        }

        private static int MaxLength(string[] lines)
        {
            int max = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                max = Math.Max(max, lines[i].Length);
            }

            return max;
        }

        /// <summary>Divide un texto en líneas que no superan el ancho indicado, cortando por palabras.</summary>
        private static string[] Wrap(string text, int width)
        {
            ArrayBuffer<string> lines = new ArrayBuffer<string>();
            string[] paragraphs = text.Replace("\r", string.Empty).Split('\n');

            for (int p = 0; p < paragraphs.Length; p++)
            {
                string[] words = paragraphs[p].Split(' ');
                StringBuilder current = new StringBuilder();

                for (int w = 0; w < words.Length; w++)
                {
                    if (current.Length > 0 && current.Length + 1 + words[w].Length > width)
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                    }

                    if (current.Length > 0)
                    {
                        current.Append(' ');
                    }

                    current.Append(words[w]);
                }

                lines.Add(current.ToString());
            }

            return lines.ToArray();
        }
    }
}
