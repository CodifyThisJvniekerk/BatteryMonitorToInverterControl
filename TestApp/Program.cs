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
        private static bool _gotResponse;
        static MemoryStream _rxBuffer = new MemoryStream();
        private string AxpertResponse;

        public static void Main(string[] args)
		{
			Engine engine = new Engine();
			
            int procId = engine.StartProcess($"{Directory.GetCurrentDirectory()}\\BattaryMonitor.exe");
            UIDA_Window mainWindow = engine.GetTopLevelByProcId(procId, "Battery Monitor V2.1.8");
            foreach(var button in mainWindow.Buttons("Connect", searchDescendants: true))
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
            decimal PVVoltage = 0;
            decimal PVWatage = 0;
            decimal ACLoad = 0;
            string dataRaw = string.Empty;
            decimal[] perminutepv = new decimal[60];
            decimal TotalMinutePVWattage = 0;
            int statloopcount = 0;
            int minuteCount = 0;
            _ = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    string data = GetStatsViaThirdParty();
                    if (data.Length == 0)
                    {
                        return;
                    }
                    dataRaw = data;
                    var info = data.Split(' ');
                    PVVoltage = decimal.Parse(info[13]);
                    PVWatage = decimal.Parse(info[19]);
                    TotalMinutePVWattage += PVWatage;
                    ACLoad = decimal.Parse(info[5]);
                    if (statloopcount == 60)
                    {
                        perminutepv[minuteCount] = TotalMinutePVWattage / 60;
                        ++minuteCount;
                        if (minuteCount == 59)
                        {
                            decimal totalforhour = 0;
                            foreach (var minuteaverage in perminutepv)
                            {
                                totalforhour += minuteaverage;
                            }
                            File.AppendAllText($"{Directory.GetCurrentDirectory()}\\Commanderlog.txt", $"Total watts for hour {DateTime.Now} {(totalforhour / 60)} \n");
                            minuteCount = 0;
                        }
                    }
                    ++statloopcount;
                }
            });
            bool UsingEskom = false;
            int loopcounter = 0;
            Thread.Sleep(2000);
            while (true)
            {
                if(loopcounter > 59)
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
                if(soc <= 20.5m && !UsingEskom && soc!=0)
                {
                    UsingEskom = true;
                    SwitchToUtility();
                    Console.WriteLine($"Back to Grid at {DateTime.Now}");
                    File.AppendAllText($"{Directory.GetCurrentDirectory()}\\Commanderlog.txt", $"Back to Grid at {DateTime.Now} \n");
                }
                else if(soc > 35 && UsingEskom && soc != 0)
                {
                    UsingEskom = false;
                    SwitchToBackToBattery();
                    Console.WriteLine($"Back to Battery at {DateTime.Now}");
                    File.AppendAllText($"{Directory.GetCurrentDirectory()}\\Commanderlog.txt", $"Back to Battery {DateTime.Now} \n");
                }
            }
        }

        private static void SwitchToBackToBattery()
        {
            CreateAxpertTestProcess(" -p COM1 QPOP01").Start();
            //ExecuteInverterCommand("QPOP02", "COM1", 2400);
        }

        private static void SwitchToUtility()
        {
            CreateAxpertTestProcess(" -p COM1 QPOP02").Start();
            //ExecuteInverterCommand("QPOP01", "COM1", 2400);
        }

        public static Process CreateAxpertTestProcess(string arguments)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = $"{Directory.GetCurrentDirectory()}\\AxpertTest.exe",
                    Arguments = " -p COM1 QPIGS",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
        }

        private static string GetStatsViaThirdParty()
        {
            var proccess = CreateAxpertTestProcess(" -p COM1 QPIGS");
            proccess.Start();
            var response = proccess.StandardOutput.ReadToEnd();
            proccess.WaitForExit();
            return response;
        }

        private static string GetInverterStats()
        {
            ExecuteInverterCommand("QPIGS", "COM1", 2400);
            if(!TryGetResponse(out var AxpertResponse))
            {
                Console.WriteLine("Inverter did not respond");
            };
            return AxpertResponse;
        }

        private static bool TryGetResponse(out string result)
        {
            result = string.Empty;
            if (!_gotResponse)
                return false;

            if (_rxBuffer.Length < 3)
                return false;

            byte[] payloadBytes = new byte[_rxBuffer.Length - 3];
            Array.Copy(_rxBuffer.GetBuffer(), payloadBytes, payloadBytes.Length);

            ushort crcMsb = _rxBuffer.GetBuffer()[_rxBuffer.Length - 3];
            ushort crcLsb = _rxBuffer.GetBuffer()[_rxBuffer.Length - 2];

            ushort calculatedCrc = CalculateCrc(payloadBytes);
            ushort receivedCrc = (ushort)((crcMsb << 8) | crcLsb);
            if (calculatedCrc != receivedCrc)
                return false;

            //Write response to console
            result = Encoding.ASCII.GetString(payloadBytes);
            return true;
        }

        public static void ExecuteInverterCommand(string commandText, string cpmPortName, int baud)
        {
            SerialPort sp = new SerialPort();
            sp.PortName = cpmPortName;
            sp.BaudRate = baud;
            sp.DataBits = 8;
            sp.Parity = Parity.None;
            sp.StopBits = StopBits.One;

            sp.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            sp.ErrorReceived += new SerialErrorReceivedEventHandler(DataErrorReceivedHandler);

            sp.Open();

            byte[] commandBytes = GetMessageBytes(commandText);

            //Flush out any existing chars
            sp.ReadExisting();

            //Send request
            sp.Write(commandBytes, 0, commandBytes.Length);

            //Wait for response
            var startTime = DateTime.Now;
            while (!_gotResponse && ((DateTime.Now - startTime).TotalMilliseconds < 1000))
            {
                Thread.Sleep(20);
            }

            sp.Close();
        }

        /// <summary>
        /// Appends crc and CR bytes to a byte array
        /// </summary>
        static byte[] GetMessageBytes(string text)
        {
            //Get bytes for command
            byte[] command = Encoding.ASCII.GetBytes(text);

            //Get CRC for command bytes
            ushort crc = CalculateCrc(command);

            //Append CRC and CR to command
            byte[] result = new byte[command.Length + 3];
            command.CopyTo(result, 0);
            result[result.Length - 3] = (byte)((crc >> 8) & 0xFF);
            result[result.Length - 2] = (byte)((crc >> 0) & 0xFF);
            result[result.Length - 1] = 0x0d;

            return result;
        }

        /// <summary>
        /// Calculates CRC for axpert inverter
        /// Ported from crc.c: http://forums.aeva.asn.au/forums/pip4048ms-inverter_topic4332_page2.html
        /// </summary>
        static ushort CalculateCrc(byte[] pin)
        {
            ushort crc;
            byte da;
            byte ptr;
            byte bCRCHign;
            byte bCRCLow;

            int len = pin.Length;

            ushort[] crc_ta = new ushort[]
                {
                    0x0000,0x1021,0x2042,0x3063,0x4084,0x50a5,0x60c6,0x70e7,
                    0x8108,0x9129,0xa14a,0xb16b,0xc18c,0xd1ad,0xe1ce,0xf1ef
                };

            crc = 0;
            for (int index = 0; index < len; index++)
            {
                ptr = pin[index];

                da = (byte)(((byte)(crc >> 8)) >> 4);
                crc <<= 4;
                crc ^= crc_ta[da ^ (ptr >> 4)];
                da = (byte)(((byte)(crc >> 8)) >> 4);
                crc <<= 4;
                crc ^= crc_ta[da ^ (ptr & 0x0f)];
            }

            //Escape CR,LF,'H' characters
            bCRCLow = (byte)(crc & 0x00FF);
            bCRCHign = (byte)(crc >> 8);
            if (bCRCLow == 0x28 || bCRCLow == 0x0d || bCRCLow == 0x0a)
            {
                bCRCLow++;
            }
            if (bCRCHign == 0x28 || bCRCHign == 0x0d || bCRCHign == 0x0a)
            {
                bCRCHign++;
            }
            crc = (ushort)(((ushort)bCRCHign) << 8);
            crc |= bCRCLow;
            return crc;
        }

        static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            var sp = sender as SerialPort;

            if ((sp != null) && (!_gotResponse))
            {
                //Read chars until we hit a CR character
                while (sp.BytesToRead > 0)
                {
                    byte b = (byte)sp.ReadByte();
                    _rxBuffer.WriteByte(b);

                    if (b == 0x0d)
                    {
                        _gotResponse = true;
                        break;
                    }
                }
            }
        }

        static void DataErrorReceivedHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            Console.Write(e.EventType);
        }
    }
}
