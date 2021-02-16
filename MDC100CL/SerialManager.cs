using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.IO.Ports;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace MDC100CL
{
	class SerialManager
	{
		private static SerialPort mdcSerialPort;
		private static readonly List<FTDIPort> ports = new List<FTDIPort>();
		private static readonly List<FTDIPort> mdc100s = new List<FTDIPort>();
        private static IDictionary<string, MDC100CommandParameters> commandParameters =
            new Dictionary<string, MDC100CommandParameters>();
        private static Dictionary<string, Func<int, int, string>> executeCommand =
            new Dictionary<string, Func<int, int, string>>();
        private static readonly FTDI _ftdi = new FTDI();
        private static readonly EventWaitHandle waitOnRead =
            new EventWaitHandle(false, EventResetMode.AutoReset);
        private static string commandResult;
        private static readonly Regex regExDevice = new Regex(@"(?!^@\d{3,})^@\d{1,2}|^@\%|^@\~|^@\#", RegexOptions.Compiled);
        private static readonly Regex regExCommand = new Regex(@"[A-Z\.\,\~\%\-\+\[\]\!]+", RegexOptions.Compiled);
        private static readonly Regex regExArgument = new Regex(@"\d+$|\*$", RegexOptions.Compiled);
        private static readonly Regex regExResultBase = new Regex("(^[A-Z][A-Z]?){1}", RegexOptions.Compiled);
        private static readonly Regex regExResultArgument = new Regex(@"(\d+$|\*|\+|\-|\,|\.|D$|A$)", RegexOptions.Compiled);

        public static bool FindAndOpenMDC100()
		{
			mdcSerialPort = new SerialPort
			{
				BaudRate = 38400,
				DataBits = 8,
				StopBits = StopBits.One,
				Parity = Parity.None,
				Handshake = Handshake.XOnXOff,
				NewLine = "\r",
				ReadTimeout = 40,
				WriteTimeout = 40,
				ReceivedBytesThreshold = 2
			};
            mdcSerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            uint ftdiCount = 0;

            FTDI.FT_STATUS status = _ftdi.GetNumberOfDevices(ref ftdiCount);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                Console.WriteLine("log.Warn: Unable to access FTDI");
            }

            FTDI.FT_DEVICE_INFO_NODE[] list = new FTDI.FT_DEVICE_INFO_NODE[ftdiCount];
            status = _ftdi.GetDeviceList(list);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                Console.WriteLine("log.Warn: Unable to access FTDI");
            }

            foreach (FTDI.FT_DEVICE_INFO_NODE node in list)
            {
                if ((status = _ftdi.OpenByLocation(node.LocId)) == FTDI.FT_STATUS.FT_OK)
                {
                    try
                    {
                        _ftdi.GetCOMPort(out string comport);

                        if (comport != null && comport.Length > 0)
                        {
                            ports.Add(new FTDIPort(comport, node.Description.ToString(), node.SerialNumber.ToString()));
                        }
                    }
                    finally
                    {
                        _ftdi.Close();
                    }
                }
            }

            foreach (FTDIPort port in ports)
            {
                if (port.NodeDescription.Contains("MDC100"))
                {
                    mdc100s.Add(port);
                }
            }

            Console.WriteLine($"------------------------------");
            Console.WriteLine($"Total MDC100 count: {mdc100s.Count}");
            Console.WriteLine($"------------------------------");
            Console.WriteLine($"");

            if (mdc100s.Count > 0)
            {
                foreach (FTDIPort mdc100 in mdc100s)
                {
                    Console.WriteLine($"--- MDC100 Devices ---");
                    Console.WriteLine($"comport: {mdc100.NodeComportName}");
                    Console.WriteLine($"description: {mdc100.NodeDescription}");
                    Console.WriteLine($"serial number: {mdc100.NodeSerialNumber}");
                    Console.WriteLine($"--- MDC100 Devices ---");
                    Console.WriteLine($"");
                }

                mdcSerialPort.PortName = mdc100s.First().NodeComportName;
                mdcSerialPort.Open();
                return true;
            }
            else
            {
                Console.WriteLine($"MDC100 not found");
                return false;
            }
        }

        private static void DataReceivedHandler(
            object sender,
            SerialDataReceivedEventArgs args)
        {
            // The command @0V* produces the longest output and needs 20 ms for all results to come back
            // Simple commands only need 4 ms for results
            commandResult = null;
            char[] charBuffer = new char[75];
            int charBufferPosition = 0;
            int iterationNumber = 0;
            int bytesToRead;
            do
            {
                Thread.Sleep(5);
                iterationNumber++;
                bytesToRead = mdcSerialPort.BytesToRead;
                // Console.WriteLine($"<DataReceivedHandler> Bytes available, iteration {iterationNumber}: {bytesToRead}");
                try
                {
                    mdcSerialPort.Read(charBuffer, charBufferPosition, bytesToRead);
                }
                catch (TimeoutException e)
                {
                    Console.WriteLine($"Timeout occured while reading from the MDC100: {e}");
                    throw;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception occurred while reading from the MDC100: {e}");
                    throw;
                }
                string partialResult = new string(charBuffer, charBufferPosition, bytesToRead);
                charBufferPosition += (bytesToRead - 1);
                commandResult += partialResult;
                Thread.Sleep(4);
                // Console.WriteLine($"<DataReceivedHandler> Bytes available after Read(), iteration {iterationNumber}: {mdcSerialPort.BytesToRead}");
            } while (mdcSerialPort.BytesToRead > 0);
            Console.WriteLine("<DataReceivedHandler> setting waitOnRead ");
            waitOnRead.Set();
        }

        public static void CloseMDC100Port()
        {
            mdcSerialPort.Close();
        }

		public static void MDC100Command(string command)
        {
            bool receivedSignal;
            mdcSerialPort.ReadTimeout = 20;
            mdcSerialPort.ReceivedBytesThreshold = 2;
            MatchCollection deviceMatches = regExDevice.Matches(command);
            MatchCollection commandMatches = regExCommand.Matches(command);
            MatchCollection argumentMatches = regExArgument.Matches(command);
            foreach (Match _device in deviceMatches)
                Console.WriteLine($"device: {_device}");
            foreach (Match _command in commandMatches)
                Console.WriteLine($"command: {_command}");
            foreach (Match _argument in argumentMatches)
                Console.WriteLine($"argument: {_argument}");
            try
            {
                mdcSerialPort.WriteLine(command);
            }
            catch (TimeoutException e)
            {
                Console.WriteLine($"Timeout occured while writing to the MDC100: {e}");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception occurred while writing to the MDC100: {e}");
                throw;
            }
            if (true) //mdcSerialPort.BytesToRead > 0
            {
                Console.WriteLine($"---- <MDC1000Command> waiting for results ----");
                receivedSignal = waitOnRead.WaitOne(150);
                if (receivedSignal) // received a response from the MDC100
                    Console.WriteLine($"---- <MDC1000Command> done waiting with signal ----");
                    if (commandResult != null)
                    {
                        string[] results = commandResult.Split('\r', StringSplitOptions.RemoveEmptyEntries);
                        foreach (string result in results)
                        {
                            Console.WriteLine(result);
                            MatchCollection resultBaseMatches = regExResultBase.Matches(result);
                            MatchCollection resultArgumentMatches = regExResultArgument.Matches(result);
                            foreach (Match _resultBase in resultBaseMatches)
                                Console.WriteLine($"resultBase: {_resultBase}");
                            foreach (Match _resultArgument in resultArgumentMatches)
                                Console.WriteLine($"resultArgument: {_resultArgument}");
                    }
                    commandResult = null;
                    }
                else
                    Console.WriteLine($"---- <MDC1000Command> done waiting with timeout ----");
            }

        }
	}
}
