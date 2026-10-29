//Jorge Rivera -> 1511425

using System;
using ElectricGrid.Collections;

namespace ElectricGrid.DataStructures
{
    // Union-Find con compresión de camino y unión por rango.
    public sealed class DisjointSet
    {
        private int[] _parent;
        private int[] _rank;
        private int _elementCount;
        private int _setCount;

        // Reserva los arreglos de padres y rangos.
        public DisjointSet(int initialCapacity = 16)
        {
            int capacity = initialCapacity < 1 ? 1 : initialCapacity;
            _parent = new int[capacity];
            _rank = new int[capacity];
        }

        public int ElementCount => _elementCount;

        public int SetCount => _setCount;

        // Crea un conjunto unitario y devuelve su índice.
        public int MakeSet()
        {
            if (_elementCount == _parent.Length)
            {
                ArrayUtils.Resize(ref _parent, _parent.Length * 2);
                ArrayUtils.Resize(ref _rank, _rank.Length * 2);
            }

            int element = _elementCount++;
            _parent[element] = element;
            _rank[element] = 0;
            _setCount++;
            return element;
        }

        // Devuelve la raíz del conjunto y comprime el camino.
        public int Find(int element)
        {
            ValidateElement(element);

            int root = element;
            while (_parent[root] != root)
            {
                root = _parent[root];
            }

            while (_parent[element] != root)
            {
                int next = _parent[element];
                _parent[element] = root;
                element = next;
            }

            return root;
        }

        // Une dos conjuntos y devuelve false si ya estaban unidos.
        public bool Union(int a, int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);

            if (rootA == rootB)
            {
                return false;
            }

            if (_rank[rootA] < _rank[rootB])
            {
                _parent[rootA] = rootB;
            }
            else if (_rank[rootA] > _rank[rootB])
            {
                _parent[rootB] = rootA;
            }
            else
            {
                _parent[rootB] = rootA;
                _rank[rootA]++;
            }

            _setCount--;
            return true;
        }

        // Indica si dos elementos tienen la misma raíz.
        public bool AreConnected(int a, int b) => Find(a) == Find(b);

        // Deja cada elemento en su propio conjunto.
        public void ResetToSingletons()
        {
            for (int i = 0; i < _elementCount; i++)
            {
                _parent[i] = i;
                _rank[i] = 0;
            }

            _setCount = _elementCount;
        }

        // Devuelve el padre actual sin comprimir el camino.
        public int PeekParent(int element)
        {
            ValidateElement(element);
            return _parent[element];
        }

        // Devuelve el rango guardado para el elemento.
        public int PeekRank(int element)
        {
            ValidateElement(element);
            return _rank[element];
        }

        // Comprueba que el índice exista.
        private void ValidateElement(int element)
        {
            if (element < 0 || element >= _elementCount)
            {
                throw new ArgumentOutOfRangeException(nameof(element));
            }
        }
    }
}
