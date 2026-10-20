using System;

namespace ElectricGrid.Core.DataStructures
{
    /// <summary>
    /// Disjoint-set (Union-Find) forest with path compression and union by rank.
    /// <para>
    /// ROLE IN THE SYSTEM: tracks the electrical zones (connected components) of the network.
    /// It answers "do these two substations belong to the same zone?" in near-constant time,
    /// which lets the application (a) reject Dijkstra requests that can never succeed and
    /// (b) analyse a proposed new line before approving it.
    /// </para>
    /// <para>
    /// Note: Union-Find models UNDIRECTED (weak) connectivity. A one-way line still joins two
    /// substations in the same zone, even though power only flows in one direction; the graph
    /// (Dijkstra) is the structure that decides whether a directed path actually exists.
    /// </para>
    /// <para>
    /// Complexity: Find / Union run in O(alpha(n)) amortized, where alpha is the inverse
    /// Ackermann function (practically constant).
    /// </para>
    /// </summary>
    /// <remarks>
    /// Developed by: Team Member C (Union-Find owner) - replace with the real name.
    /// </remarks>
    public sealed class DisjointSet
    {
        private int[] _parent;
        private int[] _rank;
        private int _elementCount;
        private int _setCount;

        /// <summary>Creates an empty structure.</summary>
        public DisjointSet(int initialCapacity = 16)
        {
            int capacity = initialCapacity < 1 ? 1 : initialCapacity;
            _parent = new int[capacity];
            _rank = new int[capacity];
        }

        /// <summary>Number of elements created so far.</summary>
        public int ElementCount => _elementCount;

        /// <summary>Number of disjoint sets (including singletons).</summary>
        public int SetCount => _setCount;

        /// <summary>Creates a new singleton set and returns its element index. Amortized O(1).</summary>
        public int MakeSet()
        {
            if (_elementCount == _parent.Length)
            {
                Array.Resize(ref _parent, _parent.Length * 2);
                Array.Resize(ref _rank, _rank.Length * 2);
            }

            int element = _elementCount++;
            _parent[element] = element;
            _rank[element] = 0;
            _setCount++;
            return element;
        }

        /// <summary>
        /// Returns the representative (root) of the set containing <paramref name="element"/>.
        /// Applies iterative path compression: every node visited is re-attached directly to
        /// the root, flattening the tree for future queries.
        /// </summary>
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

        /// <summary>
        /// Merges the sets containing both elements using union by rank.
        /// </summary>
        /// <returns><c>true</c> if two different sets were merged; <c>false</c> if they were already joined.</returns>
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

        /// <summary>Checks whether two elements are in the same set. Amortized O(alpha(n)).</summary>
        public bool AreConnected(int a, int b) => Find(a) == Find(b);

        /// <summary>
        /// Resets every existing element to its own singleton set. Union-Find cannot split sets,
        /// so this (followed by re-applying the unions) is how the structure is rebuilt after
        /// a line or substation is deleted. O(n).
        /// </summary>
        public void ResetToSingletons()
        {
            for (int i = 0; i < _elementCount; i++)
            {
                _parent[i] = i;
                _rank[i] = 0;
            }

            _setCount = _elementCount;
        }

        /// <summary>Returns the raw parent pointer of an element (no compression). For diagnostics only.</summary>
        public int PeekParent(int element)
        {
            ValidateElement(element);
            return _parent[element];
        }

        /// <summary>Returns the rank stored for an element. For diagnostics only.</summary>
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
