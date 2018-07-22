using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Win32;

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

        public static void SetRunWithStart(bool started, string name, string path)
        {
            RegistryKey Reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            if (started)
            {
                Reg.SetValue(name,path);
            }
            else
            {
                Reg.DeleteValue(name);
            }
        }

        public static bool GetRunWithStart(string name)
        {
            RegistryKey Reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            try
            {
                return !string.IsNullOrWhiteSpace(Reg.GetValue(name).ToString());
            }
            catch
            {
                return false;
            }
        }

    }
}
