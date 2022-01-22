using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyInverterControl
{
    public class PVWattage
    {
        public PVWattage(DateTime now, decimal pVWattage)
        {
            this.Time = now;
            this.Wattage = pVWattage;
        }

        public DateTime Time { get; set; }
        public decimal Wattage { get; set; }
    }
}
