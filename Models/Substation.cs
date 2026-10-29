namespace ElectricGrid.Models
{
    public sealed class Substation
    {
        // Guarda los datos de la subestación y su índice en el grafo.
        internal Substation(string code, string name, double capacityMw, int vertexIndex)
        {
            Code = code;
            Name = name;
            CapacityMw = capacityMw;
            VertexIndex = vertexIndex;
        }

        public string Code { get; }

        public string Name { get; }

        public double CapacityMw { get; }

        internal int VertexIndex { get; }
    }
}
