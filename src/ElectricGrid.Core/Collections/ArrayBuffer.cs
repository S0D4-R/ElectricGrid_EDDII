using System;

namespace ElectricGrid.Core.Collections
{
    /// <summary>
    /// Minimal growable array implemented from scratch. It is used as an auxiliary
    /// container (e.g. to collect results) so that the project never depends on
    /// <c>List&lt;T&gt;</c>, as required by the project statement.
    /// </summary>
    /// <typeparam name="T">Type of the stored elements.</typeparam>
    public sealed class ArrayBuffer<T>
    {
        private T[] _items;
        private int _count;

        /// <summary>Creates an empty buffer.</summary>
        /// <param name="initialCapacity">Initial number of slots (minimum 1).</param>
        public ArrayBuffer(int initialCapacity = 8)
        {
            _items = new T[initialCapacity < 1 ? 1 : initialCapacity];
        }

        /// <summary>Number of stored elements.</summary>
        public int Count => _count;

        /// <summary>Gets or sets the element at the given zero-based index.</summary>
        public T this[int index]
        {
            get
            {
                EnsureValidIndex(index);
                return _items[index];
            }
            set
            {
                EnsureValidIndex(index);
                _items[index] = value;
            }
        }

        /// <summary>Appends an element at the end. Amortized O(1).</summary>
        public void Add(T item)
        {
            if (_count == _items.Length)
            {
                Grow();
            }

            _items[_count++] = item;
        }

        /// <summary>Returns an exact-size copy of the stored elements. O(n).</summary>
        public T[] ToArray()
        {
            T[] copy = new T[_count];
            Array.Copy(_items, copy, _count);
            return copy;
        }

        /// <summary>Removes every element.</summary>
        public void Clear()
        {
            Array.Clear(_items, 0, _count);
            _count = 0;
        }

        private void Grow()
        {
            T[] bigger = new T[_items.Length * 2];
            Array.Copy(_items, bigger, _count);
            _items = bigger;
        }

        private void EnsureValidIndex(int index)
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
