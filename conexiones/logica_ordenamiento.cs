using System;
using System.Collections.Generic;
using System.IO;

// Subestacion: codigo, nombre y capacidad
// linea bidireccional: conecta dos subestacion, cuenta con longitud y costo de mantenimiento
// linea unidireccional: solo tiene un sentido de la subestacion principal a las secundarias, tambien tiene longitus y costo
// usar algoritmo dijkstra para calcular la ruta de menor costo para restablecer el suministro entre dos puntos 


//---- decalaracion de los esqueletos ----
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

class Linea //datos de la arista 
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

    public Arista(Linea linea, string origen, string destino)
        : this(linea, origen, destino, true)
    {
    }

    public Arista(Linea linea, string origen, string destino, bool bidireccional)
    {
        this.linea = linea;
        this.origen = origen;
        this.destino = destino;
        this.bidireccional = bidireccional;
    }
}







