namespace ElectricGrid.Services
{
    /// <summary>Vista de solo lectura de las estructuras de datos internas, para demostraciones y depuración.</summary>
    public interface IStructureInspector
    {
        /// <summary>Describe las cubetas y cadenas de colisión de la tabla hash.</summary>
        string DescribeHashTable();

        /// <summary>Describe las listas de adyacencia del grafo.</summary>
        string DescribeGraph();

        /// <summary>Describe el contenido y tamaño del Trie.</summary>
        string DescribeTrie();

        /// <summary>Describe el bosque de padres y rangos de Union-Find.</summary>
        string DescribeDisjointSets();
    }
}
