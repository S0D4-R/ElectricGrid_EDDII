using ElectricGrid.Collections;
using ElectricGrid.DataStructures;
using ElectricGrid.Models;
using ElectricGrid.Services;

namespace ElectricGrid.Tui
{
    // Copia de la red en arreglos para dibujarla.
    internal sealed class GraphSnapshot
    {
        private readonly HashTable<int> _indexByCode;

        // Guarda los nodos, aristas y zonas de la copia.
        private GraphSnapshot(Substation[] nodes, HashTable<int> indexByCode, int[] from, int[] to,
                              bool[] twoWay, int[] zoneOfNode, int zoneCount)
        {
            Nodes = nodes;
            _indexByCode = indexByCode;
            EdgeFrom = from;
            EdgeTo = to;
            EdgeTwoWay = twoWay;
            ZoneOfNode = zoneOfNode;
            ZoneCount = zoneCount;
        }

        public Substation[] Nodes { get; }

        public int[] EdgeFrom { get; }

        public int[] EdgeTo { get; }

        public bool[] EdgeTwoWay { get; }

        public int[] ZoneOfNode { get; }

        public int ZoneCount { get; }

        // Lee el servicio y arma los arreglos que usa el dibujo.
        public static GraphSnapshot Build(IPowerGridService service)
        {
            Substation[] nodes = service.GetAllSubstations();
            HashTable<int> index = new HashTable<int>(nodes.Length * 2 + 17);
            for (int i = 0; i < nodes.Length; i++)
            {
                index.Insert(nodes[i].Code, i);
            }

            PowerLine[] lines = service.GetAllLines();
            int[] from = new int[lines.Length];
            int[] to = new int[lines.Length];
            bool[] twoWay = new bool[lines.Length];
            int count = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                if (index.TryGetValue(lines[i].SourceCode, out int a) && index.TryGetValue(lines[i].TargetCode, out int b))
                {
                    from[count] = a;
                    to[count] = b;
                    twoWay[count] = lines[i].IsBidirectional;
                    count++;
                }
            }

            ArrayUtils.Resize(ref from, count);
            ArrayUtils.Resize(ref to, count);
            ArrayUtils.Resize(ref twoWay, count);

            int[] zoneOfNode = new int[nodes.Length];
            ElectricalZone[] zones = service.GetZones();
            for (int z = 0; z < zones.Length; z++)
            {
                for (int m = 0; m < zones[z].Members.Length; m++)
                {
                    if (index.TryGetValue(zones[z].Members[m].Code, out int nodeIndex))
                    {
                        zoneOfNode[nodeIndex] = z;
                    }
                }
            }

            return new GraphSnapshot(nodes, index, from, to, twoWay, zoneOfNode, zones.Length);
        }

        // Devuelve el índice de un código, o -1 si no está.
        public int IndexOf(string? code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return -1;
            }

            return _indexByCode.TryGetValue(code, out int index) ? index : -1;
        }

        // Cuenta las líneas que tocan al nodo.
        public int LineCount(int node)
        {
            int total = 0;
            for (int e = 0; e < EdgeFrom.Length; e++)
            {
                if (EdgeFrom[e] == node || EdgeTo[e] == node)
                {
                    total++;
                }
            }

            return total;
        }
    }
}
