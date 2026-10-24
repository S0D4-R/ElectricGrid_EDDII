using System;
using ElectricGrid.Collections;

namespace ElectricGrid.DataStructures
{
    /// <summary>
    /// Bosque de conjuntos disjuntos (Union-Find) con compresión de camino y unión por rango.
    /// Representa las zonas eléctricas y valida conectividad no dirigida entre subestaciones.
    /// </summary>
    /// <remarks>Responsable: Integrante C (Union-Find).</remarks>
    public sealed class DisjointSet
    {
        private int[] _parent;
        private int[] _rank;
        private int _elementCount;
        private int _setCount;

        /// <summary>Crea una estructura vacía con la capacidad inicial indicada.</summary>
        public DisjointSet(int initialCapacity = 16)
        {
            int capacity = initialCapacity < 1 ? 1 : initialCapacity;
            _parent = new int[capacity];
            _rank = new int[capacity];
        }

        /// <summary>Cantidad de elementos creados.</summary>
        public int ElementCount => _elementCount;

        /// <summary>Cantidad de conjuntos disjuntos, incluyendo los unitarios.</summary>
        public int SetCount => _setCount;

        /// <summary>Crea un conjunto unitario nuevo y devuelve el índice de su elemento.</summary>
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

        /// <summary>Devuelve el representante del conjunto del elemento, aplicando compresión de camino.</summary>
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

        /// <summary>Une los conjuntos de ambos elementos por rango; devuelve falso si ya estaban unidos.</summary>
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

        /// <summary>Indica si dos elementos pertenecen al mismo conjunto.</summary>
        public bool AreConnected(int a, int b) => Find(a) == Find(b);

        /// <summary>Deja cada elemento existente en su propio conjunto, para reconstruir tras una eliminación.</summary>
        public void ResetToSingletons()
        {
            for (int i = 0; i < _elementCount; i++)
            {
                _parent[i] = i;
                _rank[i] = 0;
            }

            _setCount = _elementCount;
        }

        /// <summary>Devuelve el padre directo de un elemento sin comprimir; solo para diagnóstico.</summary>
        public int PeekParent(int element)
        {
            ValidateElement(element);
            return _parent[element];
        }

        /// <summary>Devuelve el rango almacenado de un elemento; solo para diagnóstico.</summary>
        public int PeekRank(int element)
        {
            ValidateElement(element);
            return _rank[element];
        }

        private void ValidateElement(int element)
        {
            if (element < 0 || element >= _elementCount)
            {
                throw new ArgumentOutOfRangeException(nameof(element));
            }
        }
    }
}
