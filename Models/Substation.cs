namespace ElectricGrid.Models
{
    /// <summary>Subestación de la red eléctrica; se almacena en la tabla hash y se vincula al grafo por su índice de vértice.</summary>
    public sealed class Substation
    {
        internal Substation(string code, string name, double capacityMw, int vertexIndex)
        {
            Code = code;
            Name = name;
            CapacityMw = capacityMw;
            VertexIndex = vertexIndex;
        }

        /// <summary>Código único en mayúsculas; es la llave de la tabla hash.</summary>
        public string Code { get; }

        /// <summary>Nombre de la subestación.</summary>
        public string Name { get; }

        /// <summary>Capacidad instalada en megavatios.</summary>
        public double CapacityMw { get; }

        internal int VertexIndex { get; }
    }
}
