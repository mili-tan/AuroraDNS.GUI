using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using AuroraGUI.DnsSvr;
using AuroraGUI.Tools;

namespace AuroraGUI
{
    /// <summary>
    /// SpeedWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SpeedWindow
    {
        List<string> ListStrings;
        private bool TypeDNS;

        public SpeedWindow(bool typeDns = false)
        {
            InitializeComponent();
            TypeDNS = typeDns;
            IsEnabled = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var bgWorker = new BackgroundWorker { WorkerReportsProgress = true};
            List<SpeedList> mItems = SpeedListView.Items.Cast<SpeedList>().ToList();
            IsEnabled = false;
            SpeedListView.Items.Clear();

            bgWorker.DoWork += (o, args) =>
            {
                int i = 0;
                foreach (SpeedList item in mItems)
                {
                    double delayTime;
                    if (item.Server.Contains("google.com") && !DnsSettings.ProxyEnable &&
                        IpTools.GeoIpLocal(MainWindow.IntIPAddr.ToString(), true).Contains("CN"))
                    {
                        bgWorker.ReportProgress(i++,
                            new SpeedList
                            {
                                Server = item.Server, Name = item.Name, DelayTime = "PASS",
                                Asn = IpTools.GeoIpLocal(item.Server)
                            });
                        continue;
                    }

                    if (TypeDNS)
                    {
                        delayTime = Ping.MPing(item.Server).Average();
                        if (delayTime == 0)
                            delayTime = Ping.Tcping(item.Server, 53).Average();
                    }
                    else
                        delayTime = Ping.Curl(ListStrings[i].Split('*')[0].Trim(), "auroradns.github.io").Average();

                    bgWorker.ReportProgress(i++,
                        new SpeedList
                        {
                            Server = item.Server, Name = item.Name,
                            DelayTime = delayTime.ToString("0ms"),
                            Asn = IpTools.GeoIpLocal(item.Server)
                        });
                }
            };
            bgWorker.ProgressChanged += (o, args) => { SpeedListView.Items.Add((SpeedList) args.UserState); };
            bgWorker.RunWorkerCompleted += (o, args) => { IsEnabled = true; };

            bgWorker.RunWorkerAsync();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var bgWorker = new BackgroundWorker();
            bgWorker.DoWork += (o, args) =>
            {
                try
                {
                    ListStrings = new WebClient().DownloadString(TypeDNS ? UrlSettings.MDnsList : UrlSettings.MDohList)
                        .Split('\n').ToList();
                }
                catch (Exception exception)
                {
                    MyTools.BgwLog(@"| DownloadString failed : " + exception);
                }

                if (string.IsNullOrWhiteSpace(ListStrings[ListStrings.Count - 1]))
                    ListStrings.RemoveAt(ListStrings.Count - 1);
            };
            bgWorker.RunWorkerCompleted += (o, args) =>
            {
                if (File.Exists($"{MainWindow.SetupBasePath}dns.list") && TypeDNS)
                    foreach (var item in File.ReadAllLines($"{MainWindow.SetupBasePath}dns.list"))
                        ListStrings.Add(item);
                else if (File.Exists($"{MainWindow.SetupBasePath}doh.list") && !TypeDNS)
                    foreach (var item in File.ReadAllLines($"{MainWindow.SetupBasePath}doh.list"))
                        ListStrings.Add(item);

                foreach (var item in ListStrings)
                {
                    SpeedListView.Items.Add(new SpeedList
                    {
                        Server = TypeDNS ? item.Split('*')[0].Trim() : item.Split('*')[0].Trim().Split('/', ':')[3],
                        Name = item.Contains('*') ? item.Split('*')[1].Trim() : ""
                    });
                }

                IsEnabled = true;
            };
            bgWorker.RunWorkerAsync();
        }
    }

    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public class SpeedList
    {
        public string Server { get; set; }
        public string Name { get; set; }
        public string DelayTime { get; set; }
        public string Asn { get; set; }
    }
}
