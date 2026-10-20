using System;
using System.Globalization;
using System.Text;
using ElectricGrid.Core.Models;
using ElectricGrid.Core.Services;

namespace ElectricGrid.ConsoleApp.UI
{
    /// <summary>
    /// Keyboard input helpers for the console front end. All validation loops live here so the
    /// menu handlers stay short, and none of this code belongs to the Core library (a GUI
    /// would replace this class with its own controls).
    /// </summary>
    internal sealed class ConsoleInput
    {
        private const int MaxHints = 5;
        private readonly IPowerGridService _service;

        public ConsoleInput(IPowerGridService service)
        {
            _service = service;
        }

        /// <summary>Reads a line; returns <paramref name="defaultValue"/> when the user just presses Enter.</summary>
        public string ReadText(string prompt, string? defaultValue = null)
        {
            string suffix = defaultValue == null ? string.Empty : $" [{defaultValue}]";
            Console.Write($"{prompt}{suffix}: ");
            string? line = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
            {
                return defaultValue ?? string.Empty;
            }

            return line.Trim();
        }

        /// <summary>Reads a number greater than or equal to <paramref name="minimum"/>, repeating until valid.</summary>
        public double ReadNumber(string prompt, double minimum)
        {
            while (true)
            {
                string text = ReadText(prompt);
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && value >= minimum)
                {
                    return value;
                }

                ConsolePresenter.Warning($"Please enter a number greater than or equal to {minimum.ToString(CultureInfo.InvariantCulture)} (use '.' as decimal separator).");
            }
        }

        /// <summary>Reads a yes/no answer.</summary>
        public bool ReadYesNo(string prompt, bool defaultValue)
        {
            while (true)
            {
                string answer = ReadText($"{prompt} (y/n)", defaultValue ? "y" : "n").ToLowerInvariant();
                if (answer == "y" || answer == "yes" || answer == "s" || answer == "si")
                {
                    return true;
                }

                if (answer == "n" || answer == "no")
                {
                    return false;
                }

                ConsolePresenter.Warning("Please answer y or n.");
            }
        }

        /// <summary>
        /// Reads a substation, offering live suggestions while the user types (Trie prefix search
        /// followed by Hash Table lookups; available once the Trie is implemented). The operator may type a code or a name; TAB accepts the
        /// first suggestion.
        /// </summary>
        /// <returns>The resolved substation code, or <c>null</c> if nothing could be resolved.</returns>
        public string? ReadSubstation(string label)
        {
            // Live suggestions need the trie; until it exists the operator types the code instead.
            bool liveSuggestions = _service.SupportsAutocomplete && !Console.IsInputRedirected && !Console.IsOutputRedirected;
            string typed = liveSuggestions
                ? ReadWithLiveSuggestions(label)
                : ReadText(_service.SupportsAutocomplete ? label : $"{label} (code)");

            return ResolveToCode(typed);
        }

        /// <summary>Waits for the user to press Enter.</summary>
        public void Pause()
        {
            Console.WriteLine();
            Console.Write("Press ENTER to continue...");
            Console.ReadLine();
        }

        private string ReadWithLiveSuggestions(string label)
        {
            Console.Write($"{label} (type code or name, TAB = first suggestion): ");
            int startLeft = Console.CursorLeft;

            // Reserve the lines used to display the suggestions below the prompt.
            for (int i = 0; i < MaxHints; i++)
            {
                Console.WriteLine();
            }

            int promptTop = Console.CursorTop - MaxHints;
            Console.SetCursorPosition(startLeft, promptTop);

            StringBuilder buffer = new StringBuilder();
            int maxLength = Math.Max(1, Console.WindowWidth - startLeft - 2);

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                    }
                }
                else if (key.Key == ConsoleKey.Tab)
                {
                    Substation[] first = _service.SuggestSubstations(buffer.ToString(), 1);
                    if (first.Length > 0)
                    {
                        buffer.Clear();
                        buffer.Append(first[0].Name);
                    }
                }
                else if (!char.IsControl(key.KeyChar) && buffer.Length < maxLength)
                {
                    buffer.Append(key.KeyChar);
                }

                RenderPromptAndHints(buffer.ToString(), startLeft, promptTop);
            }

            ClearHints(promptTop);
            Console.SetCursorPosition(0, promptTop + 1);
            return buffer.ToString().Trim();
        }

        private void RenderPromptAndHints(string text, int startLeft, int promptTop)
        {
            int width = Math.Max(1, Console.WindowWidth - 1);

            Console.SetCursorPosition(startLeft, promptTop);
            Console.Write(new string(' ', Math.Max(0, width - startLeft)));
            Console.SetCursorPosition(startLeft, promptTop);
            Console.Write(text);

            Substation[] hints = text.Length == 0
                ? new Substation[0]
                : _service.SuggestSubstations(text, MaxHints);

            for (int i = 0; i < MaxHints; i++)
            {
                Console.SetCursorPosition(0, promptTop + 1 + i);
                Console.Write(new string(' ', width));
                Console.SetCursorPosition(0, promptTop + 1 + i);

                if (i < hints.Length)
                {
                    string line = $"   > {hints[i].Code,-8} {hints[i].Name}";
                    Console.Write(line.Length > width ? line.Substring(0, width) : line);
                }
            }

            Console.SetCursorPosition(startLeft + text.Length, promptTop);
        }

        private static void ClearHints(int promptTop)
        {
            int width = Math.Max(1, Console.WindowWidth - 1);
            for (int i = 0; i < MaxHints; i++)
            {
                Console.SetCursorPosition(0, promptTop + 1 + i);
                Console.Write(new string(' ', width));
            }
        }

        /// <summary>Turns what the operator typed (code, exact name or unambiguous prefix) into a substation code.</summary>
        private string? ResolveToCode(string typed)
        {
            if (string.IsNullOrWhiteSpace(typed))
            {
                ConsolePresenter.Warning("Nothing was entered.");
                return null;
            }

            Substation? byCode = _service.FindSubstation(typed);
            if (byCode != null)
            {
                return byCode.Code;
            }

            if (!_service.SupportsAutocomplete)
            {
                ConsolePresenter.Warning($"No substation with code '{typed.ToUpperInvariant()}'. (Search by name requires the Trie, pending implementation.)");
                return null;
            }

            Substation[] candidates = _service.SuggestSubstations(typed, 10);

            for (int i = 0; i < candidates.Length; i++)
            {
                if (string.Equals(candidates[i].Name, typed, StringComparison.OrdinalIgnoreCase))
                {
                    return candidates[i].Code;
                }
            }

            if (candidates.Length == 1)
            {
                return candidates[0].Code;
            }

            if (candidates.Length == 0)
            {
                ConsolePresenter.Warning($"'{typed}' does not match any substation code or name.");
            }
            else
            {
                ConsolePresenter.Warning($"'{typed}' is ambiguous. Matches: {JoinCodes(candidates)}. Type more letters or use the code.");
            }

            return null;
        }

        private static string JoinCodes(Substation[] substations)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < substations.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(substations[i].Code);
            }

            return builder.ToString();
        }
    }
}
