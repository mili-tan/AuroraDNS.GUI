using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using AuroraGUI.DnsSvr;
using Microsoft.Win32;
using MojoUnity;

namespace AuroraGUI.Tools
{
    static class MyTools
    {
        public static void BackgroundLog(string log)
        {
            using (var worker = new BackgroundWorker())
            {
                worker.DoWork += (o, ea) =>
                {
                    var fileName =
                        $"{MainWindow.SetupBasePath}Log/{DateTime.Today.Year}{DateTime.Today.Month:00}{DateTime.Today.Day:00}.log";
                    try
                    {
                        File.AppendAllLines(fileName, new[] {log});
                    }
                    catch (Exception e)
                    {
                        if (!Directory.Exists($"{MainWindow.SetupBasePath}Log"))
                            Directory.CreateDirectory($"{MainWindow.SetupBasePath}Log");
                        if (!File.Exists(fileName)) File.Create(fileName).Close();
                        Thread.Sleep(500);
                        File.AppendAllLines(fileName, e is IOException ? new[] {log} : new[] {e.Message, log});
                        Thread.Sleep(100);
                    }
                };

                worker.RunWorkerAsync();
            }
        }

        public static void BackgroundWriteCache(CacheItem item, int ttl = 600)
        {
            using (var worker = new BackgroundWorker())
            {
                worker.DoWork += (o, ea) =>
                {
                    if (!MemoryCache.Default.Contains(item.Key))
                        MemoryCache.Default.Add(item,
                            new CacheItemPolicy {AbsoluteExpiration = DateTimeOffset.Now + TimeSpan.FromSeconds(ttl)});
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
            RegistryKey reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            if (started)
                reg.SetValue(name, path);
            else
                reg.DeleteValue(name);
        }

        public static bool GetRunWithStart(string name)
        {
            RegistryKey reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            try
            {
                return !string.IsNullOrWhiteSpace(reg.GetValue(name).ToString());
            }
            catch
            {
                return false;
            }
        }

        public static bool IsNslookupLocDns()
        {
            var process = Process.Start(new ProcessStartInfo("nslookup.exe", new Uri(DnsSettings.HttpsDnsUrl).Host)
                {UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true});
            process.WaitForExit();
            return process.StandardOutput.ReadToEnd().Contains("127.0.0.1");
        }

        public static void CheckUpdate(string filePath)
        {
            using (var worker = new BackgroundWorker())
            {
                worker.DoWork += (o, ea) =>
                {
                    try
                    {
                        var jsonStr = Regex.Replace(Encoding.UTF8.GetString(
                                new WebClient {Headers = {["User-Agent"] = "AuroraDNSC/0.1"}}.DownloadData(
                                    "https://api.github.com/repos/mili-tan/AuroraDNS.GUI/releases/latest")),
                            @"[\u4e00-\u9fa5|\u3002|\uff0c]", "");
                        var assets = Json.Parse(jsonStr).AsObjectGetArray("assets");
                        var fileTime = File.GetLastWriteTime(filePath);
                        string downloadUrl = assets[0].AsObjectGetString("browser_download_url");

                        if (Convert.ToInt32(downloadUrl.Split('/')[7]) >
                            Convert.ToInt32(fileTime.Year - 2000 + fileTime.Month.ToString("00") +
                                            fileTime.Day.ToString("00")))
                            Process.Start(downloadUrl);
                        else
                            MessageBox.Show(
                                $"当前AuroraDNS.GUI({fileTime.Year - 2000}{fileTime.Month:00}{fileTime.Day:00})已是最新版本,无需更新。");
                    }
                    catch (Exception e)
                    {
                        BackgroundLog(@"| Download list failed : " + e);
                    }
                };

                worker.RunWorkerAsync();
            }
        }

        // ReSharper disable once UnusedMember.Global
        public static string IsoCountryCodeToFlagEmoji(string country)
        {
            return string.Concat(country.ToUpper().Select(x => char.ConvertFromUtf32(x + 0x1F1A5)));
        }

        public static bool IsBadSoftExist()
        {
            string[] badSoftProcess =
            {
                "360Safe", "ZhuDongFangYu", "2345SoftSvc", "2345RTProtect",
                "QQPCTray", "QQPCRTP", "kxetray", "kxescore"
            };
            int offenseCount = badSoftProcess.Sum(processName => Process.GetProcessesByName(processName).Length);
            return offenseCount != 0;
        }
    }
}
