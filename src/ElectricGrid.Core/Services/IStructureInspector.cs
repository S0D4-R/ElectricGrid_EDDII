namespace ElectricGrid.Core.Services
{
    /// <summary>
    /// Read-only diagnostic view of the internal data structures. Kept separate from
    /// <see cref="IPowerGridService"/> because it targets demonstrations, debugging and the
    /// technical defense rather than regular grid operation.
    /// </summary>
    public interface IStructureInspector
    {
        /// <summary>Describes buckets and collision chains of the hash table.</summary>
        string DescribeHashTable();

        /// <summary>Describes the adjacency lists of the graph.</summary>
        string DescribeGraph();

        /// <summary>Describes the entries and size of the trie.</summary>
        string DescribeTrie();

        /// <summary>Describes the parent/rank forest of the Union-Find structure.</summary>
        string DescribeDisjointSets();
    }
}
