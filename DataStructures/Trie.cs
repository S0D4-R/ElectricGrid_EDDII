using System;

namespace ElectricGrid.DataStructures
{
    /// <summary>Sugerencia devuelta por una búsqueda por prefijo: texto original y código asociado.</summary>
    public readonly struct TrieMatch(string text, string payload)
    {
        /// <summary>Texto insertado (nombre de la subestación).</summary>
        public string Text { get; } = text ?? string.Empty;

        /// <summary>Valor asociado al texto (código de la subestación).</summary>
        public string Payload { get; } = payload ?? string.Empty;
    }

    /// <summary>
    /// Árbol de prefijos para sugerir nombres de subestación mientras se escribe.
    /// Implementado desde cero manejando memoria con arreglos primitivos.
    /// </summary>
    public sealed class Trie
    {
        // Estructura Interna del Nodo
        private sealed class Node(char key)
        {
            public char Key { get; } = key;
            public Node[]? Children { get; set; }
            public int ChildCount { get; set; }

            public string[]? Payloads { get; set; }
            public int PayloadCount { get; set; }

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

            public bool AddPayload(string payload)
            {
                if (Payloads is not null)
                {
                    for (int i = 0; i < PayloadCount; i++)
                    {
                        if (string.Equals(Payloads[i], payload, StringComparison.Ordinal)) return false; // Ya existe
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

        // Estado del Trie
        private readonly Node _root = new('\0');
        private int _count = 0;
        private int _nodeCount = 1; // La raíz cuenta como 1

        /// <summary>Cantidad de entradas almacenadas.</summary>
        public int Count => _count;

        /// <summary>Cantidad de nodos del árbol, incluida la raíz.</summary>
        public int NodeCount => _nodeCount;

        /// <summary>Inserta un par (texto, valor); devuelve falso si el par ya existe.</summary>
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

        /// <summary>Indica si el texto está almacenado con cualquier valor.</summary>
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

        /// <summary>Elimina un par (texto, valor) y poda las ramas vacías; devuelve falso si no existía.</summary>
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
                // Podar (Pruning): Si el hijo quedó vacío, lo quitamos para ahorrar memoria
                if (child.ChildCount == 0 && child.PayloadCount == 0)
                {
                    current.RemoveChildAt(childIndex);
                    _nodeCount--;
                }
            }
            return removed;
        }

        /// <summary>Devuelve coincidencias cuyo texto comienza con el prefijo.</summary>
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
                    return Array.Empty<TrieMatch>(); // El prefijo no existe
                }
            }

            // Para no reservar demasiada memoria en vano, el límite del buffer
            // será el menor valor entre maxResults y el total de datos en el Trie.
            int arraySize = Math.Min(maxResults, _count);
            TrieMatch[] buffer = new TrieMatch[arraySize];
            int found = 0;

            Dfs(current, prefix, buffer, ref found, maxResults);

            // Si los encontrados son menos que el tamaño del arreglo, lo truncamos.
            if (found != buffer.Length)
            {
                TrieMatch[] result = new TrieMatch[found];
                Array.Copy(buffer, result, found);
                return result;
            }

            return buffer;
        }

        /// <summary>Versión práctica para la interfaz: devuelve hasta maxResults sugerencias usando solo el texto.</summary>
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

        /// <summary>Devuelve sugerencias con texto y código asociado; útil para mostrar la lista de resultados con payload.</summary>
        public TrieMatch[] SearchByPrefix(string? prefix)
        {
            return SearchByPrefix(prefix, 10);
        }

        /// <summary>Recorre todo el árbol y devuelve todas las entradas.</summary>
        public TrieMatch[] GetAll()
        {
            TrieMatch[] buffer = new TrieMatch[_count];
            int found = 0;
            Dfs(_root, string.Empty, buffer, ref found, _count);
            return buffer;
        }

        /// <summary>Búsqueda en profundidad (DFS) para encontrar sufijos y valores asociados.</summary>
        private static void Dfs(Node node, string currentText, TrieMatch[] buffer, ref int found, int maxResults)
        {
            if (found >= maxResults) return;

            // Extraer las cargas (payloads) del nodo actual
            for (int i = 0; i < node.PayloadCount; i++)
            {
                if (found >= maxResults) return;
                buffer[found++] = new TrieMatch(currentText, node.Payloads![i]);
            }

            // Continuar bajando por los hijos
            for (int i = 0; i < node.ChildCount; i++)
            {
                if (found >= maxResults) return;
                Dfs(node.Children![i], currentText + node.Children[i].Key, buffer, ref found, maxResults);
            }
        }

        /// <summary>Genera una descripción imprimible del contenido y tamaño del árbol.</summary>
        public string Describe()
        {
            return $"Trie en memoria: {_count} registros vinculados en {_nodeCount} nodos totales.";
        }
    }
}