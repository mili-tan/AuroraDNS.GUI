using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace AuroraGUI
{
    /// <summary>
    /// SpeedWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SpeedWindow
    {
        private bool TypeDNS;
        public SpeedWindow(bool typeDNS = false)
        {
            InitializeComponent();
            TypeDNS = typeDNS;
            IsEnabled = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var bgw = new BackgroundWorker();
            bgw.DoWork += (o, args) =>
            {
                foreach (SpeedList item in SpeedListView.Items)
                {
                    Debug.WriteLine(item.Server);
                }
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> mListStrings = null;
            var bgw = new BackgroundWorker();
            bgw.DoWork += (o, args) =>
            {
                mListStrings = new WebClient().DownloadString(TypeDNS ? "https://dns.mili.one/DNS.list" 
                    : "https://dns.mili.one/DoH.list").Split('\n').ToList();
                if (string.IsNullOrWhiteSpace(mListStrings[mListStrings.Count - 1]))
                    mListStrings.RemoveAt(mListStrings.Count - 1);
            };
            bgw.RunWorkerCompleted += (o, args) =>
            {
                foreach (var item in mListStrings)
                    SpeedListView.Items.Add(!TypeDNS
                        ? new SpeedList {Server = item.Split('/', ':')[3]}
                        : new SpeedList {Server = item});
                IsEnabled = true;
            };
            bgw.RunWorkerAsync();
        }
    }
    public class SpeedList
    {
        public string Server { get; set; }
        public string DelayTime { get; set; }
        public string ASN { get; set; }

        public static explicit operator SpeedList(ItemCollection v)
        {
            throw new NotImplementedException();
        }
    }
}
