using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using static System.Windows.Forms.Application;
using WinFormMessageBox = System.Windows.Forms.MessageBox;

namespace AuroraGUI
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();
            EnableVisualStyles();

            Log.IsChecked = DnsSettings.DebugLog;
            EDNSCustomize.IsChecked = DnsSettings.EDnsCustomize;
            WhiteList.IsChecked = DnsSettings.WhiteListEnable;
            BlackList.IsChecked = DnsSettings.BlackListEnable;
            Proxy.IsChecked = DnsSettings.ProxyEnable;

            DoHUrlText.Text = DnsSettings.HttpsDnsUrl;
            BackupDNS.Text =  DnsSettings.SecondDnsIp.ToString();
            EDNSClientIP.Text = DnsSettings.EDnsIp.ToString();
            ListenIP.Text = DnsSettings.ListenIp.ToString();

            ProxyServer.Text = DnsSettings.WProxy.Address.Host;
            ProxyServerPort.Text = DnsSettings.WProxy.Address.Port.ToString();
        }

        private void EDNSCustomize_OnChecked(object sender, RoutedEventArgs e) => EDNSClientIP.IsEnabled = true;
        private void EDNSCustomize_OnUnchecked(object sender, RoutedEventArgs e) => EDNSClientIP.IsEnabled = false;

        private void Proxy_OnChecked(object sender, RoutedEventArgs e)
        {
            ProxyServer.IsEnabled = true;
            ProxyServerPort.IsEnabled = true;
        }

        private void Proxy_OnUnchecked(object sender, RoutedEventArgs e)
        {
            ProxyServer.IsEnabled = false;
            ProxyServerPort.IsEnabled = false;
        }

        private void TextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e) =>
            ListenIP.IsEnabled = true;

        private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
        {
            DnsSettings.DebugLog = Convert.ToBoolean(Log.IsChecked);
            DnsSettings.EDnsCustomize = Convert.ToBoolean(EDNSCustomize.IsChecked);
            DnsSettings.BlackListEnable = Convert.ToBoolean(BlackList.IsChecked);
            DnsSettings.WhiteListEnable = Convert.ToBoolean(WhiteList.IsChecked);
            DnsSettings.ProxyEnable = Convert.ToBoolean(Proxy.IsChecked);

            if (!string.IsNullOrWhiteSpace(DoHUrlText.Text) &&
                !string.IsNullOrWhiteSpace(BackupDNS.Text) &&
                !string.IsNullOrWhiteSpace(EDNSClientIP.Text) &&
                !string.IsNullOrWhiteSpace(ListenIP.Text))
            {
                DnsSettings.HttpsDnsUrl = DoHUrlText.Text;
                DnsSettings.SecondDnsIp = IPAddress.Parse(BackupDNS.Text);
                DnsSettings.EDnsIp = IPAddress.Parse(EDNSClientIP.Text);
                DnsSettings.ListenIp = IPAddress.Parse(ListenIP.Text);

                if (Proxy.IsChecked == true)
                    DnsSettings.WProxy = new WebProxy(ProxyServer.Text + ":" + ProxyServerPort.Text);
                else
                    DnsSettings.WProxy = new WebProxy("127.0.0.1:80");
                
            }
            else
                WinFormMessageBox.Show(@"不应为空,请填写完全");
            
        }

    }
}
