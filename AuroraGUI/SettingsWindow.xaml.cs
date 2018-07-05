using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        }

        private void Log_OnChecked(object sender, RoutedEventArgs e) => DnsSettings.DebugLog = true;
        private void Log_OnUnchecked(object sender, RoutedEventArgs e) => DnsSettings.DebugLog = false;

        private void EDNSCustomize_OnChecked(object sender, RoutedEventArgs e)
        {
            DnsSettings.EDnsCustomize = true;
            EDNSClientIP.IsEnabled = true;
        }

        private void EDNSCustomize_OnUnchecked(object sender, RoutedEventArgs e)
        {
            DnsSettings.EDnsCustomize = false;
            EDNSClientIP.IsEnabled = false;
        }

        private void BlackList_OnChecked(object sender, RoutedEventArgs e) => DnsSettings.BlackListEnable = true;
        private void BlackList_OnUnchecked(object sender, RoutedEventArgs e) => DnsSettings.BlackListEnable = false;

        private void WhiteList_OnChecked(object sender, RoutedEventArgs e) => DnsSettings.WhiteListEnable = true;
        private void WhiteList_OnUnchecked(object sender, RoutedEventArgs e) => DnsSettings.WhiteListEnable = false;

        private void DoHUrlText_OnTextChanged(object sender, TextChangedEventArgs e) => DnsSettings.HttpsDnsUrl = DoHUrlText.Text;

        private void Proxy_OnChecked(object sender, RoutedEventArgs e)
        {
            DnsSettings.ProxyEnable = true;
            ProxyServer.IsEnabled = true;
            ProxyServerPort.IsEnabled = true;
        }

        private void Proxy_OnUnchecked(object sender, RoutedEventArgs e)
        {
            DnsSettings.ProxyEnable = false;
            ProxyServer.IsEnabled = false;
            ProxyServerPort.IsEnabled = false;
        }

        private void ProxyServer_OnTextChanged(object sender, TextChangedEventArgs e) =>
            DnsSettings.WProxy = new WebProxy(ProxyServer.Text);

        private void ProxyServerPort_OnTextChanged(object sender, TextChangedEventArgs e) =>
            DnsSettings.WProxy = new WebProxy(ProxyServer.Text + ":" + ProxyServerPort.Text);

        private void BackupDNS_OnTextChanged(object sender, TextChangedEventArgs e) =>
            DnsSettings.SecondDnsIp = IPAddress.Parse(BackupDNS.Text);

        private void EDNSClientIP_OnTextChanged(object sender, TextChangedEventArgs e) =>
            DnsSettings.EDnsIp = IPAddress.Parse(EDNSClientIP.Text);

        private void ListenIP_OnTextChanged(object sender, TextChangedEventArgs e) =>
            DnsSettings.ListenIp = IPAddress.Parse(ListenIP.Text);

        private void TextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e) =>
            ListenIP.IsEnabled = true;

    }
}
