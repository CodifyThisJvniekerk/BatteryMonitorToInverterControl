using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using UIDeskAutomationLib;

namespace InverterSourceController
{
    public partial class Service1 : ServiceBase
    {
        public InverterControlLibrary.InverterComander InverterComander { get; set; } = new InverterControlLibrary.InverterComander();
        public Config Config { get; private set; }
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            using (StreamReader r = new StreamReader("ServiceConfig.json"))
            {
                string json = r.ReadToEnd();
                Config = JsonConvert.DeserializeObject<Config>(json);
            }
        }

        protected override void OnStop()
        {
        }

        private void RemainingBatteryChecker_Tick(object sender, EventArgs e)
        {
            var procceses = Process.GetProcessesByName("BattaryMonitor.exe");
            Engine engine = new Engine();
            UIDA_Window mainWindow = null;
            Process BattaryMonitor;
            if (procceses.Any())
            {
                BattaryMonitor = procceses[0];
                mainWindow = engine.GetTopLevelByProcId(BattaryMonitor.Id, "Battery Monitor V2.1.8");
            }
            else
            {
                BattaryMonitor = Process.Start(Config.BatteryMonitorFolder + "BattaryMonitor.exe");
                mainWindow = engine.GetTopLevelByProcId(BattaryMonitor.Id, "Battery Monitor V2.1.8");
                foreach (var button in mainWindow.Buttons("Connect", searchDescendants: true))
                {
                    button.Invoke();
                    button.WaitForInputIdle(1000);
                }
            }
            decimal soc = decimal.Parse(mainWindow.TabCtrls()[0].TabItems()[0].Labels(searchDescendants: true)[6].Text);
            if (TryGetUsingGridStatus(out var usingGrid))
            {
                if (soc >= Config.BackToBattarySOC)
                {
                    if (usingGrid)
                    {
                        InverterComander.ChangeToSolarUtilityBattery(Config.InverterComPort);
                    }
                }
                else if (soc <= Config.BackToGridSOC)
                {
                    if (!usingGrid)
                    {
                        InverterComander.ChangeToSolarBatteryUtility(Config.InverterComPort);
                    }
                }
            }
            
        }

        private bool TryGetUsingGridStatus(out bool usingGrid)
        {
            return InverterComander.TryGetUsingGridStatus(Config.InverterComPort, out usingGrid, out _, out _);
        }
    }
}
