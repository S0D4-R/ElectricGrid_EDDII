namespace ElectricGrid.Collections
{
    // Utilidades para redimensionar y limpiar arreglos sin usar Array.
    public static class ArrayUtils
    {
        // Crea un arreglo nuevo y copia los elementos que caben.
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

        // Devuelve cada posición a su valor por defecto.
        public static void Clear<T>(T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = default!;
            }
        }
    }
}
