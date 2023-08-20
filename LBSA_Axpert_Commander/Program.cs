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
        static bool usingEskom;

        public static void Main(string[] args)
        {
            try
            {
                engine = new Engine();
                UIDA_Window mainWindow;
                GetBatteryProcessID(out _proccessid);
                ClickConnectButtonAndAwaitIdle(out mainWindow);
                RetrieveInverterOpperatingMode();
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
                    Thread.Sleep(10000);
                    Console.WriteLine($"SOC : {soc}%");
                    Console.WriteLine($"Current : {current}A");
                    Console.WriteLine($"Status : {state}");
                    if (soc < 25m && !IsUsingEskom() && soc != 0)
                    {
                        var response = InverterComander.ChangeToSolarUtilityBattery("COM1");
                        RetrieveInverterOpperatingMode();
                        if (IsUsingEskom())
                        {
                            Console.WriteLine($"Back to Grid at {DateTime.Now}");
                            using (var writer = File.AppendText($"{Directory.GetCurrentDirectory()}\\Commanderlog.txt"))
                            {
                                writer.WriteLine($"Back to Grid at { DateTime.Now} {response} \n");
                            }
                        }
                        else
                        {
                            using (var writer = File.AppendText($"{Directory.GetCurrentDirectory()}\\Commanderlog.txt"))
                            {
                                writer.WriteLine($"Attempt Back to Grid Failed at { DateTime.Now} {response} \n");
                            }
                        }
                        
                    }
                    else if (soc > 35 && !IsUsingEskom() && soc != 0)
                    {
                        var response = InverterComander.ChangeToSolarBatteryUtility("COM1");
                        RetrieveInverterOpperatingMode();
                        if (!IsUsingEskom())
                        {
                            Console.WriteLine($"Back to Battery at {DateTime.Now}");
                            using (var writer = File.AppendText($"{Directory.GetCurrentDirectory()}\\Commanderlog.txt"))
                            {
                                writer.WriteLine($"Back to Battery {DateTime.Now} {response} \n");
                            }
                        }
                        else
                        {
                            using (var writer = File.AppendText($"{Directory.GetCurrentDirectory()}\\Commanderlog.txt"))
                            {
                                writer.WriteLine($"Attempt Back to Battery Failed at { DateTime.Now} {response} \n");
                            }
                        }    
                    }
                }

            }catch (Exception ex)
            {
                using (var writer = File.AppendText($"{Directory.GetCurrentDirectory()}\\Errorlog.txt"))
                {
                    writer.WriteLine($"{ex}");
                }
            }
            finally
            {
            }
        }

        private static void RetrieveInverterOpperatingMode()
        {
            var result = InverterComander.GetCurrentOperatingMode("COM1");
            if (result == "Y" || result == "L")
            {
                usingEskom = true;
            }
            else
            {
                usingEskom = false;
            }
        }

        private static bool IsUsingEskom()
        {
            return usingEskom;
        }

        private static void ClickConnectButtonAndAwaitIdle(out UIDA_Window mainWindow)
        {
            mainWindow = engine.GetTopLevelByProcId(_proccessid, "Battery Monitor V2.1.8");
            foreach (var button in mainWindow.Buttons("Connect", searchDescendants: true))
            {
                button.SimulateClick();
                button.WaitForInputIdle(500);
            }
        }

        private static void GetBatteryProcessID(out int _proccessid)
        {
            try
            {
                Process[] battaryProcess = Process.GetProcessesByName("BattaryMonitor");
                if (battaryProcess.Length >= 1)
                {
                    _proccessid = battaryProcess[0].Id;
                }
                else
                {
                    _proccessid = engine.StartProcess($"{Directory.GetCurrentDirectory()}\\BattaryMonitor.exe");
                }
            }
            catch (Exception ex)
            {
                using (var writer = File.AppendText($"{Directory.GetCurrentDirectory()}\\Errorlog.txt"))
                {
                    writer.WriteLine($"{ex}");
                }
                _proccessid = -1;
            }
        }
    }
}
