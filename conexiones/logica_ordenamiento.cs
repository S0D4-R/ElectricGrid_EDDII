using System.Collections.Generic;
using System.IO;

// Subestacion: codigo, nombre y capacidad
// linea bidireccional: conecta dos subestacion, cuenta con longitud y costo de mantenimiento
// linea unidireccional: solo tiene un sentido de la subestacion principal a las secundarias, tambien tiene longitus y costo
// usar algoritmo dijkstra para calcular la ruta de menor costo para restablecer el suministro entre dos puntos 


//---- decalaracion de los esqueletos ----
class SubEstacion(string codigo, string nombre, int capacidad)
{
    public string codigo = codigo;
    public string nombre = nombre;
    public int capacidad = capacidad;
}

class Linea(int longitud, int costo) //datos de la arista
{
    public int longitud = longitud;
    public int costo = costo;
}

class Arista(Linea linea, string origen, string destino, bool bidireccional)
{
    public Linea linea = linea;
    public string origen = origen;
    public string destino = destino;
    public bool bidireccional = bidireccional;

    public Arista(Linea linea, string origen, string destino)
        : this(linea, origen, destino, true)
    {
    }
}

class ResultadoRuta(string[] estaciones, long costoTotal)
{
    public string[] estaciones = estaciones;
    public long costoTotal = costoTotal;
}

//---- algoritmo dijkstra ----
class GrafoEstaciones
{
    private readonly SubEstacion[] estaciones;
    private readonly Arista[] aristas;
    private int cantidadEstaciones;
    private int cantidadAristas;

    public GrafoEstaciones(int capacidadEstaciones, int capacidadAristas)
    {
        if (capacidadEstaciones < 0)
            throw new ArgumentOutOfRangeException(nameof(capacidadEstaciones), "Las capacidades no pueden ser negativas.");
        if (capacidadAristas < 0)
            throw new ArgumentOutOfRangeException(nameof(capacidadAristas), "Las capacidades no pueden ser negativas.");

        estaciones = new SubEstacion[capacidadEstaciones];
        aristas = new Arista[capacidadAristas];
    }

    public void AgregarEstacion(SubEstacion estacion)
    {
        ArgumentNullException.ThrowIfNull(estacion);
        if (cantidadEstaciones == estaciones.Length)
            throw new InvalidOperationException("Se alcanzo la capacidad de estaciones.");
        if (BuscarEstacion(estacion.codigo) >= 0)
            throw new ArgumentException("Ya existe una estacion con ese codigo.");

        estaciones[cantidadEstaciones++] = estacion;
    }

    // CSV con columnas: Tipo,Codigo,Nombre,Capacidad,Origen,Destino,Longitud,Costo,Bidireccional.
    // Tipo puede ser ESTACION o LINEA. Las lineas requieren true/false en Bidireccional.
    // Para rutas relativas se busca primero desde el directorio actual y luego junto al ejecutable.
    public static GrafoEstaciones CargarDesdeCsv(string rutaCsv)
    {
        if (string.IsNullOrWhiteSpace(rutaCsv))
            throw new ArgumentException("Debe indicar la ruta del archivo CSV.", "rutaCsv");

        string rutaCompleta = ResolverRutaCsv(rutaCsv);

        string[] filas = File.ReadAllLines(rutaCompleta);
        List<SubEstacion> estacionesCsv = [];
        List<Arista> lineasCsv = [];

        for (int i = 0; i < filas.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(filas[i]))
                continue;

            string[] campos = SepararCamposCsv(filas[i]);
            if (i == 0 && campos.Length > 0 &&
                campos[0].TrimStart('\uFEFF').Equals("Tipo", StringComparison.OrdinalIgnoreCase))
                continue;

            if (campos.Length == 0)
                continue;

            string tipo = campos[0].Trim();
            if (tipo.Equals("ESTACION", StringComparison.OrdinalIgnoreCase))
            {
                if (campos.Length < 4)
                    throw new FormatException("Fila " + (i + 1) + ": faltan datos de la estacion.");

                estacionesCsv.Add(new(
                    campos[1].Trim(), campos[2].Trim(), int.Parse(campos[3].Trim())));
            }
            else if (tipo.Equals("LINEA", StringComparison.OrdinalIgnoreCase))
            {
                if (campos.Length < 9)
                    throw new FormatException("Fila " + (i + 1) + ": faltan datos de la linea.");

                if (!bool.TryParse(campos[8].Trim(), out bool bidireccional))
                    throw new FormatException("Fila " + (i + 1) + ": Bidireccional debe ser true o false.");

                lineasCsv.Add(new(
                    new(int.Parse(campos[6].Trim()), int.Parse(campos[7].Trim())),
                    campos[4].Trim(), campos[5].Trim(), bidireccional));
            }
            else
            {
                throw new FormatException("Fila " + (i + 1) + ": Tipo debe ser ESTACION o LINEA.");
            }
        }

        GrafoEstaciones grafo = new GrafoEstaciones(estacionesCsv.Count, lineasCsv.Count);
        foreach (SubEstacion estacion in estacionesCsv)
            grafo.AgregarEstacion(estacion);
        foreach (Arista arista in lineasCsv)
            grafo.AgregarLinea(arista.linea, arista.origen, arista.destino, arista.bidireccional);

        return grafo;
    }

    private static string ResolverRutaCsv(string rutaCsv)
    {
        if (Path.IsPathRooted(rutaCsv))
            return rutaCsv;

        string[] directoriosIniciales =
        [
            Environment.CurrentDirectory,
            AppDomain.CurrentDomain.BaseDirectory
        ];

        foreach (string directorioInicial in directoriosIniciales)
        {
            DirectoryInfo directorio = new DirectoryInfo(directorioInicial);
            while (directorio != null)
            {
                string candidato = Path.Combine(directorio.FullName, rutaCsv);
                if (File.Exists(candidato))
                    return candidato;

                directorio = directorio.Parent;
            }
        }

        return Path.GetFullPath(rutaCsv);
    }

    private static string[] SepararCamposCsv(string fila)
    {
        List<string> campos = new List<string>();
        System.Text.StringBuilder campo = new System.Text.StringBuilder();
        bool entreComillas = false;

        for (int i = 0; i < fila.Length; i++)
        {
            char caracter = fila[i];
            if (caracter == '"')
            {
                if (entreComillas && i + 1 < fila.Length && fila[i + 1] == '"')
                {
                    campo.Append('"');
                    i++;
                }
                else
                {
                    entreComillas = !entreComillas;
                }
            }
            else if (caracter == ',' && !entreComillas)
            {
                campos.Add(campo.ToString());
                campo.Length = 0;
            }
            else
            {
                campo.Append(caracter);
            }
        }

        if (entreComillas)
            throw new FormatException("El CSV contiene comillas sin cerrar.");

        campos.Add(campo.ToString());
        return [.. campos];
    }

    public void AgregarLinea(Linea linea, string origen, string destino, bool bidireccional)
    {
        ArgumentNullException.ThrowIfNull(linea);
        if (linea.costo < 0)
            throw new ArgumentException("Dijkstra requiere costos no negativos.");
        if (BuscarEstacion(origen) < 0 || BuscarEstacion(destino) < 0)
            throw new ArgumentException("El origen y el destino deben existir en el grafo.");
        if (cantidadAristas == aristas.Length)
            throw new InvalidOperationException("Se alcanzo la capacidad de lineas.");

        aristas[cantidadAristas++] = new Arista(linea, origen, destino, bidireccional);
    }

    // Devuelve el recorrido por codigo de estacion y su costo de mantenimiento total.
    // Si no existe ruta, estaciones sera un arreglo vacio y costoTotal sera long.MaxValue.
    public ResultadoRuta Dijkstra(string codigoOrigen, string codigoDestino)
    {
        int indiceOrigen = BuscarEstacion(codigoOrigen);
        int indiceDestino = BuscarEstacion(codigoDestino);
        if (indiceOrigen < 0 || indiceDestino < 0)
            throw new ArgumentException("El origen y el destino deben existir en el grafo.");

        long[] costos = new long[cantidadEstaciones];
        int[] anterior = new int[cantidadEstaciones];
        bool[] visitada = new bool[cantidadEstaciones];

        for (int i = 0; i < cantidadEstaciones; i++)
        {
            costos[i] = long.MaxValue;
            anterior[i] = -1;
        }
        costos[indiceOrigen] = 0;

        // Selecciona el nodo no visitado de menor costo mediante busqueda lineal.
        for (int paso = 0; paso < cantidadEstaciones; paso++)
        {
            int actual = -1;
            for (int i = 0; i < cantidadEstaciones; i++)
            {
                if (!visitada[i] && costos[i] != long.MaxValue &&
                    (actual < 0 || costos[i] < costos[actual]))
                    actual = i;
            }

            if (actual < 0 || actual == indiceDestino)
                break;
            visitada[actual] = true;

            for (int i = 0; i < cantidadAristas; i++)
            {
                Arista arista = aristas[i];
                int vecino = -1;

                if (arista.origen == estaciones[actual].codigo)
                    vecino = BuscarEstacion(arista.destino);
                else if (arista.bidireccional && arista.destino == estaciones[actual].codigo)
                    vecino = BuscarEstacion(arista.origen);

                if (vecino >= 0 && !visitada[vecino] &&
                    costos[actual] <= long.MaxValue - arista.linea.costo)
                {
                    long nuevoCosto = costos[actual] + arista.linea.costo;
                    if (nuevoCosto < costos[vecino])
                    {
                        costos[vecino] = nuevoCosto;
                        anterior[vecino] = actual;
                    }
                }
            }
        }

        if (costos[indiceDestino] == long.MaxValue)
            return new ResultadoRuta(Array.Empty<string>(), long.MaxValue);

        int cantidadRuta = 0;
        for (int actual = indiceDestino; actual >= 0; actual = anterior[actual])
            cantidadRuta++;

        string[] ruta = new string[cantidadRuta];
        int posicion = cantidadRuta - 1;
        for (int actual = indiceDestino; actual >= 0; actual = anterior[actual])
            ruta[posicion--] = estaciones[actual].codigo;

        return new ResultadoRuta(ruta, costos[indiceDestino]);
    }

    private int BuscarEstacion(string codigo)
    {
        for (int i = 0; i < cantidadEstaciones; i++)
        {
            if (estaciones[i].codigo == codigo)
                return i;
        }
        return -1;
    }

    public void MostrarDatos()
    {
        Console.WriteLine("\nEstaciones:");
        if (cantidadEstaciones == 0)
            Console.WriteLine("  No hay estaciones cargadas.");

        for (int i = 0; i < cantidadEstaciones; i++)
            Console.WriteLine("  {0} - {1} (capacidad: {2})", estaciones[i].codigo, estaciones[i].nombre, estaciones[i].capacidad);

        Console.WriteLine("Lineas:");
        if (cantidadAristas == 0)
            Console.WriteLine("  No hay lineas cargadas.");

        for (int i = 0; i < cantidadAristas; i++)
        {
            Arista arista = aristas[i];
            Console.WriteLine("  {0} {1} {2} (longitud: {3}, costo: {4})",
                arista.origen, arista.bidireccional ? "<->" : "->", arista.destino,
                arista.linea.longitud, arista.linea.costo);
        }
    }
}

class Program
{
    static void Main()
    {
        GrafoEstaciones grafo = new GrafoEstaciones(0, 0);
        bool continuar = true;

        while (continuar)
        {
            Console.WriteLine("\n=== Menu de conexiones electricas ===");
            Console.WriteLine("1. Cargar datos desde CSV (datos.csv)");
            Console.WriteLine("2. Mostrar estaciones y lineas");
            Console.WriteLine("3. Buscar ruta de menor costo (Dijkstra)");
            Console.WriteLine("4. Salir");
            Console.Write("Seleccione una opcion: ");

            string opcion = Console.ReadLine();
            switch (opcion)
            {
                case "1":
                    Console.Write("Ruta del archivo [datos.csv]: ");
                    string rutaCsv = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(rutaCsv))
                        rutaCsv = "datos.csv";

                    try
                    {
                        grafo = GrafoEstaciones.CargarDesdeCsv(rutaCsv);
                        Console.WriteLine("CSV cargado correctamente.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("No se pudo cargar el CSV: " + ex.Message);
                    }
                    break;
                case "2":
                    grafo.MostrarDatos();
                    break;
                case "3":
                    Console.Write("Codigo de la estacion de origen: ");
                    string origen = Console.ReadLine();
                    Console.Write("Codigo de la estacion de destino: ");
                    string destino = Console.ReadLine();
                    try
                    {
                        ResultadoRuta resultado = grafo.Dijkstra(origen, destino);
                        if (resultado.estaciones.Length == 0)
                            Console.WriteLine("No existe una ruta entre esas estaciones.");
                        else
                        {
                            Console.WriteLine("Ruta: " + string.Join(" -> ", resultado.estaciones));
                            Console.WriteLine("Costo total: " + resultado.costoTotal);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine("No se pudo calcular la ruta: " + ex.Message);
                    }
                    break;
                case "4":
                    continuar = false;
                    break;
                default:
                    Console.WriteLine("Opcion no valida.");
                    break;
            }
        }
    }

}







