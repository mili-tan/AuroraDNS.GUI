using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Effects;
using AuroraGUI.DnsSvr;
using AuroraGUI.Tools;
using MaterialDesignThemes.Wpf;

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

            Grid.Effect = new BlurEffect() { Radius = 5, RenderingBias = RenderingBias.Performance };
            TypeDNS = typeDns;
            StratButton.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Hidden;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.Visibility = Visibility.Visible;
            var bgWorker = new BackgroundWorker { WorkerReportsProgress = true};
            List<SpeedList> mItems = SpeedListView.Items.Cast<SpeedList>().ToList();
            StratButton.IsEnabled = false;
            SpeedListView.Items.Clear();

            bgWorker.DoWork += (o, args) =>
            {
                int i = 1;
                foreach (SpeedList item in mItems)
                {
                    double delayTime;
                    if (item.Server.Contains("google.com") && !DnsSettings.ProxyEnable &&
                        IpTools.GeoIpLocal(MainWindow.IntIPAddr.ToString(), true).Contains("CN"))
                    {
                        bgWorker.ReportProgress(i++,
                            new SpeedList
                            {
                                Server = item.Server, Name = item.Name, DelayTime = 0,
                                Asn = IpTools.GeoIpLocal(item.Server).Trim()
                            });
                        continue;
                    }

                    try
                    {
                        if (TypeDNS)
                        {
                            //delayTime = Ping.MPing(item.Server).Average();
                            //if (delayTime == 0)
                            //    delayTime = Ping.Tcping(item.Server, 53).Average();
                            //var dnsDelayTime = Ping.DnsTest(item.Server).Average();
                            //if (dnsDelayTime > delayTime) delayTime = dnsDelayTime;
                            delayTime = Math.Round(Ping.DnsTest(item.Server).Average(), 2);
                        }
                        else
                            delayTime = Math.Round(Ping.Tcping(item.Server, 443).Average(), 2);

                        bgWorker.ReportProgress(i++,
                            new SpeedList
                            {
                                Server = item.Server,
                                Name = item.Name,
                                DelayTime = delayTime,
                                Asn = IpTools.GeoIpLocal(item.Server).Trim()
                            });
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                }
            };
            bgWorker.ProgressChanged += (o, args) =>
            {
                SpeedListView.Items.Add((SpeedList) args.UserState);
                ProgressBar.Value = args.ProgressPercentage;
            };
            bgWorker.RunWorkerCompleted += (o, args) =>
            {
                StratButton.IsEnabled = true;
                ProgressBar.Value = 0;
                ProgressBar.Visibility = Visibility.Hidden;
            };

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
                    if (string.IsNullOrWhiteSpace(ListStrings[ListStrings.Count - 1]))
                        ListStrings.RemoveAt(ListStrings.Count - 1);
                    args.Result = true;
                }
                catch (Exception exception)
                {
                    MyTools.BackgroundLog(@"| Download String failed : " + exception);
                    args.Result = false;
                }
            };
            bgWorker.RunWorkerCompleted += (o, args) =>
            {
                if (!(bool)args.Result)
                {
                    Grid.Effect = null;
                    Snackbar.IsActive = true;
                    Snackbar.Message = new SnackbarMessage() {Content = "获取列表内容失败，请检查互联网连接。"};
                    return;
                }

                try
                {
                    if (File.Exists($"{MainWindow.SetupBasePath}dns.list") && TypeDNS)
                        foreach (var item in File.ReadAllLines($"{MainWindow.SetupBasePath}dns.list"))
                            ListStrings.Add(item);
                    else if (File.Exists($"{MainWindow.SetupBasePath}doh.list") && !TypeDNS)
                        foreach (var item in File.ReadAllLines($"{MainWindow.SetupBasePath}doh.list"))
                            ListStrings.Add(item);

                    if (ListStrings != null && ListStrings.Count != 0)
                    {
                        foreach (var item in ListStrings)
                        {
                            SpeedListView.Items.Add(new SpeedList
                            {
                                Server = TypeDNS ? item.Split('*', ',')[0].Trim() : new Uri(item.Split('*', ',')[0].Trim()).Host,
                                Name = item.Contains('*') || item.Contains(',') ? item.Split('*', ',')[1].Trim() : ""
                            });
                        }
                    }

                    StratButton.IsEnabled = true;
                    ProgressBar.Maximum = SpeedListView.Items.Count;
                    Grid.Effect = null;
                }
                catch (Exception exception)
                {
                    MyTools.BackgroundLog(@"| Read String failed : " + exception);
                    MessageBox.Show($"Error: 尝试载入列表失败{Environment.NewLine}Original error: {exception}");
                }
            };
            bgWorker.RunWorkerAsync();
        }

        private void SpeedListView_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader header)
            {
                //获得点击的列
                GridViewColumn clickedColumn = header.Column;
                if (clickedColumn != null)
                {
                    string bindingProperty = (clickedColumn.DisplayMemberBinding as Binding).Path.Path;
                    SortDescriptionCollection sortDescription = SpeedListView.Items.SortDescriptions;

                    ListSortDirection sortDirection = ListSortDirection.Ascending;
                    if (sortDescription.Count > 0)
                    {
                        SortDescription sort = sortDescription[0];
                        sortDirection = (ListSortDirection)(((int)sort.Direction + 1) % 2);
                        sortDescription.Clear();
                    }
                    sortDescription.Add(new SortDescription(bindingProperty, sortDirection));
                }
            }
        }
    }

    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public class SpeedList
    {
        public string Server { get; set; }
        public string Name { get; set; }
        public object DelayTime { get; set; }
        public string Asn { get; set; }
    }
}
