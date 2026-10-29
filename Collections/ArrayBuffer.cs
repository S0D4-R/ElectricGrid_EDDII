using System;

namespace ElectricGrid.Collections
{
    // Arreglo dinámico sencillo que se usa en lugar de List<T>.
    public sealed class ArrayBuffer<T>
    {
        private T[] _items;
        private int _count;

        // Crea el arreglo interno con la capacidad indicada.
        public ArrayBuffer(int initialCapacity = 8)
        {
            _items = new T[initialCapacity < 1 ? 1 : initialCapacity];
        }

        public int Count => _count;

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

        // Agrega al final y duplica la capacidad si ya no hay espacio.
        public void Add(T item)
        {
            if (_count == _items.Length)
            {
                ArrayUtils.Resize(ref _items, _items.Length * 2);
            }

            _items[_count++] = item;
        }

        // Copia los elementos a un arreglo del tamaño exacto.
        public T[] ToArray()
        {
            T[] copy = new T[_count];
            for (int i = 0; i < _count; i++)
            {
                copy[i] = _items[i];
            }

            return copy;
        }

        // Descarta los elementos y conserva la capacidad actual.
        public void Clear()
        {
            ArrayUtils.Clear(_items);
            _count = 0;
        }

        // Verifica que el índice apunte a un elemento existente.
        private void EnsureValidIndex(int index)
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
