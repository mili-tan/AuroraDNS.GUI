using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using Microsoft.Win32;
using MojoUnity;
using static System.AppDomain;

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
                    File.AppendAllText($"{CurrentDomain.SetupInformation.ApplicationBase}Log/{DateTime.Today.Year}{DateTime.Today.Month}{DateTime.Today.Day}.log", log + Environment.NewLine);
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

        public static bool IsNslookupLocDns()
        {
            var p = Process.Start(new ProcessStartInfo("nslookup.exe", "sjtu.edu.cn")
                {UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true});
            p.WaitForExit();
            return p.StandardOutput.ReadToEnd().Contains("127.0.0.1");
        }

        public static void CheckUpdate(string filePath)
        {
            var assets = Json.Parse(new WebClient() { Headers = { ["User-Agent"] = "AuroraDNSC/0.1" } }.DownloadString(
                    "https://api.github.com/repos/mili-tan/AuroraDNS.GUI/releases/latest"))
                .AsObjectGetArray("assets");
            var fileTime = File.GetLastWriteTime(filePath);
            string downloadUrl = assets[0].AsObjectGetString("browser_download_url");
            if (Convert.ToInt32(downloadUrl.Split('/')[7]) >
                Convert.ToInt32((fileTime.Year - 2000).ToString() + fileTime.Month + fileTime.Day))
                Process.Start(downloadUrl);
            else
                MessageBox.Show($"当前AuroraDNS.GUI({Convert.ToInt32((fileTime.Year - 2000).ToString() + fileTime.Month + fileTime.Day)})已是最新版本,无需更新。");
        }

        public static string IsoCountryCodeToFlagEmoji(string country)
        {
            return string.Concat(country.ToUpper().Select(x => char.ConvertFromUtf32(x + 0x1F1A5)));
        }
    }
}
