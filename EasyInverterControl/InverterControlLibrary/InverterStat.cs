using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InverterControlLibrary
{
    public class InverterStat
    {
        public InverterStat(string rawresponse)
        {
            if (rawresponse.StartsWith("("))
            {
                var proccessdata = rawresponse.Replace("(", "").Split(' ');
                GridVoltage = decimal.Parse(proccessdata[0]);
                GridFrequency = decimal.Parse(proccessdata[1]);
                OutputVoltage = decimal.Parse(proccessdata[2]);
                OutputFrequency = decimal.Parse(proccessdata[3]);
                OutputApparentPower = decimal.Parse(proccessdata[4]);
                OutputActivePower = decimal.Parse(proccessdata[5]);
                OutputLoadPercent = decimal.Parse(proccessdata[6]);
                BusVoltage = decimal.Parse(proccessdata[7]);
                BatteryVoltage = decimal.Parse(proccessdata[8]);
                BatteryChargingCurrent = decimal.Parse(proccessdata[9]);
                BatteryCapacity = decimal.Parse(proccessdata[10]);
                InverterHeatSinkTemperature = decimal.Parse(proccessdata[11]);
                SolarInputCurrentForBattery = decimal.Parse(proccessdata[12]);
            }
        }

        public decimal GridVoltage { get; }
        public decimal GridFrequency { get; }
        public decimal OutputVoltage { get; }
        public decimal OutputFrequency { get; }
        public decimal OutputApparentPower { get; }
        public decimal OutputActivePower { get; }
        public decimal OutputLoadPercent { get; }
        public decimal BusVoltage { get; }
        public decimal BatteryVoltage { get; }
        public decimal BatteryChargingCurrent { get; }
        public decimal BatteryCapacity { get; }
        public decimal InverterHeatSinkTemperature { get; }
        public decimal SolarInputCurrentForBattery { get; }
    }
}
