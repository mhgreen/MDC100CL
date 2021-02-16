using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace MDC100CL
{
    class Program
    {

        static bool loopContinue;
        static bool mdcFound;
        static void Main(string[] args)
        {

            string message;
            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
            loopContinue = true;

            mdcFound = SerialManager.FindAndOpenMDC100();

            if (mdcFound)
            {
                Console.WriteLine("Type QUIT to exit");
            }

            while (loopContinue && mdcFound)
            {
                message = Console.ReadLine();

                if (stringComparer.Equals("quit", message))
                {
                    SerialManager.CloseMDC100Port();
                    loopContinue = false;
                }
                else
                {
                    SerialManager.MDC100Command(message);
                    //string indata = SerialManager.CommandWithReturn(message);
                    //string[] results = indata.Split("\r");
                    //foreach (string result in results)
                    {
                        //Console.WriteLine($"received {result}");
                    }
                }
            }
        }
    }
}