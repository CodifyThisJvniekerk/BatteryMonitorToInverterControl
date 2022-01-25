using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UIDeskAutomationLib;

namespace TestApp
{
    class Program
    {
        static int _proccessid;
        static Engine engine;
        static InverterControlLibrary.InverterComander InverterComander = new InverterControlLibrary.InverterComander();
        
        public static void Main(string[] args)
        {
            try
            {
                Process[] battaryProcess = Process.GetProcessesByName("BattaryMonitor");
                if(battaryProcess.Length >= 1)
                {
                    battaryProcess[0].Kill();
                }
                engine = new Engine();

                _proccessid = engine.StartProcess($"{Directory.GetCurrentDirectory()}\\BattaryMonitor.exe");
                UIDA_Window mainWindow = engine.GetTopLevelByProcId(_proccessid, "Battery Monitor V2.1.8");
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
                _ = Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(1000);
                        if(InverterComander.TryGetInverterStats("COM1", out var inverterStat, out string stats, out string error))
                        {
                            PVVoltage = inverterStat.PVVoltage;
                            PVWatage = inverterStat.PVWattage;
                            ACLoad = inverterStat.OutputActivePower;
                            dataRaw = stats;
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
                    if (soc < 20.5m && !UsingEskom && soc != 0)
                    {
                        UsingEskom = true;
                        InverterComander.ChangeToSolarUtilityBattery("COM1");
                        Console.WriteLine($"Back to Grid at {DateTime.Now}");
                        using (var writer = File.AppendText($"{Directory.GetCurrentDirectory()}\\Commanderlog.txt"))
                        {
                            writer.WriteLine($"Back to Grid at { DateTime.Now} \n");
                        }
                    }
                    else if (soc > 35 && UsingEskom && soc != 0)
                    {
                        UsingEskom = false;
                        InverterComander.ChangeToSolarBatteryUtility("COM1");
                        Console.WriteLine($"Back to Battery at {DateTime.Now}");
                        using (var writer = File.AppendText($"{Directory.GetCurrentDirectory()}\\Commanderlog.txt"))
                        {
                            writer.WriteLine($"Back to Battery {DateTime.Now} \n");
                        }
                    }
                }
            }
            finally
            {
            }
        }
    }
}
