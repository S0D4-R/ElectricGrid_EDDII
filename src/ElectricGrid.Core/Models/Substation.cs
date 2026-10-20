namespace ElectricGrid.Core.Models
{
    /// <summary>
    /// Substation of the municipal electrical grid. Instances are immutable records
    /// stored in the hash table and referenced by the graph through <see cref="VertexIndex"/>.
    /// </summary>
    public sealed class Substation
    {
        /// <summary>Creates a substation record.</summary>
        internal Substation(string code, string name, double capacityMw, int vertexIndex)
        {
            Code = code;
            Name = name;
            CapacityMw = capacityMw;
            VertexIndex = vertexIndex;
        }

        /// <summary>Unique code (normalized to upper case). Key of the hash table.</summary>
        public string Code { get; }

        /// <summary>Human readable name. Indexed by the trie for autocomplete.</summary>
        public string Name { get; }

        /// <summary>Installed capacity in megawatts (MW).</summary>
        public double CapacityMw { get; }

        /// <summary>Index of the vertex that represents this substation in the graph and Union-Find.</summary>
        internal int VertexIndex { get; }
    }
}
