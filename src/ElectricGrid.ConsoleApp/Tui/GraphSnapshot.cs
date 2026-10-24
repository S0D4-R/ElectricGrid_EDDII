using System;
using ElectricGrid.Core.DataStructures;
using ElectricGrid.Core.Models;
using ElectricGrid.Core.Services;

namespace ElectricGrid.ConsoleApp.Tui
{
    /// <summary>
    /// Immutable, array-based copy of the network taken from <see cref="IPowerGridService"/>,
    /// prepared for drawing: nodes, edges expressed as node indices and the zone of every node.
    /// The lookup code -> index reuses the project's own <see cref="HashTable{TValue}"/>.
    /// </summary>
    internal sealed class GraphSnapshot
    {
        private readonly HashTable<int> _indexByCode;

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

        /// <summary>Zero-based electrical zone of each node (Union-Find component).</summary>
        public int[] ZoneOfNode { get; }

        public int ZoneCount { get; }

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

            Array.Resize(ref from, count);
            Array.Resize(ref to, count);
            Array.Resize(ref twoWay, count);

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

        /// <summary>Index of a substation code, or -1.</summary>
        public int IndexOf(string? code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return -1;
            }

            return _indexByCode.TryGetValue(code, out int index) ? index : -1;
        }

        /// <summary>Number of lines that touch the node.</summary>
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
