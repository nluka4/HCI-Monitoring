using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkService.Model
{
    public enum DERType
    {
        SolarPanel,
        WindTurbine
    }
    public class DER
    {
        public int id { get; set; }
        public string name { get; set; }
        public DERType type { get; set; }
        public double vrednost { get; set; }

        public DER(int id, string name, DERType type, double vrednost)
        {
            this.id = id;
            this.name = name;
            this.type = type;
            this.vrednost = vrednost;
        }   
    }
}
