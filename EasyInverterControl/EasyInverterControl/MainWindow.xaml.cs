using InverterControlLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
            if(InverterCommander.TryGetInverterStats(comport.Text,out var inverterStat, out string stats, out string error))
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
    }
}
