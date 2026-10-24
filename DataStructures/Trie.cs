using System;

namespace ElectricGrid.DataStructures
{
    /// <summary>Sugerencia devuelta por una búsqueda por prefijo: texto original y código asociado.</summary>
    public readonly struct TrieMatch
    {
        /// <summary>Crea una coincidencia con el texto y el valor asociado.</summary>
        public TrieMatch(string text, string payload)
        {
            Text = text;
            Payload = payload;
        }

        /// <summary>Texto insertado (nombre de la subestación).</summary>
        public string Text { get; }

        /// <summary>Valor asociado al texto (código de la subestación).</summary>
        public string Payload { get; }
    }

    /// <summary>
    /// Árbol de prefijos para sugerir nombres de subestación mientras se escribe.
    /// Esqueleto pendiente de implementación que define el contrato que usa el resto del sistema.
    /// </summary>
    /// <remarks>Responsable: Integrante D (Trie). TODO [TRIE]: implementar todos los miembros.</remarks>
    public sealed class Trie
    {
        /// <summary>Cantidad de entradas almacenadas.</summary>
        public int Count => throw new NotImplementedException("El Trie está pendiente de implementación.");

        /// <summary>Cantidad de nodos del árbol, incluida la raíz.</summary>
        public int NodeCount => throw new NotImplementedException("El Trie está pendiente de implementación.");

        /// <summary>Inserta un par (texto, valor); devuelve falso si el par ya existe.</summary>
        public bool Insert(string text, string payload)
        {
            // TODO [TRIE]
            throw new NotImplementedException("El Trie está pendiente de implementación.");
        }

        /// <summary>Indica si el texto está almacenado con cualquier valor.</summary>
        public bool Contains(string text)
        {
            // TODO [TRIE]
            throw new NotImplementedException("El Trie está pendiente de implementación.");
        }

        /// <summary>Elimina un par (texto, valor) y poda las ramas vacías; devuelve falso si no existía.</summary>
        public bool Remove(string text, string payload)
        {
            // TODO [TRIE]
            throw new NotImplementedException("El Trie está pendiente de implementación.");
        }

        /// <summary>Devuelve hasta maxResults coincidencias cuyo texto comienza con el prefijo.</summary>
        public TrieMatch[] SearchByPrefix(string prefix, int maxResults)
        {
            // TODO [TRIE]
            throw new NotImplementedException("El Trie está pendiente de implementación.");
        }

        /// <summary>Recorre todo el árbol y devuelve todas las entradas.</summary>
        public TrieMatch[] GetAll()
        {
            // TODO [TRIE]
            throw new NotImplementedException("El Trie está pendiente de implementación.");
        }

        /// <summary>Genera una descripción imprimible del contenido y tamaño del árbol.</summary>
        public string Describe()
        {
            // TODO [TRIE]
            throw new NotImplementedException("El Trie está pendiente de implementación.");
        }
    }
}
