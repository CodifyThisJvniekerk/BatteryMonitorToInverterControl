﻿using InverterControlLibrary;
using System.Diagnostics;
using System.Text;
using UIDeskAutomationLib;

namespace TestApp
{
    class Program
    {
        static InverterComander inverterComander = new InverterComander();

        public static void Main(string[] args)
        {
            try
            {
                Engine engine = new Engine();

                int procId = engine.StartProcess($"{Directory.GetCurrentDirectory()}\\BattaryMonitor.exe");
                UIDA_Window mainWindow = engine.GetTopLevelByProcId(procId, "Battery Monitor V2.1.8");
                foreach (var button in mainWindow.Buttons("Connect", searchDescendants: true))
                {
                    button.Invoke();
                    button.WaitForInputIdle(1000);
                }
                decimal soc = 0m;
                decimal current = 0m;
                string state = "";
                _ = Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(1000);
                        List<string> values = new List<string>();
                        int index = 0;
                        foreach (var label in mainWindow.TabCtrls()[0].TabItems()[0].Labels(searchDescendants: true))
                        {
                            ++index;
                            if (index == 4)
                            {
                                current = decimal.Parse(label.Text.Replace("A", ""));
                            }
                            else if (index == 6)
                            {
                                soc = decimal.Parse(label.GetText().Replace("%", ""));
                            }
                            else if (index == 8)
                            {
                                state = label.GetText();
                                break;
                            }
                        }
                    }
                });
                decimal PVVoltage = 0;
                decimal PVWatage = 0;
                decimal ACLoad = 0;
                string dataRaw = string.Empty;
                decimal[] perminutepv = new decimal[60];
                _ = Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(1000);
                        if(inverterComander.TryGetInverterStats("COM1",out var inverterStat, out string stats,out string error))
                        {
                            dataRaw = stats;
                            PVVoltage = inverterStat.PVVoltage;
                            PVWatage = inverterStat.PVWattage;
                            ACLoad = inverterStat.OutputActivePower;
                        }
                    }
                });
                bool UsingEskom = false;
                int loopcounter = 0;
                Thread.Sleep(2000);
                while (true)
                {
                    if (loopcounter > 59)
                    {
                        Console.Clear();
                        loopcounter = 0;
                    }
                    ++loopcounter;
                    Thread.Sleep(3000);
                    Console.WriteLine($"SOC : {soc}%");
                    Console.WriteLine($"Current : {current}A");
                    Console.WriteLine($"Status : {state}");
                    Console.WriteLine($"Inverter PV Input Wattage: {PVWatage}");
                    Console.WriteLine($"Inverter AC Load: {ACLoad}");
                    Console.WriteLine($"Inverter PV Voltage: {PVVoltage}");
                    Console.WriteLine($"Inverter full response: {dataRaw}");
                    if (soc <= 20.5m && !UsingEskom && soc != 0)
                    {
                        UsingEskom = true;
                        inverterComander.ChangeToSolarUtilityBattery("COM1");
                        Console.WriteLine($"Back to Grid at {DateTime.Now}");
                        File.AppendAllText($"{Directory.GetCurrentDirectory()}\\Commanderlog.txt", $"Back to Grid at {DateTime.Now} \n");
                    }
                    else if (soc > 35 && UsingEskom && soc != 0)
                    {
                        UsingEskom = false;
                        inverterComander.ChangeToSolarBatteryUtility("COM1");
                        Console.WriteLine($"Back to Battery at {DateTime.Now}");
                        File.AppendAllText($"{Directory.GetCurrentDirectory()}\\Commanderlog.txt", $"Back to Battery {DateTime.Now} \n");
                    }
                }
            }
            finally
            {
            }
        }
    }
}