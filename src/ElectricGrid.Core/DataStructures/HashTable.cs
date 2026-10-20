using System;
using System.Text;

namespace ElectricGrid.Core.DataStructures
{
    /// <summary>
    /// Hash table with string keys implemented from scratch using separate chaining.
    /// <para>
    /// ROLE IN THE SYSTEM: primary registry of substations. Given a substation code it
    /// returns the complete record in O(1) average time, which is the most frequent
    /// operation of the application (every route, traversal or connectivity query
    /// starts by resolving codes into records).
    /// </para>
    /// <para>
    /// Design decisions:
    /// <list type="bullet">
    /// <item><description>Separate chaining: simple deletion and graceful degradation under collisions.</description></item>
    /// <item><description>Prime-sized bucket array: spreads keys better with the modulo operator.</description></item>
    /// <item><description>Automatic rehashing when the load factor exceeds 0.75, keeping chains short.</description></item>
    /// <item><description>Own djb2 hash function (no <c>GetHashCode</c>/<c>Dictionary</c> involved).</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <typeparam name="TValue">Type of the stored values.</typeparam>
    /// <remarks>
    /// Developed by: Team Member A (Hash Table owner) - replace with the real name.
    /// </remarks>
    public sealed class HashTable<TValue>
    {
        private const int DefaultCapacity = 17;
        private const double MaxLoadFactor = 0.75;

        /// <summary>Node of a bucket chain.</summary>
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

        /// <summary>Creates an empty table.</summary>
        /// <param name="initialCapacity">Desired number of buckets (rounded up to a prime).</param>
        public HashTable(int initialCapacity = DefaultCapacity)
        {
            _buckets = new Entry?[NextPrime(initialCapacity < 3 ? 3 : initialCapacity)];
        }

        /// <summary>Number of stored key/value pairs.</summary>
        public int Count => _count;

        /// <summary>Current number of buckets.</summary>
        public int Capacity => _buckets.Length;

        /// <summary>Ratio between stored pairs and buckets.</summary>
        public double LoadFactor => (double)_count / _buckets.Length;

        /// <summary>
        /// Inserts a new pair. Average O(1), worst case O(n).
        /// </summary>
        /// <returns><c>false</c> if the key already exists (the table is left unchanged).</returns>
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

        /// <summary>Replaces the value of an existing key. Average O(1).</summary>
        /// <returns><c>false</c> if the key does not exist.</returns>
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

        /// <summary>Looks a key up. Average O(1), worst case O(n).</summary>
        /// <returns><c>true</c> when found; <paramref name="value"/> then holds the stored value.</returns>
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

        /// <summary>Checks whether a key exists. Average O(1).</summary>
        public bool ContainsKey(string key) => FindEntry(key) != null;

        /// <summary>Removes a key. Average O(1), worst case O(n).</summary>
        /// <returns><c>true</c> if the key existed.</returns>
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

        /// <summary>Removes every pair.</summary>
        public void Clear()
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            _count = 0;
        }

        /// <summary>Visits every pair (bucket order). O(capacity + n).</summary>
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

        /// <summary>Returns every key as an array. O(capacity + n).</summary>
        public string[] GetKeys()
        {
            string[] keys = new string[_count];
            int position = 0;
            ForEach((key, _) => keys[position++] = key);
            return keys;
        }

        /// <summary>
        /// Builds a printable description of the internal layout (buckets and collision chains),
        /// useful for demonstrations and for explaining the structure during the defense.
        /// </summary>
        /// <param name="maxBucketsShown">Maximum number of non-empty buckets to print.</param>
        public string Describe(int maxBucketsShown = 40)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Hash table | entries: {_count} | buckets: {_buckets.Length} | load factor: {LoadFactor:F2}");

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
                    builder.Append($"  bucket[{i,3}] ");
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

            builder.AppendLine($"Longest collision chain: {longestChain}");
            return builder.ToString();
        }

        /// <summary>
        /// djb2 string hash (Daniel J. Bernstein). Deterministic and independent from the
        /// runtime's <c>GetHashCode</c>. O(k) where k is the key length.
        /// </summary>
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

        /// <summary>Doubles (to the next prime) the bucket array and relinks every node. O(n).</summary>
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
                throw new ArgumentException("The key cannot be null or empty.", nameof(key));
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
