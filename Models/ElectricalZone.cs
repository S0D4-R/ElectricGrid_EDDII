namespace ElectricGrid.Models
{
    public sealed class ElectricalZone
    {
        // Crea la zona y suma la capacidad de sus miembros.
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

        public int Id { get; }

        public Substation[] Members { get; }

        public double TotalCapacityMw { get; }
    }
}
