using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;

namespace AuroraGUI
{
    /// <summary>
    /// SpeedWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SpeedWindow
    {
        public SpeedWindow()
        {
            InitializeComponent();
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(SpeedListView.Items[0]);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> dohListStrings = null;
            var bgw = new BackgroundWorker();
            bgw.DoWork += (o, args) =>
            {
                dohListStrings = new WebClient().DownloadString("https://dns.mili.one/DoH.list").Split('\n').ToList();
                if (string.IsNullOrWhiteSpace(dohListStrings[dohListStrings.Count - 1]))
                    dohListStrings.RemoveAt(dohListStrings.Count - 1);
            };
            bgw.RunWorkerCompleted += (o, args) =>
            {
                foreach (var dohUrlString in dohListStrings)
                    SpeedListView.Items.Add(new SpeedList { Server = dohUrlString.Split('/', ':')[3] });
            };
            bgw.RunWorkerAsync();
        }
    }
    public class SpeedList
    {
        public string Server { get; set; }
        public string DelayTime { get; set; }
        public string ASN { get; set; }
    }
}
