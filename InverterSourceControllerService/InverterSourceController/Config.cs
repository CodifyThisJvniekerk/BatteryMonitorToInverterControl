using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InverterSourceController
{
    public class Config
    {
        public string BatteryMonitorFolder { get; set; }
        public string InverterComPort { get; set; }
        public decimal BackToGridSOC { get; set; }
        public decimal BackToBattarySOC { get; set; }
    }
}
