using System;

namespace ElectricGrid.Collections
{
    /// <summary>Arreglo dinámico propio que reemplaza a List&lt;T&gt; como contenedor auxiliar de resultados.</summary>
    public sealed class ArrayBuffer<T>
    {
        private T[] _items;
        private int _count;

        /// <summary>Crea un contenedor vacío con la capacidad inicial indicada.</summary>
        public ArrayBuffer(int initialCapacity = 8)
        {
            _items = new T[initialCapacity < 1 ? 1 : initialCapacity];
        }

        /// <summary>Cantidad de elementos almacenados.</summary>
        public int Count => _count;

        /// <summary>Obtiene o reemplaza el elemento de la posición indicada.</summary>
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

        /// <summary>Agrega un elemento al final, ampliando la capacidad cuando es necesario.</summary>
        public void Add(T item)
        {
            if (_count == _items.Length)
            {
                ArrayUtils.Resize(ref _items, _items.Length * 2);
            }

            _items[_count++] = item;
        }

        /// <summary>Devuelve una copia del contenido con el tamaño exacto.</summary>
        public T[] ToArray()
        {
            T[] copy = new T[_count];
            for (int i = 0; i < _count; i++)
            {
                copy[i] = _items[i];
            }

            return copy;
        }

        /// <summary>Elimina todos los elementos.</summary>
        public void Clear()
        {
            ArrayUtils.Clear(_items);
            _count = 0;
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
