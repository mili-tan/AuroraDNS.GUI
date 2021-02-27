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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ARSoft.Tools.Net.Dns;
using AuroraGUI.DnsSvr;
using AuroraGUI.Tools;
using MaterialDesignThemes.Wpf;
using SourceChord.FluentWPF;
using static System.AppDomain;
using static SourceChord.FluentWPF.AcrylicWindow;
using MessageBox = System.Windows.MessageBox;
// ReSharper disable UseObjectOrCollectionInitializer
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
                worker.DoWork += (a, s) =>
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

            IsSysDns.IsChecked = MyTools.IsNslookupLocDns();
        }

        private void ExitItem_Click(object sender, EventArgs e)
        {
            try
            {
                UrlReg.UnReg("doh");
                UrlReg.UnReg("dns-over-https");
                UrlReg.UnReg("aurora-doh-list");
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                            "\\AuroraDNS.UrlReged");
                if (DnsSettings.AutoCleanLogEnable)
                {
                    foreach (var item in Directory.GetFiles($"{SetupBasePath}Log"))
                        if (item != $"{SetupBasePath}Log" +
                            $"\\{DateTime.Today.Year}{DateTime.Today.Month:00}{DateTime.Today.Day:00}.log")
                            File.Delete(item);
                    if (File.Exists(Path.GetTempPath() + "setdns.cmd"))
                        File.Delete(Path.GetTempPath() + "setdns.cmd");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            try
            {
                Close();
                Environment.Exit(0);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Process.GetProcessById(Process.GetCurrentProcess().Id).Kill();
            }
        }

        private void ExitResetItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (new WindowsPrincipal(WindowsIdentity.GetCurrent())
                    .IsInRole(WindowsBuiltInRole.Administrator))
                    SysDnsSet.ResetDns();
                else SysDnsSet.ResetDnsCmd();
                UrlReg.UnReg("doh");
                UrlReg.UnReg("dns-over-https");
                UrlReg.UnReg("aurora-doh-list");
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                            "\\AuroraDNS.UrlReged");
                if (DnsSettings.AutoCleanLogEnable)
                {
                    foreach (var item in Directory.GetFiles($"{SetupBasePath}Log"))
                        if (item != $"{SetupBasePath}Log" +
                            $"\\{DateTime.Today.Year}{DateTime.Today.Month:00}{DateTime.Today.Day:00}.log")
                            File.Delete(item);
                    if (File.Exists(Path.GetTempPath() + "setdns.cmd"))
                        File.Delete(Path.GetTempPath() + "setdns.cmd");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            try
            {
                Close();
                Environment.Exit(0);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Process.GetProcessById(Process.GetCurrentProcess().Id).Kill();
            }
        }

        private void NotepadLogItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(
                $"{SetupBasePath}Log/{DateTime.Today.Year}{DateTime.Today.Month:00}{DateTime.Today.Day:00}.log"))
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "notepad.exe",
                    Arguments =
                        $"{SetupBasePath}Log/{DateTime.Today.Year}{DateTime.Today.Month:00}{DateTime.Today.Day:00}.log"
                });
            else
                MessageBox.Show("找不到当前日志文件，或当前未产生日志文件。");
        }

        private void RestartItem_Click(object sender, EventArgs e)
        {
            if (!MDnsSvrTask.IsCompleted)
                MDnsServer.Stop();
            Process.Start(new ProcessStartInfo { FileName = Process.GetCurrentProcess().MainModule.FileName });
            Environment.Exit(Environment.ExitCode);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Environment.OSVersion.Version.Major < 10)
            {
                try
                {
                    TaskbarIcon.IconSource = new BitmapImage(new Uri("pack://application:,,,/Resources/AuroraBlack.ico"));
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.ToString());
                }
                Background = new SolidColorBrush(Colors.White) {Opacity = 1};
            }

            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width - 5;
            Top = desktopWorkingArea.Bottom - Height - 5;

            FadeIn(0.2);
            Visibility = Visibility.Visible;
            TaskbarIcon.Visibility = Visibility.Visible;

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
                    snackbarMsg.ActionClick += (a, s) => Environment.Exit(Environment.ExitCode);
                    Snackbar.Message = snackbarMsg;
                    TaskbarToolTip.Text = @"AuroraDNS - [请不要重复启动]";
                }
                else
                {
                    Snackbar.Message = new SnackbarMessage() {Content = $"DNS 服务器无法启动, {DnsSettings.ListenPort}端口被占用。"};
                    TaskbarToolTip.Text = @"AuroraDNS - [端口被占用]";
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
                TaskbarToolTip.Text = @"AuroraDNS - Running";
            }
        }

        private void DnsEnable_Unchecked(object sender, RoutedEventArgs e)
        {
            MDnsServer.Stop();
            if (!MDnsSvrTask.IsCompleted)
            {
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "DNS 服务器已停止" });
                TaskbarToolTip.Text = @"AuroraDNS - Stop";
            }
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Closed += (a, s) =>
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
                    FileName = Process.GetCurrentProcess().MainModule.FileName,
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
            GC.Collect();
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

        private void AboutItem_OnClick(object sender, RoutedEventArgs e)
        {
            new AboutWindow().Show();
        }

        private void UpdateItem_OnClick(object sender, RoutedEventArgs e)
        {
            MyTools.CheckUpdate(GetType().Assembly.Location);
        }
    }
}
