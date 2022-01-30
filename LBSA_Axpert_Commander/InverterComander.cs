using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace InverterControlLibrary
{
    public class InverterComander
    {
        private bool _gotResponse;
        private MemoryStream _rxBuffer = new MemoryStream();
        private SerialPort ComPort { get; set; }
        public bool TryGetInverterStats(string port, out InverterStat inverterStat, out string stats, out string error)
        {
            stats = string.Empty;
            error = string.Empty;
            inverterStat = null;
            try
            {
                if (TryInnitialize(port, out var information, out var errormessage))
                {
                    ExecuteInverterCommand("QPIGS");
                    if (TryGetResponse(out var data, out var errorDataRetrieval))
                    {
                        stats = data.ToString();
                        inverterStat = new InverterStat(stats);
                        return true;
                    }
                    else
                    {
                        error = errorDataRetrieval.ToString();
                        return false;
                    }
                }
                else
                {
                    error = errormessage;
                    return false;
                }
            }
            finally
            {
                _gotResponse = false;
            }
        }

        public string ChangeToSolarUtilityBattery(string portName)
        {
            try
            {
                if (TryInnitialize(portName, out var Successindicator, out var errormessage))
                {
                    ExecuteInverterCommand("POP01");
                    if (TryGetResponse(out var data, out var errorResponse))
                    {
                        return data;
                    }
                    else
                    {
                        return errorResponse;
                    }
                }
                else
                {
                    return errormessage;
                }
            }
            finally
            {
                _gotResponse = false;
            }
        }

        public string ChangeToSolarBatteryUtility(string portName)
        {
            try
            {
                if (TryInnitialize(portName, out var Successindicator, out string error))
                {
                    ExecuteInverterCommand("POP02");
                    if (TryGetResponse(out var data, out string responseerror))
                    {
                        return data;
                    }
                }
                return Successindicator;
            }
            finally
            {
                _gotResponse = false;
            }
        }

        private bool TryGetResponse(out string result, out string error)
        {
            result = string.Empty;
            error = string.Empty;
            if (!_gotResponse)
            {
                error = "Inverter did not respond";
                return false;
            }
            byte[] payloadBytes = new byte[_rxBuffer.Length - 3];
            Array.Copy(_rxBuffer.GetBuffer(), payloadBytes, payloadBytes.Length);

            ushort crcMsb = _rxBuffer.GetBuffer()[_rxBuffer.Length - 3];
            ushort crcLsb = _rxBuffer.GetBuffer()[_rxBuffer.Length - 2];

            ushort calculatedCrc = CalculateCrc(payloadBytes);
            ushort receivedCrc = (ushort)((crcMsb << 8) | crcLsb);
            if (calculatedCrc != receivedCrc)
            {
                error = "Wrong response";
                return false;
            }
            //Write response to console
            result = Encoding.ASCII.GetString(payloadBytes);
            return true;
        }

        private void ClearMemoryStream()
        {
            var buffer = _rxBuffer.GetBuffer();
            Array.Clear(buffer, 0, buffer.Length);
            _rxBuffer.Position = 0;
            _rxBuffer.SetLength(0);
            _rxBuffer.Capacity = 0;
        }

        private bool TryInnitialize(string portName, out string information, out string error, int buad = 2400)
        {
            error = "";
            information = "success";
            try
            {
                if (ComPort != null && ComPort.IsOpen)
                {
                    error += "Port Already Open";
                    return true;
                }
                else if (ComPort != null && !ComPort.IsOpen)
                {
                    information += "ReOpening Port";
                    ComPort.Open();
                    return true;
                }
                information += "Innitialize Port";
                ComPort = new SerialPort();
                ComPort.PortName = portName;
                ComPort.BaudRate = buad;
                ComPort.DataBits = 8;
                ComPort.Parity = Parity.None;
                ComPort.StopBits = StopBits.One;

                ComPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                ComPort.ErrorReceived += new SerialErrorReceivedEventHandler(DataErrorReceivedHandler);
                ComPort.Open();
            }
            catch (Exception x)
            {
                error = x.ToString();
                using (var writer = File.AppendText($"{Directory.GetCurrentDirectory()}\\Errorlog.txt"))
                {
                    writer.WriteLine($"{error}");
                }
                return false;
            }
            return true;
        }
        private void ExecuteInverterCommand(string commandText)
        {
            var commandBytes = CreateInverterCommand(commandText);
            ExecuteAndAwaitInverterResponse(commandBytes);
        }

        private void ExecuteAndAwaitInverterResponse(byte[] commandBytes)
        {
            ClearMemoryStream();
            //Flush out any existing chars
            ComPort.ReadExisting();

            //Send request
            ComPort.Write(commandBytes, 0, commandBytes.Length);

            //Wait for response
            var startTime = DateTime.Now;
            while (!_gotResponse && ((DateTime.Now - startTime).TotalMilliseconds < 1000))
            {
                Thread.Sleep(20);
            }
        }

        private byte[] CreateInverterCommand(string commandText)
        {
            //Get bytes for command
            byte[] command = Encoding.ASCII.GetBytes(commandText);

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
        private ushort CalculateCrc(byte[] pin)
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

        private void DataErrorReceivedHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            Console.Write(e.EventType);
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
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
    }
}