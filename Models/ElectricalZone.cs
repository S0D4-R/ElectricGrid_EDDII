namespace ElectricGrid.Models
{
    /// <summary>Conjunto conectado de subestaciones; corresponde a un componente de Union-Find.</summary>
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

        /// <summary>Número de zona, iniciando en 1.</summary>
        public int Id { get; }

        /// <summary>Subestaciones de la zona.</summary>
        public Substation[] Members { get; }

        /// <summary>Suma de las capacidades de los miembros en MW.</summary>
        public double TotalCapacityMw { get; }
    }
}
