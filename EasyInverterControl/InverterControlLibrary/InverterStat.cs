using System.Globalization;


namespace InverterControlLibrary
{
    public class InverterStat
    {
        public CultureInfo CultureInfo { get; } = System.Globalization.CultureInfo.InvariantCulture;
        public InverterStat(string rawresponse)
        {
            if (rawresponse.StartsWith("("))
            {
                var proccessdata = rawresponse.Replace("(", "").Split(' ');
                GridVoltage = decimal.Parse(proccessdata[0], CultureInfo);
                GridFrequency = decimal.Parse(proccessdata[1], CultureInfo);
                OutputVoltage = decimal.Parse(proccessdata[2], CultureInfo);
                OutputFrequency = decimal.Parse(proccessdata[3], CultureInfo);
                OutputApparentPower = decimal.Parse(proccessdata[4], CultureInfo);
                OutputActivePower = decimal.Parse(proccessdata[5], CultureInfo);
                OutputLoadPercent = decimal.Parse(proccessdata[6], CultureInfo);
                BusVoltage = decimal.Parse(proccessdata[7], CultureInfo);
                BatteryVoltage = decimal.Parse(proccessdata[8], CultureInfo);
                BatteryChargingCurrent = decimal.Parse(proccessdata[9], CultureInfo);
                BatteryCapacity = decimal.Parse(proccessdata[10], CultureInfo);
                InverterHeatSinkTemperature = decimal.Parse(proccessdata[11], CultureInfo);
                SolarInputCurrentForBattery = decimal.Parse(proccessdata[12], CultureInfo);
                PVVoltage = decimal.Parse(proccessdata[13], CultureInfo);
                PVWattage = decimal.Parse(proccessdata[19], CultureInfo);
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
        public decimal PVVoltage { get; }
        public decimal PVWattage { get; }
    }
}
