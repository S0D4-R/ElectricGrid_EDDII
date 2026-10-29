namespace ElectricGrid.Services
{
    // Consultas de solo lectura para mostrar las estructuras internas.
    public interface IStructureInspector
    {
        // Describe las cubetas y colisiones de la tabla hash.
        string DescribeHashTable();

        // Describe las listas de adyacencia del grafo.
        string DescribeGraph();

        // Describe el contenido y tamaño del Trie.
        string DescribeTrie();

        // Describe el padre y el rango de cada elemento de Union-Find.
        string DescribeDisjointSets();
    }
}
