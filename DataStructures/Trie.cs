//Rodrigo Herman

using System;
using System.Text;

namespace ElectricGrid.DataStructures
{
    // Coincidencia del Trie: texto guardado y valor asociado.
    public readonly struct TrieMatch(string text, string payload)
    {
        public string Text { get; } = text ?? string.Empty;

        public string Payload { get; } = payload ?? string.Empty;
    }

    // Árbol de prefijos para sugerir nombres de subestación.
    public sealed class Trie
    {
        // Nodo del Trie con sus hijos y los valores del texto que termina aquí.
        private sealed class Node(char key)
        {
            public char Key { get; } = key;
            public Node[]? Children { get; set; }
            public int ChildCount { get; set; }

            public string[]? Payloads { get; set; }
            public int PayloadCount { get; set; }

            // Devuelve la posición del hijo con esa letra, o -1.
            public int GetChildIndex(char c)
            {
                if (Children is not null)
                {
                    for (int i = 0; i < ChildCount; i++)
                    {
                        if (Children[i].Key == c) return i;
                    }
                }
                return -1;
            }

            // Agrega un hijo y devuelve el nodo creado.
            public Node AddChild(char c)
            {
                if (Children is null) Children = new Node[2];
                else if (ChildCount == Children.Length)
                {
                    Node[] newArr = new Node[Children.Length * 2];
                    Array.Copy(Children, newArr, ChildCount);
                    Children = newArr;
                }

                Node n = new(c);
                Children[ChildCount++] = n;
                return n;
            }

            // Quita un hijo y desplaza los siguientes.
            public void RemoveChildAt(int index)
            {
                if (Children is null) return;

                for (int i = index; i < ChildCount - 1; i++)
                {
                    Children[i] = Children[i + 1];
                }
                Children[ChildCount - 1] = null!;
                ChildCount--;
            }

            // Agrega un valor si no estaba repetido.
            public bool AddPayload(string payload)
            {
                if (Payloads is not null)
                {
                    for (int i = 0; i < PayloadCount; i++)
                    {
                        if (string.Equals(Payloads[i], payload, StringComparison.Ordinal)) return false;
                    }
                }

                if (Payloads is null) Payloads = new string[1];
                else if (PayloadCount == Payloads.Length)
                {
                    string[] newArr = new string[Payloads.Length == 0 ? 1 : Payloads.Length * 2];
                    Array.Copy(Payloads, newArr, PayloadCount);
                    Payloads = newArr;
                }

                Payloads[PayloadCount++] = payload;
                return true;
            }

            // Quita un valor concreto del nodo.
            public bool RemovePayload(string payload)
            {
                if (Payloads is not null)
                {
                    for (int i = 0; i < PayloadCount; i++)
                    {
                        if (string.Equals(Payloads[i], payload, StringComparison.Ordinal))
                        {
                            for (int j = i; j < PayloadCount - 1; j++)
                            {
                                Payloads[j] = Payloads[j + 1];
                            }
                            Payloads[PayloadCount - 1] = null!;
                            PayloadCount--;
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private readonly Node _root = new('\0');
        private int _count = 0;
        private int _nodeCount = 1;

        public int Count => _count;

        public int NodeCount => _nodeCount;

        // Crea el camino del texto y guarda el valor al final.
        public bool Insert(string? text, string? payload)
        {
            text ??= string.Empty;
            payload ??= string.Empty;

            Node current = _root;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int childIndex = current.GetChildIndex(c);
                if (childIndex >= 0)
                {
                    current = current.Children![childIndex];
                }
                else
                {
                    current = current.AddChild(c);
                    _nodeCount++;
                }
            }

            if (current.AddPayload(payload))
            {
                _count++;
                return true;
            }
            return false;
        }

        // Indica si el texto tiene al menos un valor guardado.
        public bool Contains(string? text)
        {
            text ??= string.Empty;

            Node current = _root;
            for (int i = 0; i < text.Length; i++)
            {
                int childIndex = current.GetChildIndex(text[i]);
                if (childIndex >= 0)
                {
                    current = current.Children![childIndex];
                }
                else
                {
                    return false;
                }
            }
            return current.PayloadCount > 0;
        }

        // Elimina un valor y poda las ramas que quedan vacías.
        public bool Remove(string? text, string? payload)
        {
            text ??= string.Empty;
            payload ??= string.Empty;

            bool removed = RemoveRecursive(_root, text, 0, payload);
            if (removed)
            {
                _count--;
            }
            return removed;
        }

        // Baja hasta el nodo del texto y elimina el valor.
        private bool RemoveRecursive(Node current, string text, int index, string payload)
        {
            if (index == text.Length)
            {
                return current.RemovePayload(payload);
            }

            char c = text[index];
            int childIndex = current.GetChildIndex(c);
            if (childIndex < 0) return false;

            Node child = current.Children![childIndex];
            bool removed = RemoveRecursive(child, text, index + 1, payload);

            if (removed)
            {
                if (child.ChildCount == 0 && child.PayloadCount == 0)
                {
                    current.RemoveChildAt(childIndex);
                    _nodeCount--;
                }
            }
            return removed;
        }

        // Devuelve hasta maxResults coincidencias que empiecen por el prefijo.
        public TrieMatch[] SearchByPrefix(string? prefix, int maxResults)
        {
            prefix ??= string.Empty;
            if (maxResults <= 0) return Array.Empty<TrieMatch>();

            Node current = _root;
            for (int i = 0; i < prefix.Length; i++)
            {
                int childIndex = current.GetChildIndex(prefix[i]);
                if (childIndex >= 0)
                {
                    current = current.Children![childIndex];
                }
                else
                {
                    return Array.Empty<TrieMatch>();
                }
            }

            int arraySize = Math.Min(maxResults, _count);
            TrieMatch[] buffer = new TrieMatch[arraySize];
            int found = 0;

            Dfs(current, prefix, buffer, ref found, maxResults);

            if (found != buffer.Length)
            {
                TrieMatch[] result = new TrieMatch[found];
                Array.Copy(buffer, result, found);
                return result;
            }

            return buffer;
        }

        // Devuelve solo los textos de las coincidencias.
        public string[] GetSuggestions(string? prefix, int maxResults = 10)
        {
            if (string.IsNullOrEmpty(prefix)) return Array.Empty<string>();

            TrieMatch[] matches = SearchByPrefix(prefix, maxResults);
            string[] suggestions = new string[matches.Length];
            for (int i = 0; i < matches.Length; i++)
            {
                suggestions[i] = matches[i].Text;
            }
            return suggestions;
        }

        // Atajo de SearchByPrefix con un límite de 10.
        public TrieMatch[] SearchByPrefix(string? prefix)
        {
            return SearchByPrefix(prefix, 10);
        }

        // Devuelve todas las entradas guardadas en el árbol.
        public TrieMatch[] GetAll()
        {
            TrieMatch[] buffer = new TrieMatch[_count];
            int found = 0;
            Dfs(_root, string.Empty, buffer, ref found, _count);
            return buffer;
        }

        // Recoge los valores del nodo y sigue por sus hijos.
        private static void Dfs(Node node, string currentText, TrieMatch[] buffer, ref int found, int maxResults)
        {
            if (found >= maxResults) return;

            for (int i = 0; i < node.PayloadCount; i++)
            {
                if (found >= maxResults) return;
                buffer[found++] = new TrieMatch(currentText, node.Payloads![i]);
            }

            for (int i = 0; i < node.ChildCount; i++)
            {
                if (found >= maxResults) return;
                Dfs(node.Children![i], currentText + node.Children[i].Key, buffer, ref found, maxResults);
            }
        }

        // Resume cuántos registros y nodos tiene el Trie.
        public string Describe()
        {
            return $"Trie en memoria: {_count} registros vinculados en {_nodeCount} nodos totales.";
        }

        // Dibuja el árbol en texto, una rama por línea.
        public string ObtenerEstructuraVisual()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("(raíz) [*] = fin de palabra");

            for (int i = 0; i < _root.ChildCount; i++)
            {
                GenerarRama(_root.Children![i], sb, "");
            }

            return sb.ToString();
        }

        // Escribe un nodo y se llama para cada hijo.
        private void GenerarRama(Node nodo, StringBuilder sb, string sangria)
        {
            if (nodo == null) return;

            sb.Append(sangria);

            sb.Append("  └─ ");
            sb.Append(nodo.Key);

            if (nodo.PayloadCount > 0)
            {
                sb.Append(" [*]");
            }
            sb.AppendLine();

            for (int i = 0; i < nodo.ChildCount; i++)
            {
                GenerarRama(nodo.Children![i], sb, sangria + "    ");
            }
        }
    }
}
