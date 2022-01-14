using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UIDeskAutomationLib;

namespace TestApp
{
	class Program
	{
		public static void Main(string[] args)
		{
			Engine engine = new Engine();
            engine.LogFile = "log.txt";
			
            int procId = engine.StartProcess("C:/Users/bbdnet1522/Desktop/BattaryMonitor V2.1.8 End User/BattaryMonitor V2.1.8 End User/BattaryMonitor.exe");
            UIDA_Window mainWindow = engine.GetTopLevelByProcId(procId, "Battery Monitor V2.1.8");
            foreach(var button in mainWindow.Buttons("Connect", searchDescendants: true))
            {
                button.Invoke();
                button.WaitForInputIdle(1500);
            }
            decimal soc = 0m;
            decimal current = 0m;
            string state = "";
            _ = Task.Factory.StartNew(() =>
              {
                  while (true)
                  {
                      List<string> values = new List<string>();
                      int index = 0;
                      foreach (var label in mainWindow.TabCtrls()[0].TabItems()[0].Labels(searchDescendants: true))
                      {
                          ++index;
                          if (index == 4)
                          {
                              current = decimal.Parse(label.Text.Replace("A", ""));
                          }else if (index == 6)
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
            while (true)
            {
                Thread.Sleep(1000);
                Console.Clear();
                Console.WriteLine($"SOC : {soc}%");
                Console.WriteLine($"Current : {current}A");
                Console.WriteLine($"Status : {state}");
            }
        }
	}
}
