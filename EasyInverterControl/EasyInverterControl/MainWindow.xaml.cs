using InverterControlLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EasyInverterControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public CancellationTokenSource PVChartCancellation { get; set; }
        public Visibility PVWattageChartVisible { get; set; } = Visibility.Visible;
        public ObservableCollection<PVWattage> PVWattages { get; } = new ObservableCollection<PVWattage>();
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            InverterCommander = new InverterComander();
        }

        private InverterComander InverterCommander { get; }

        public string InverterResponse { get; private set; }

        public string CommandResponse { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void GetStats_Click(object sender, RoutedEventArgs e)
        {
            InverterResponse = "";
            OnPropertyChanged(nameof(InverterResponse));
            if (InverterCommander.TryGetInverterStats(comport.Text, out var inverterStat, out string stats, out string error))
            {
                MessageBox.Show(stats);
                InverterResponse = stats;
                OnPropertyChanged(nameof(InverterResponse));
            }
            else
            {
                MessageBox.Show(error);
                CommandResponse = error;
                OnPropertyChanged(nameof(CommandResponse));
            }
        }

        private void ChangeToGrid_Click(object sender, RoutedEventArgs e)
        {
            CommandResponse = InverterCommander.ChangeToSolarUtilityBattery(comport.Text);
            OnPropertyChanged(nameof(CommandResponse));
        }

        private void ChangeToSolarBatteryUtility_Click(object sender, RoutedEventArgs e)
        {
            CommandResponse = InverterCommander.ChangeToSolarBatteryUtility(comport.Text);
            OnPropertyChanged(nameof(CommandResponse));
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            PVChartCancellation = new CancellationTokenSource();
            CancellationToken token = PVChartCancellation.Token;
            _ = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(1000);
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (InverterCommander.TryGetInverterStats(comport.Text, out var inverterStat, out string stats, out string error))
                        {
                            PVWattages.Add(new PVWattage(DateTime.Now, inverterStat.PVWattage));
                        }
                        InverterResponse = $"Wattage {inverterStat.PVWattage}";
                    }, System.Windows.Threading.DispatcherPriority.Normal);
                } 
            }, token);
        }

        private void cbShowWattageGraph_Unchecked(object sender, RoutedEventArgs e)
        {
            PVChartCancellation.Cancel();
        }
    }
}
