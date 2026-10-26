// Jorge Rivera -> 1511425

using System;
using System.Text;
using ElectricGrid.Collections;

namespace ElectricGrid.DataStructures
{
    /// <summary>
    /// Tabla hash con llaves de texto y resolución de colisiones por encadenamiento.
    /// Se usa como registro principal de subestaciones, localizadas por su código.
    /// </summary>
    /// <remarks>Responsable: Integrante A (Tabla Hash).</remarks>
    public sealed class HashTable<TValue>
    {
        private const int DefaultCapacity = 17;
        private const double MaxLoadFactor = 0.75;

        private sealed class Entry
        {
            public readonly string Key;
            public TValue Value;
            public Entry? Next;

            public Entry(string key, TValue value, Entry? next)
            {
                Key = key;
                Value = value;
                Next = next;
            }
        }

        private Entry?[] _buckets;
        private int _count;

        /// <summary>Crea una tabla vacía; la cantidad de cubetas se ajusta al siguiente número primo.</summary>
        public HashTable(int initialCapacity = DefaultCapacity)
        {
            _buckets = new Entry?[NextPrime(initialCapacity < 3 ? 3 : initialCapacity)];
        }

        /// <summary>Cantidad de pares almacenados.</summary>
        public int Count => _count;

        /// <summary>Cantidad actual de cubetas.</summary>
        public int Capacity => _buckets.Length;

        /// <summary>Relación entre pares almacenados y cubetas.</summary>
        public double LoadFactor => (double)_count / _buckets.Length;

        /// <summary>Inserta un par nuevo; devuelve falso si la llave ya existe.</summary>
        public bool Insert(string key, TValue value)
        {
            ValidateKey(key);

            int index = IndexFor(key, _buckets.Length);
            for (Entry? current = _buckets[index]; current != null; current = current.Next)
            {
                if (string.Equals(current.Key, key, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            if ((double)(_count + 1) / _buckets.Length > MaxLoadFactor)
            {
                Rehash();
                index = IndexFor(key, _buckets.Length);
            }

            _buckets[index] = new Entry(key, value, _buckets[index]);
            _count++;
            return true;
        }

        /// <summary>Reemplaza el valor de una llave existente; devuelve falso si no existe.</summary>
        public bool Update(string key, TValue value)
        {
            Entry? entry = FindEntry(key);
            if (entry == null)
            {
                return false;
            }

            entry.Value = value;
            return true;
        }

        /// <summary>Busca una llave y devuelve su valor; falso si no existe.</summary>
        public bool TryGetValue(string key, out TValue value)
        {
            Entry? entry = FindEntry(key);
            if (entry == null)
            {
                value = default!;
                return false;
            }

            value = entry.Value;
            return true;
        }

        /// <summary>Indica si la llave existe en la tabla.</summary>
        public bool ContainsKey(string key) => FindEntry(key) != null;

        /// <summary>Elimina una llave; devuelve falso si no existía.</summary>
        public bool Remove(string key)
        {
            ValidateKey(key);

            int index = IndexFor(key, _buckets.Length);
            Entry? previous = null;

            for (Entry? current = _buckets[index]; current != null; current = current.Next)
            {
                if (string.Equals(current.Key, key, StringComparison.Ordinal))
                {
                    if (previous == null)
                    {
                        _buckets[index] = current.Next;
                    }
                    else
                    {
                        previous.Next = current.Next;
                    }

                    _count--;
                    return true;
                }

                previous = current;
            }

            return false;
        }

        /// <summary>Elimina todos los pares de la tabla.</summary>
        public void Clear()
        {
            ArrayUtils.Clear(_buckets);
            _count = 0;
        }

        /// <summary>Recorre todos los pares en el orden de las cubetas.</summary>
        public void ForEach(Action<string, TValue> visitor)
        {
            for (int i = 0; i < _buckets.Length; i++)
            {
                for (Entry? current = _buckets[i]; current != null; current = current.Next)
                {
                    visitor(current.Key, current.Value);
                }
            }
        }

        /// <summary>Devuelve todas las llaves almacenadas.</summary>
        public string[] GetKeys()
        {
            string[] keys = new string[_count];
            int position = 0;
            ForEach((key, _) => keys[position++] = key);
            return keys;
        }

        /// <summary>Genera una descripción imprimible de las cubetas y sus cadenas de colisión.</summary>
        public string Describe(int maxBucketsShown = 40)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Tabla hash | entradas: {_count} | cubetas: {_buckets.Length} | factor de carga: {LoadFactor:F2}");

            int longestChain = 0;
            int shown = 0;

            for (int i = 0; i < _buckets.Length; i++)
            {
                int length = 0;
                for (Entry? current = _buckets[i]; current != null; current = current.Next)
                {
                    length++;
                }

                if (length == 0)
                {
                    continue;
                }

                longestChain = Math.Max(longestChain, length);

                if (shown < maxBucketsShown)
                {
                    builder.Append($"  cubeta[{i,3}] ");
                    for (Entry? current = _buckets[i]; current != null; current = current.Next)
                    {
                        builder.Append(current.Key);
                        if (current.Next != null)
                        {
                            builder.Append(" -> ");
                        }
                    }

                    builder.AppendLine();
                    shown++;
                }
            }

            builder.AppendLine($"Cadena de colisión más larga: {longestChain}");
            return builder.ToString();
        }

        /// <summary>Calcula el hash djb2 de un texto, independiente de GetHashCode.</summary>
        public static uint ComputeHash(string key)
        {
            uint hash = 5381;
            for (int i = 0; i < key.Length; i++)
            {
                hash = unchecked(hash * 33 + key[i]);
            }

            return hash;
        }

        private static int IndexFor(string key, int bucketCount)
        {
            return (int)(ComputeHash(key) % (uint)bucketCount);
        }

        private Entry? FindEntry(string key)
        {
            ValidateKey(key);

            int index = IndexFor(key, _buckets.Length);
            for (Entry? current = _buckets[index]; current != null; current = current.Next)
            {
                if (string.Equals(current.Key, key, StringComparison.Ordinal))
                {
                    return current;
                }
            }

            return null;
        }

        private void Rehash()
        {
            Entry?[] oldBuckets = _buckets;
            Entry?[] newBuckets = new Entry?[NextPrime(oldBuckets.Length * 2)];

            for (int i = 0; i < oldBuckets.Length; i++)
            {
                Entry? current = oldBuckets[i];
                while (current != null)
                {
                    Entry? next = current.Next;
                    int index = IndexFor(current.Key, newBuckets.Length);
                    current.Next = newBuckets[index];
                    newBuckets[index] = current;
                    current = next;
                }
            }

            _buckets = newBuckets;
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("La llave no puede ser nula ni vacía.", nameof(key));
            }
        }

        private static int NextPrime(int minimum)
        {
            int candidate = minimum % 2 == 0 ? minimum + 1 : minimum;
            while (!IsPrime(candidate))
            {
                candidate += 2;
            }

            return candidate;
        }

        private static bool IsPrime(int number)
        {
            if (number < 2)
            {
                return false;
            }

            for (int divisor = 3; (long)divisor * divisor <= number; divisor += 2)
            {
                if (number % divisor == 0)
                {
                    return false;
                }
            }

            return number % 2 != 0 || number == 2;
        }
    }
}
