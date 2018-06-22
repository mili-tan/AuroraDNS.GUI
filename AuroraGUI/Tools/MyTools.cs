using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace AuroraGUI
{
    static class MyTools
    {
        public static void BgwLog(string log)
        {
            using (BackgroundWorker worker = new BackgroundWorker())
            {
                worker.DoWork += (o, ea) =>
                {
                    if (!Directory.Exists("Log"))
                    {
                        Directory.CreateDirectory("Log");
                    }
                    File.AppendAllText($"./Log/{DateTime.Today.Year}{DateTime.Today.Month}{DateTime.Today.Day}.log", log + Environment.NewLine);
                };

                worker.RunWorkerAsync();
            }
        }

        public static bool PortIsUse(int port)
        {
            IPEndPoint[] ipEndPointsTcp = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            IPEndPoint[] ipEndPointsUdp = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();

            return ipEndPointsTcp.Any(endPoint => endPoint.Port == port)
                   || ipEndPointsUdp.Any(endPoint => endPoint.Port == port);
        }

    }
}
