using System;
using System.IO;
using System.Collections.Generic;

// ESTRUCTURA TRIE (Para buscar nombres)


class ListaCadenas
{
    public string[] elementos;
    public int cantidad;

    public ListaCadenas()
    {
        elementos = new string[4];
        cantidad = 0;
    }

    public void Agregar(string valor)
    {
        if (cantidad == elementos.Length)
        {
            string[] nuevoArreglo = new string[elementos.Length * 2];
            for (int i = 0; i < elementos.Length; i++) nuevoArreglo[i] = elementos[i];
            elementos = nuevoArreglo;
        }
        elementos[cantidad++] = valor;
    }
}

class NodoTrie
{
    public NodoTrie[] Hijos;
    public bool FinDePalabra;
    public string ValorOriginal; // Guarda el nombre original con mayúsculas

    public NodoTrie()
    {
        Hijos = new NodoTrie[256]; 
        FinDePalabra = false;
        ValorOriginal = null;
    }
}

class Trie
{
    private NodoTrie raiz;

    public Trie()
    {
        raiz = new NodoTrie();
    }

    private string Normalizar(string texto)
    {
        if (texto == null) return "";
        string resultado = "";
        int inicio = 0, fin = texto.Length - 1;
        while (inicio <= fin && texto[inicio] == ' ') inicio++;
        while (fin >= inicio && texto[fin] == ' ') fin--;

        for (int i = inicio; i <= fin; i++)
        {
            char c = texto[i];
            if (c >= 'A' && c <= 'Z') c = (char)(c + 32); 
            resultado += c;
        }
        return resultado;
    }

    public void Insertar(string palabra)
    {
        string normalizada = Normalizar(palabra);
        if (normalizada == "") return;

        NodoTrie actual = raiz;

        for (int i = 0; i < normalizada.Length; i++)
        {
            char letra = normalizada[i];
            int indice = letra % 256; 

            if (actual.Hijos[indice] == null)
            {
                actual.Hijos[indice] = new NodoTrie();
            }
            actual = actual.Hijos[indice];
        }

        actual.FinDePalabra = true;
        actual.ValorOriginal = palabra; // Se guarda la palabra original con mayúsculas y espacios
    }

    public ListaCadenas Autocompletar(string prefijo)
    {
        string prefijoNorm = Normalizar(prefijo);
        ListaCadenas resultado = new ListaCadenas();
        
        NodoTrie actual = raiz;
        for (int i = 0; i < prefijoNorm.Length; i++)
        {
            int indice = prefijoNorm[i] % 256;
            if (actual.Hijos[indice] == null) return resultado;
            actual = actual.Hijos[indice];
        }

        RecolectarPalabras(actual, resultado);
        return resultado;
    }

    private void RecolectarPalabras(NodoTrie nodo, ListaCadenas resultado)
    {
        if (nodo.FinDePalabra)
        {
            resultado.Agregar(nodo.ValorOriginal);
        }

        for (int i = 0; i < 256; i++)
        {
            if (nodo.Hijos[i] != null)
            {
                RecolectarPalabras(nodo.Hijos[i], resultado);
            }
        }
    }
}

// ALGORITMO DIJKSTRA Y GRAFOS


class SubEstacion
{
    public string codigo;
    public string nombre;
    public int capacidad;

    public SubEstacion(string codigo, string nombre, int capacidad)
    {
        this.codigo = codigo;
        this.nombre = nombre;
        this.capacidad = capacidad;
    }
}

class Linea
{
    public int longitud;
    public int costo;

    public Linea(int longitud, int costo)
    {
        this.longitud = longitud;
        this.costo = costo;
    }
}

class Arista
{
    public Linea linea;
    public string origen;
    public string destino;
    public bool bidireccional;

    public Arista(Linea linea, string origen, string destino, bool bidireccional)
    {
        this.linea = linea;
        this.origen = origen;
        this.destino = destino;
        this.bidireccional = bidireccional;
    }
}

class ResultadoRuta
{
    public string[] estaciones;
    public long costoTotal;

    public ResultadoRuta(string[] estaciones, long costoTotal)
    {
        this.estaciones = estaciones;
        this.costoTotal = costoTotal;
    }
}

class GrafoEstaciones
{
    private readonly SubEstacion[] estaciones;
    private readonly Arista[] aristas;
    private int cantidadEstaciones;
    private int cantidadAristas;
    public Trie BuscadorNombres; // Trie integrado en el grafo

    public GrafoEstaciones(int capacidadEstaciones, int capacidadAristas)
    {
        estaciones = new SubEstacion[capacidadEstaciones];
        aristas = new Arista[capacidadAristas];
        BuscadorNombres = new Trie();
    }

    public void AgregarEstacion(SubEstacion estacion)
    {
        estaciones[cantidadEstaciones++] = estacion;
        BuscadorNombres.Insertar(estacion.nombre); // Llena el Trie automáticamente
    }

    public static GrafoEstaciones CargarDesdeCsv(string rutaCsv)
    {
        string[] filas = File.ReadAllLines(rutaCsv);
        
        List<SubEstacion> estacionesCsv = new List<SubEstacion>();
        List<Arista> lineasCsv = new List<Arista>();

        for (int i = 0; i < filas.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(filas[i])) continue;

            string[] campos = filas[i].Split(',');
            if (campos[0].Trim().Equals("Tipo", StringComparison.OrdinalIgnoreCase)) continue;

            string tipo = campos[0].Trim();
            if (tipo.Equals("ESTACION", StringComparison.OrdinalIgnoreCase))
            {
                estacionesCsv.Add(new SubEstacion(campos[1].Trim(), campos[2].Trim(), int.Parse(campos[3].Trim())));
            }
            else if (tipo.Equals("LINEA", StringComparison.OrdinalIgnoreCase))
            {
                bool bidireccional = bool.Parse(campos[8].Trim());
                lineasCsv.Add(new Arista(
                    new Linea(int.Parse(campos[6].Trim()), int.Parse(campos[7].Trim())),
                    campos[4].Trim(), campos[5].Trim(), bidireccional));
            }
        }

        GrafoEstaciones grafo = new GrafoEstaciones(estacionesCsv.Count, lineasCsv.Count);
        foreach (SubEstacion est in estacionesCsv) grafo.AgregarEstacion(est);
        foreach (Arista arista in lineasCsv)
        {
            grafo.aristas[grafo.cantidadAristas++] = arista;
        }

        return grafo;
    }

    public int BuscarEstacionPorCodigo(string codigo)
    {
        for (int i = 0; i < cantidadEstaciones; i++)
        {
            if (estaciones[i].codigo == codigo) return i;
        }
        return -1;
    }

    public SubEstacion ObtenerEstacionPorCodigo(string codigo)
    {
        int indice = BuscarEstacionPorCodigo(codigo);
        return indice >= 0 ? estaciones[indice] : null;
    }

    public ResultadoRuta Dijkstra(string codigoOrigen, string codigoDestino)
    {
        int indiceOrigen = BuscarEstacionPorCodigo(codigoOrigen);
        int indiceDestino = BuscarEstacionPorCodigo(codigoDestino);
        
        if (indiceOrigen < 0 || indiceDestino < 0) return new ResultadoRuta(new string[0], long.MaxValue);

        long[] costos = new long[cantidadEstaciones];
        int[] anterior = new int[cantidadEstaciones];
        bool[] visitada = new bool[cantidadEstaciones];

        for (int i = 0; i < cantidadEstaciones; i++)
        {
            costos[i] = long.MaxValue;
            anterior[i] = -1;
        }
        costos[indiceOrigen] = 0;

        for (int paso = 0; paso < cantidadEstaciones; paso++)
        {
            int actual = -1;
            for (int i = 0; i < cantidadEstaciones; i++)
            {
                if (!visitada[i] && costos[i] != long.MaxValue && (actual < 0 || costos[i] < costos[actual]))
                    actual = i;
            }

            if (actual < 0 || actual == indiceDestino) break;
            visitada[actual] = true;

            for (int i = 0; i < cantidadAristas; i++)
            {
                Arista arista = aristas[i];
                int vecino = -1;

                if (arista.origen == estaciones[actual].codigo) vecino = BuscarEstacionPorCodigo(arista.destino);
                else if (arista.bidireccional && arista.destino == estaciones[actual].codigo) vecino = BuscarEstacionPorCodigo(arista.origen);

                if (vecino >= 0 && !visitada[vecino] && costos[actual] <= long.MaxValue - arista.linea.costo)
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

        if (costos[indiceDestino] == long.MaxValue) return new ResultadoRuta(new string[0], long.MaxValue);

        int cantidadRuta = 0;
        for (int actual = indiceDestino; actual >= 0; actual = anterior[actual]) cantidadRuta++;

        string[] ruta = new string[cantidadRuta];
        int posicion = cantidadRuta - 1;
        for (int actual = indiceDestino; actual >= 0; actual = anterior[actual]) ruta[posicion--] = estaciones[actual].codigo;

        return new ResultadoRuta(ruta, costos[indiceDestino]);
    }

    public void MostrarDatos()
    {
        Console.WriteLine("\nEstaciones:");
        for (int i = 0; i < cantidadEstaciones; i++)
            Console.WriteLine($"  {estaciones[i].codigo} - {estaciones[i].nombre}");
    }
}


// MENÚ PRINCIPAL 

class Program
{
    static string Pedir(string mensaje)
    {
        Console.Write(mensaje);
        string input = Console.ReadLine();
        return input == null ? "" : input.Trim();
    }

    static void Main()
    {
        GrafoEstaciones grafo = new GrafoEstaciones(0, 0);
        bool continuar = true;

        while (continuar)
        {
            Console.WriteLine("\n=== SISTEMA DE CONEXIONES ELÉCTRICAS ===");
            Console.WriteLine("1. Cargar datos desde CSV (datos.csv)");
            Console.WriteLine("2. Mostrar estaciones");
            Console.WriteLine("3. Buscar central por prefijo de NOMBRE (Trie)");
            Console.WriteLine("4. Buscar estación por CÓDIGO");
            Console.WriteLine("5. Calcular ruta de menor costo (Dijkstra)");
            Console.WriteLine("0. Salir");

            string opcion = Pedir("Seleccione una opción: ");

            switch (opcion)
            {
                case "1":
                    string rutaCsv = Pedir("Ruta del archivo [datos.csv]: ");
                    if (string.IsNullOrWhiteSpace(rutaCsv)) rutaCsv = "datos.csv";
                    try
                    {
                        grafo = GrafoEstaciones.CargarDesdeCsv(rutaCsv);
                        Console.WriteLine("CSV cargado correctamente. El Trie se actualizó con los nombres de las centrales.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error al cargar el CSV: " + ex.Message);
                    }
                    break;

                case "2":
                    grafo.MostrarDatos();
                    break;

                case "3": // <-- Opción enlazada al Trie
                    string prefijo = Pedir("Ingrese el inicio del nombre de la central: ");
                    if (grafo.BuscadorNombres != null)
                    {
                        ListaCadenas sugerencias = grafo.BuscadorNombres.Autocompletar(prefijo);
                        if (sugerencias.cantidad == 0)
                        {
                            Console.WriteLine("No se encontraron centrales con ese nombre.");
                        }
                        else
                        {
                            Console.WriteLine($"Centrales encontradas ({sugerencias.cantidad}):");
                            for (int i = 0; i < sugerencias.cantidad; i++)
                            {
                                Console.WriteLine(" - " + sugerencias.elementos[i]);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Primero debe cargar el archivo CSV.");
                    }
                    break;

                case "4":
                    string codigoBuscar = Pedir("Código de la estación a buscar: ");
                    SubEstacion est = grafo.ObtenerEstacionPorCodigo(codigoBuscar);
                    if (est == null) Console.WriteLine("No existe una estación con ese código.");
                    else Console.WriteLine($"Encontrada: {est.codigo} - {est.nombre} (Capacidad: {est.capacidad})");
                    break;

                case "5":
                    string origen = Pedir("Código de la estación de origen: ");
                    string destino = Pedir("Código de la estación de destino: ");
                    ResultadoRuta resultado = grafo.Dijkstra(origen, destino);
                    if (resultado.estaciones.Length == 0)
                        Console.WriteLine("No existe una ruta entre esas estaciones o los códigos son incorrectos.");
                    else
                    {
                        Console.WriteLine("Ruta: " + string.Join(" -> ", resultado.estaciones));
                        Console.WriteLine("Costo total: " + resultado.costoTotal);
                    }
                    break;

                case "0":
                    continuar = false;
                    Console.WriteLine("Fin del programa.");
                    break;

                default:
                    Console.WriteLine("Opción inválida.");
                    break;
            }
        }
    }
}