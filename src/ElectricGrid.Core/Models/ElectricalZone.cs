namespace ElectricGrid.Core.Models
{
    /// <summary>A connected set of substations (one Union-Find component).</summary>
    public sealed class ElectricalZone
    {
        internal ElectricalZone(int id, Substation[] members)
        {
            Id = id;
            Members = members;

            double total = 0;
            for (int i = 0; i < members.Length; i++)
            {
                total += members[i].CapacityMw;
            }

            TotalCapacityMw = total;
        }

        /// <summary>Sequential zone number (1-based).</summary>
        public int Id { get; }

        /// <summary>Substations in the zone.</summary>
        public Substation[] Members { get; }

        /// <summary>Sum of the capacities of the members (MW).</summary>
        public double TotalCapacityMw { get; }
    }
}
