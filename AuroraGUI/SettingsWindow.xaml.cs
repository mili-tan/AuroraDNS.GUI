using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using static System.Windows.Forms.Application;
using WinFormMessageBox = System.Windows.Forms.MessageBox;

// ReSharper disable LocalizableElement

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

            if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                RunWithStart.IsChecked = MyTools.GetRunWithStart("AuroraDNS");
            else
                RunWithStart.IsEnabled = false;

            if (File.Exists("black.list"))
                BlackList.IsEnabled = true;
            if (File.Exists("white.list"))
                WhiteList.IsEnabled = true;
        }

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
                DnsSettings.HttpsDnsUrl = DoHUrlText.Text.Trim();
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
            
            File.WriteAllText("config.json", 
                "{\n  " +
                $"\"Listen\" : \"{DnsSettings.ListenIp}\",\n  " +
                $"\"SecondDns\" : \"{DnsSettings.SecondDnsIp}\",\n  " +
                $"\"BlackList\" : {DnsSettings.BlackListEnable.ToString().ToLower()},\n  " +
                $"\"WhiteList\" : {DnsSettings.WhiteListEnable.ToString().ToLower()},\n  " +
                $"\"DebugLog\" : {DnsSettings.DebugLog.ToString().ToLower()},\n  " +
                $"\"EDnsCustomize\" : {DnsSettings.EDnsCustomize.ToString().ToLower()},\n  " +
                $"\"EDnsClientIp\" : \"{DnsSettings.EDnsIp}\",\n  " +
                $"\"ProxyEnable\" : {DnsSettings.ProxyEnable.ToString().ToLower()},\n  " +
                $"\"HttpsDns\" : \"{DnsSettings.HttpsDnsUrl.Trim()}\",\n  " +
                $"\"Proxy\" : \"{ProxyServer.Text + ":" + ProxyServerPort.Text}\" \n" +
                "}");
        }

        private void BlackListButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "list files (*.list)|*.list|txt files (*.txt)|*.txt|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(File.ReadAllText(openFileDialog.FileName)))
                        WinFormMessageBox.Show("Error: 无效的空文件。");
                    else
                    {
                        File.Copy(openFileDialog.FileName, "black.list");
                        WinFormMessageBox.Show("导入成功!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: 无法写入文件 \n\rOriginal error: " + ex.Message);
                }
            }

            if (File.Exists("black.list"))
                BlackList.IsEnabled = true;
        }

        private void WhiteListButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "list files (*.list)|*.list|Hosts file|hosts|txt files (*.txt)|*.txt|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(File.ReadAllText(openFileDialog.FileName)))
                        WinFormMessageBox.Show("Error: 无效的空文件。");
                    else
                    {
                        File.Copy(openFileDialog.FileName, "white.list");
                        WinFormMessageBox.Show("导入成功!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: 无法写入文件 \n\rOriginal error: " + ex.Message);
                }
            }

            if (File.Exists("white.list"))
                WhiteList.IsEnabled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] dohListStrings = null,dnsListStrings = null;
            var bgw = new BackgroundWorker();
            bgw.DoWork += (o, args) =>
            {
                dohListStrings = new WebClient().DownloadString("https://dns.mili.one/DoH.list").Split('\n');
                dnsListStrings = new WebClient().DownloadString("https://dns.mili.one/DNS.list").Split('\n');
            };
            bgw.RunWorkerCompleted += (o, args) =>
            {
                foreach (var dohUrlString in dohListStrings)
                    DoHUrlText.Items.Add(dohUrlString);
                foreach (var dnsAddrString in dnsListStrings)
                    BackupDNS.Items.Add(dnsAddrString);
            };
            bgw.RunWorkerAsync();
        }

        private void RunWithStart_Checked(object sender, RoutedEventArgs e) =>
            MyTools.SetRunWithStart(true, "AuroraDNS", GetType().Assembly.Location);

        private void RunWithStart_Unchecked(object sender, RoutedEventArgs e) =>
            MyTools.SetRunWithStart(false, "AuroraDNS", GetType().Assembly.Location);

        private void RunAsAdmin_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = GetType().Assembly.Location,
                    Verb = "runas"
                };
                try
                {
                    Process.Start(startInfo);
                    Environment.Exit(Environment.ExitCode);
                }
                catch (Exception exception)
                {
                    MyTools.BgwLog(exception.ToString());
                }
            }
        }
    }
}
