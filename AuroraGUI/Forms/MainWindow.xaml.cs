using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using ARSoft.Tools.Net.Dns;
using AuroraGUI.DnsSvr;
using AuroraGUI.Fx;
using AuroraGUI.Tools;
using MaterialDesignThemes.Wpf;
using static System.AppDomain;
using WinFormMenuItem = System.Windows.Forms.MenuItem;
using WinFormContextMenu = System.Windows.Forms.ContextMenu;
using MessageBox = System.Windows.MessageBox;

// ReSharper disable NotAccessedField.Local

namespace AuroraGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public static string SetupBasePath = CurrentDomain.SetupInformation.ApplicationBase;
        public static IPAddress IntIPAddr = IPAddress.Any;
        public static IPAddress LocIPAddr = IPAddress.Any;
        public static NotifyIcon NotifyIcon;
        private static DnsServer MDnsServer;
        private Task MDnsSvrTask = new Task(() => MDnsServer.Start());

        public MainWindow()
        {
            InitializeComponent();

            WindowStyle = WindowStyle.SingleBorderWindow;
            Grid.Effect = new BlurEffect { Radius = 5, RenderingBias = RenderingBias.Performance };

            if (TimeZoneInfo.Local.Id.Contains("China Standard Time") && RegionInfo.CurrentRegion.GeoId == 45) 
            {
                //Mainland China PRC
                DnsSettings.SecondDnsIp = IPAddress.Parse("119.29.29.29");
                DnsSettings.HttpsDnsUrl = "https://neatdns.ustclug.org/resolve";
                UrlSettings.MDnsList = "https://cdn.jsdelivr.net/gh/mili-tan/AuroraDNS.GUI/List/L10N/DNS-CN.list";
                UrlSettings.WhatMyIpApi = "https://myip.ustclug.org/";
            }
            else if (TimeZoneInfo.Local.Id.Contains("Taipei Standard Time") && RegionInfo.CurrentRegion.GeoId == 237) 
            {
                //Taiwan ROC
                DnsSettings.SecondDnsIp = IPAddress.Parse("101.101.101.101");
                DnsSettings.HttpsDnsUrl = "https://dns.twnic.tw/dns-query";
                UrlSettings.MDnsList = "https://cdn.jsdelivr.net/gh/mili-tan/AuroraDNS.GUI/List/L10N/DNS-TW.list";
            }
            else if (RegionInfo.CurrentRegion.GeoId == 104)
                //HongKong SAR
                UrlSettings.MDnsList = "https://cdn.jsdelivr.net/gh/mili-tan/AuroraDNS.GUI/List/L10N/DNS-HK.list";

            if (!File.Exists($"{SetupBasePath}config.json"))
            {
                if (MyTools.IsBadSoftExist())
                    MessageBox.Show("Tips: AuroraDNS 强烈不建议您使用国产安全软件产品！");
                if (!MyTools.IsNslookupLocDns())
                {
                    var msgResult =
                        MessageBox.Show(
                            "Question: 初次启动，是否要将您的系统默认 DNS 服务器设为 AuroraDNS?"
                            , "Question", MessageBoxButton.OKCancel);
                    if (msgResult == MessageBoxResult.OK) IsSysDns_OnClick(null, null);
                }
            }

            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\AuroraDNS.UrlReged"))
            {
                try
                {
                    UrlReg.Reg("doh");
                    UrlReg.Reg("dns-over-https");
                    UrlReg.Reg("aurora-doh-list");

                    File.Create(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                "\\AuroraDNS.UrlReged");
                    File.SetAttributes(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                        "\\AuroraDNS.UrlReged", FileAttributes.Hidden);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            try
            {
                if (File.Exists($"{SetupBasePath}url.json"))
                    UrlSettings.ReadConfig($"{SetupBasePath}url.json");
                if (File.Exists($"{SetupBasePath}config.json"))
                    DnsSettings.ReadConfig($"{SetupBasePath}config.json");
                if (DnsSettings.BlackListEnable && File.Exists($"{SetupBasePath}black.list"))
                    DnsSettings.ReadBlackList($"{SetupBasePath}black.list");
                if (DnsSettings.WhiteListEnable && File.Exists($"{SetupBasePath}white.list"))
                    DnsSettings.ReadWhiteList($"{SetupBasePath}white.list");
                if (DnsSettings.WhiteListEnable && File.Exists($"{SetupBasePath}rewrite.list"))
                    DnsSettings.ReadWhiteList($"{SetupBasePath}rewrite.list");
                if (DnsSettings.ChinaListEnable && File.Exists("china.list"))
                    DnsSettings.ReadChinaList(SetupBasePath + "china.list");
            }
            catch (UnauthorizedAccessException e)
            {
                MessageBoxResult msgResult =
                    MessageBox.Show(
                        "Error: 尝试读取配置文件权限不足或IO安全故障，点击确定现在尝试以管理员权限启动。点击取消中止程序运行。" +
                        $"{Environment.NewLine}Original error: {e}", "错误", MessageBoxButton.OKCancel);
                if (msgResult == MessageBoxResult.OK) RunAsAdmin();
                else Close();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error: 尝试读取配置文件错误{Environment.NewLine}Original error: {e}");
            }
            
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
            if (DnsSettings.AllowSelfSignedCert)
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => true;

//            switch (0.0)
//            {
//                case 1:
//                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
//                    break;
//                case 1.1:
//                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
//                    break;
//                case 1.2:
//                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
//                    break;
//                default:
//                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
//                    break;
//            }

            MDnsServer = new DnsServer(DnsSettings.ListenIp, 10, 10);
            MDnsServer.QueryReceived += QueryResolve.ServerOnQueryReceived;
            //MDnsSvrWorker.DoWork += (sender, args) => MDnsServer.Start();
            //MDnsSvrWorker.Disposed += (sender, args) => MDnsServer.Stop();

            using (BackgroundWorker worker = new BackgroundWorker())
            {
                worker.DoWork += (sender, args) =>
                {
                    LocIPAddr = IPAddress.Parse(IpTools.GetLocIp());
                    if (!(Equals(DnsSettings.EDnsIp, IPAddress.Any) && DnsSettings.EDnsCustomize))
                    {
                        IntIPAddr = IPAddress.Parse(IpTools.GetIntIp());
                        var local = IpTools.GeoIpLocal(IntIPAddr.ToString());
                        Dispatcher?.Invoke(() => { TitleTextItem.Header = $"{IntIPAddr}{Environment.NewLine}{local}"; });
                    }

                    try
                    {
                        if (DnsSettings.WhiteListEnable && File.Exists($"{SetupBasePath}white.sub.list"))
                            DnsSettings.ReadWhiteListSubscribe($"{SetupBasePath}white.sub.list");
                        if (DnsSettings.WhiteListEnable && File.Exists($"{SetupBasePath}rewrite.sub.list"))
                            DnsSettings.ReadWhiteListSubscribe($"{SetupBasePath}rewrite.sub.list");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Error: 尝试下载订阅列表失败{Environment.NewLine}Original error: {e}");
                    }

                    MemoryCache.Default.Trim(100);
                };
                worker.RunWorkerAsync();
            }

            NotifyIcon = new NotifyIcon()
            {
                Text = @"AuroraDNS", Visible = false,
                Icon = Properties.Resources.AuroraWhite
            };
            WinFormMenuItem showItem = new WinFormMenuItem("最小化 / 恢复", MinimizedNormal);
            WinFormMenuItem restartItem = new WinFormMenuItem("重新启动", (sender, args) =>
            {
                if (!MDnsSvrTask.IsCompleted)
                    MDnsServer.Stop();
                Process.Start(new ProcessStartInfo {FileName = GetType().Assembly.Location});
                Environment.Exit(Environment.ExitCode);
            });
            WinFormMenuItem notepadLogItem = new WinFormMenuItem("查阅日志", (sender, args) =>
            {
                if (File.Exists(
                    $"{SetupBasePath}Log/{DateTime.Today.Year}{DateTime.Today.Month:00}{DateTime.Today.Day:00}.log"))
                    Process.Start(new ProcessStartInfo(
                        $"{SetupBasePath}Log/{DateTime.Today.Year}{DateTime.Today.Month:00}{DateTime.Today.Day:00}.log"));
                else
                    MessageBox.Show("找不到当前日志文件，或当前未产生日志文件。");
            });
            WinFormMenuItem abootItem = new WinFormMenuItem("关于…", (sender, args) => new AboutWindow().Show());
            WinFormMenuItem updateItem = new WinFormMenuItem("检查更新…", (sender, args) => MyTools.CheckUpdate(GetType().Assembly.Location));
            WinFormMenuItem settingsItem = new WinFormMenuItem("设置…", (sender, args) => new SettingsWindow().Show());
            WinFormMenuItem exitItem = new WinFormMenuItem("退出", (sender, args) =>
            {
                try
                {
                    UrlReg.UnReg("doh");
                    UrlReg.UnReg("dns-over-https");
                    UrlReg.UnReg("aurora-doh-list");
                    File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                "\\AuroraDNS.UrlReged");
                    if (!DnsSettings.AutoCleanLogEnable) return;
                    foreach (var item in Directory.GetFiles($"{SetupBasePath}Log"))
                        if (item != $"{SetupBasePath}Log" +
                            $"\\{DateTime.Today.Year}{DateTime.Today.Month:00}{DateTime.Today.Day:00}.log")
                            File.Delete(item);
                    if (File.Exists(Path.GetTempPath() + "setdns.cmd")) File.Delete(Path.GetTempPath() + "setdns.cmd");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Close();
                Environment.Exit(Environment.ExitCode);
            });

            NotifyIcon.ContextMenu =
                new WinFormContextMenu(new[]
                {
                    showItem, notepadLogItem, new WinFormMenuItem("-"), abootItem, updateItem, settingsItem, new WinFormMenuItem("-"), restartItem, exitItem
                });

            NotifyIcon.DoubleClick += MinimizedNormal;

            IsSysDns.IsChecked = MyTools.IsNslookupLocDns();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
            if (Environment.OSVersion.Version.Major == 10)
                WindowBlur.SetEnabled(this, true);
            else
            {
                NotifyIcon.Icon = Properties.Resources.AuroraBlack;
                Background = new SolidColorBrush(Colors.White) {Opacity = 1};
            }

            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width - 5;
            Top = desktopWorkingArea.Bottom - Height - 5;

            FadeIn(0.2);
            Visibility = Visibility.Visible;
            NotifyIcon.Visible = true;

            if (!MyTools.PortIsUse(DnsSettings.ListenPort))
            {
                IsLog.IsChecked = DnsSettings.DebugLog;
                if (Equals(DnsSettings.ListenIp, IPAddress.Any))
                    IsGlobal.IsChecked = true;

                DnsEnable.IsChecked = true;
                Grid.Effect = null;

                if (File.Exists($"{SetupBasePath}config.json"))
                    WindowState = WindowState.Minimized;
            }
            else
            {
                Snackbar.IsActive = true;
                if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)
                        .Count(o => o.Id != Process.GetCurrentProcess().Id) > 0)
                {
                    var snackbarMsg = new SnackbarMessage()
                    {
                        Content = "可能已有一个正在运行的实例, 请不要重复启动！",
                        ActionContent = "退出"
                    };
                    snackbarMsg.ActionClick += (o, args) => Environment.Exit(Environment.ExitCode);
                    Snackbar.Message = snackbarMsg;
                    NotifyIcon.Text = @"AuroraDNS - [请不要重复启动]";
                }
                else
                {
                    Snackbar.Message = new SnackbarMessage() {Content = $"DNS 服务器无法启动, {DnsSettings.ListenPort}端口被占用。"};
                    NotifyIcon.Text = @"AuroraDNS - [端口被占用]";
                }

                DnsEnable.IsEnabled = false;
                ControlGrid.IsEnabled = false;
            }

            if (Equals(DnsSettings.ListenIp, IPAddress.IPv6Any) ||
                Equals(DnsSettings.ListenIp, IPAddress.IPv6Loopback))
            {
                new Fwder(Equals(DnsSettings.ListenIp, IPAddress.IPv6Any) ? IPAddress.Any : IPAddress.Loopback, 53,
                    IPAddress.IPv6Loopback).Run();
            }

            IsLog.ToolTip = IsLog.IsChecked == true ? "记录日志 : 是" : "记录日志 : 否";
            if (IsSysDns.IsChecked == true) IsSysDns.ToolTip = "已设为系统 DNS";
            if (Equals(DnsSettings.ListenIp, IPAddress.Any)) IsGlobal.ToolTip = "当前监听 : 局域网";
            else if (Equals(DnsSettings.ListenIp, IPAddress.Loopback)) IsGlobal.ToolTip = "当前监听 : 本地";
            else IsGlobal.ToolTip = "当前监听 : " + DnsSettings.ListenIp;
        }

        private void IsGlobal_Checked(object sender, RoutedEventArgs e)
        {
            if (MyTools.PortIsUse(DnsSettings.ListenPort))
            {
                MDnsServer.Stop();
                MDnsServer = new DnsServer(new IPEndPoint(IPAddress.Any, DnsSettings.ListenPort), 10, 10);
                MDnsServer.QueryReceived += QueryResolve.ServerOnQueryReceived;
                Snackbar.MessageQueue.Enqueue(new TextBlock {Text = "监听地址 : 局域网 " + IPAddress.Any});
                MDnsSvrTask = new Task(() => MDnsServer.Start());
                MDnsSvrTask.Start();
                IsGlobal.ToolTip = "当前监听 : 局域网";
            }
        }

        private void IsGlobal_Unchecked(object sender, RoutedEventArgs e)
        {
            if (MyTools.PortIsUse(DnsSettings.ListenPort))
            {
                MDnsServer.Stop();
                MDnsServer = new DnsServer(new IPEndPoint(IPAddress.Loopback, DnsSettings.ListenPort), 10, 10);
                MDnsServer.QueryReceived += QueryResolve.ServerOnQueryReceived;
                Snackbar.MessageQueue.Enqueue(new TextBlock {Text = "监听地址 : 本地 " + IPAddress.Loopback});
                MDnsSvrTask = new Task(() => MDnsServer.Start());
                MDnsSvrTask.Start();
                IsGlobal.ToolTip = "当前监听 : 本地";
            }
        }

        private void IsLog_Checked(object sender, RoutedEventArgs e)
        {
            DnsSettings.DebugLog = true;
            if (MyTools.PortIsUse(DnsSettings.ListenPort))
                Snackbar.MessageQueue.Enqueue(new TextBlock {Text = "记录日志 : 是" });
            IsLog.ToolTip = "记录日志 : 是";
        }

        private void IsLog_Unchecked(object sender, RoutedEventArgs e)
        {
            DnsSettings.DebugLog = false;
            if (MyTools.PortIsUse(DnsSettings.ListenPort))
                Snackbar.MessageQueue.Enqueue(new TextBlock {Text = "记录日志 : 否" });
            IsLog.ToolTip = "记录日志 : 否";
        }

        private void DnsEnable_Checked(object sender, RoutedEventArgs e)
        {
            MDnsSvrTask = new Task(() => MDnsServer.Start());
            MDnsSvrTask.Start();
            if (!MDnsSvrTask.IsCompleted)
            {
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "DNS 服务器已启动" });
                NotifyIcon.Text = @"AuroraDNS - Running";
            }
        }

        private void DnsEnable_Unchecked(object sender, RoutedEventArgs e)
        {
            MDnsServer.Stop();
            if (!MDnsSvrTask.IsCompleted)
            {
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "DNS 服务器已停止" });
                NotifyIcon.Text = @"AuroraDNS - Stop";
            }
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Closed += (o, args) =>
            {
                IsLog.IsChecked = DnsSettings.DebugLog;
                IsGlobal.IsChecked = Equals(DnsSettings.ListenIp, IPAddress.Any);
                //IsLog.ToolTip = IsLog.IsChecked.Value ? "记录日志 : 是" : "记录日志 : 否";
                //IsGlobal.ToolTip = Equals(DnsSettings.ListenIp, IPAddress.Any) ? "当前监听 : 局域网" : "当前监听 : 本地";
            };
            settingsWindow.Show();
        }

        public void RunAsAdmin()
        {
            try
            {
                MDnsServer.Stop();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = GetType().Assembly.Location,
                    Verb = "runas"
                };

                Process.Start(startInfo);
                Environment.Exit(Environment.ExitCode);
            }
            catch (Exception exception)
            {
                MyTools.BackgroundLog(exception.ToString());
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                FadeIn(0.2);
                ShowInTaskbar = true;
            }
            else if (WindowState == WindowState.Minimized)
                ShowInTaskbar = false;

            GC.Collect();
        }

        private void FadeIn(double sec)
        {
            var fadeInStoryboard = new Storyboard();
            DoubleAnimation fadeInAnimation = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromSeconds(sec)));
            Storyboard.SetTarget(fadeInAnimation, this);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(OpacityProperty));
            fadeInStoryboard.Children.Add(fadeInAnimation);

            Dispatcher?.BeginInvoke(new Action(fadeInStoryboard.Begin), DispatcherPriority.Render, null);
        }

        private void MinimizedNormal(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Minimized;
                Hide();
            }
            else if (WindowState == WindowState.Minimized)
            {
                Show();
                WindowState = WindowState.Normal;
            }
        }

        private void IsSysDns_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MyTools.IsNslookupLocDns())
                {
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        SysDnsSet.ResetDns();
                        Snackbar.MessageQueue.Enqueue(new TextBlock {Text = "已将 DNS 重置为自动获取"});
                    }
                    else
                    {
                        SysDnsSet.ResetDnsCmd();
                        Snackbar.MessageQueue.Enqueue(new TextBlock {Text = "已通过 Netsh 将 DNS 重置为自动获取"});
                    }

                    IsSysDns.ToolTip = "设为系统 DNS";
                    IsSysDns.IsChecked = false;
                }
                else
                {
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(
                        WindowsBuiltInRole.Administrator))
                    {
                        SysDnsSet.SetDns(IPAddress.Loopback.ToString(), DnsSettings.SecondDnsIp.ToString());
                        IsSysDns.ToolTip = "已设为系统 DNS";
                    }
                    else
                    {
                        SysDnsSet.SetDnsCmd(IPAddress.Loopback.ToString(), DnsSettings.SecondDnsIp.ToString());
                        Snackbar.MessageQueue.Enqueue(new TextBlock {Text = "已通过 Netsh 设为系统 DNS"});
                    }

                    Snackbar.MessageQueue.Enqueue(new TextBlock
                    {
                        Text = "主DNS : " + IPAddress.Loopback +
                               Environment.NewLine +
                               "辅DNS : " + DnsSettings.SecondDnsIp
                    });

                    IsSysDns.ToolTip = "已设为系统 DNS";
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }
    }
}
