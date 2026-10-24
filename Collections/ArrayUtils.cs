namespace ElectricGrid.Collections
{
    /// <summary>Operaciones auxiliares sobre arreglos implementadas con ciclos propios, sin usar la clase Array de .NET.</summary>
    public static class ArrayUtils
    {
        /// <summary>Reemplaza el arreglo por uno nuevo del tamaño indicado, conservando los elementos que caben.</summary>
        public static void Resize<T>(ref T[] array, int newSize)
        {
            T[] resized = new T[newSize];
            int limit = array.Length < newSize ? array.Length : newSize;

            for (int i = 0; i < limit; i++)
            {
                resized[i] = array[i];
            }

            array = resized;
        }

        /// <summary>Restablece todas las posiciones del arreglo a su valor por defecto.</summary>
        public static void Clear<T>(T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = default!;
            }
        }
    }
}
