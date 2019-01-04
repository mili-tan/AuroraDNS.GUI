using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public SpeedWindow(bool typeDNS = false)
        {
            InitializeComponent();
            TypeDNS = typeDNS;
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
                    if (item.Server.Contains("google.com") &&
                        !DnsSettings.ProxyEnable && TimeZoneInfo.Local.Id.Contains("China Standard Time"))
                    {
                        bgWorker.ReportProgress(i++,
                            new SpeedList
                                {Server = item.Server, DelayTime = "PASS", Asn = IpTools.GeoIpLocal(item.Server)});
                        continue;
                    }

                    if (TypeDNS)
                    {
                        delayTime = Ping.MPing(item.Server).Average();
                        if (delayTime == 0)
                            delayTime = Ping.Tcping(item.Server, 53).Average();
                    }
                    else
                        delayTime = Ping.Curl(ListStrings[i], item.Server).Average();

                    bgWorker.ReportProgress(i++,
                        new SpeedList
                        {
                            Server = item.Server, DelayTime = delayTime.ToString("0ms"),
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
                    ListStrings = new WebClient().DownloadString(TypeDNS ? "https://cdn.jsdelivr.net/gh/AuroraDNS/AuroraDNS.github.io/DNS.list"
                        : "https://cdn.jsdelivr.net/gh/AuroraDNS/AuroraDNS.github.io/DoH.list").Split('\n').ToList();
                }
                catch (Exception exception)
                {
                    MyTools.BgwLog(@"| Download list failed : " + exception);
                }

                if (string.IsNullOrWhiteSpace(ListStrings[ListStrings.Count - 1]))
                    ListStrings.RemoveAt(ListStrings.Count - 1);
            };
            bgWorker.RunWorkerCompleted += (o, args) =>
            {
                foreach (var item in ListStrings)
                    SpeedListView.Items.Add(!TypeDNS
                        ? new SpeedList {Server = item.Split('/', ':')[3]}
                        : new SpeedList {Server = item});
                IsEnabled = true;
            };
            bgWorker.RunWorkerAsync();
        }
    }

    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public class SpeedList
    {
        public string Server { get; set; }
        public string DelayTime { get; set; }
        public string Asn { get; set; }
    }
}
