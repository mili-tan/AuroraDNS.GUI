using System;
using System.Diagnostics;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ARSoft.Tools.Net.Dns;
using MaterialDesignThemes.Wpf;
// ReSharper disable NotAccessedField.Local

namespace AuroraGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public static DnsServer MyDnsServer;
        public static IPAddress MyIPAddr;
        public static IPAddress LocIPAddr;
        private NotifyIcon NotifyIcon;

        public MainWindow()
        {

            InitializeComponent();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            LocIPAddr = IPAddress.Parse(IpTools.GetLocIp());
            MyIPAddr = IPAddress.Parse(Thread.CurrentThread.CurrentCulture.Name == "zh-CN" 
                ? new WebClient().DownloadString("http://members.3322.org/dyndns/getip").Trim() 
                : new WebClient().DownloadString("https://api.ipify.org").Trim());

            MyDnsServer = new DnsServer(DnsSettings.ListenIp, 10, 10);
            MyDnsServer.QueryReceived += QueryResolve.ServerOnQueryReceived;
            
            NotifyIcon = new NotifyIcon(){Text = @"AuroraDNS",Visible = true,Icon = System.Drawing.Icon.ExtractAssociatedIcon(GetType().Assembly.Location) };
            NotifyIcon.DoubleClick += NotifyIconOnDoubleClick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
            WindowBlur.SetEnabled(this, true);
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width - 5;
            Top = desktopWorkingArea.Bottom - Height - 5;
            Topmost = true;

            FadeIn(0.50);
            Visibility = Visibility.Visible;

            if (!Tools.PortIsUse(53))
            {
                DnsEnable.IsChecked = true;
            }
            else
            {
                Snackbar.IsActive = true;
                Snackbar.Message = new SnackbarMessage(){Content = "DNS 服务器无法启动:端口被占用" };
                NotifyIcon.Text = @"AuroraDNS - [端口被占用]";
                DnsEnable.IsEnabled = false;
                IsEnabled = false;
            }
        }

        private void IsGlobal_Checked(object sender, RoutedEventArgs e)
        {
            DnsSettings.ListenIp = IPAddress.Any;
            Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "监听地址:" + IPAddress.Any });
            DnsEnable.IsChecked = false;
        }

        private void IsGlobal_Unchecked(object sender, RoutedEventArgs e)
        {
            DnsSettings.ListenIp = IPAddress.Loopback;
            Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "监听地址:" + IPAddress.Loopback });
            DnsEnable.IsChecked = false;
        }

        private void IsSysDns_Checked(object sender, RoutedEventArgs e)
        {
            if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                SysDnsSet.SetDns("127.0.0.1", "1.0.0.1");
                Snackbar.MessageQueue.Enqueue(new TextBlock()
                {
                    Text = "主DNS:" + IPAddress.Loopback + 
                           Environment.NewLine + 
                           "辅DNS:1.0.0.1"
                });
            }
            else
            {
                var snackbarMsg = new SnackbarMessage()
                {
                    Content = "权限不足",
                    ActionContent = "Administrator权限运行",
                };
                snackbarMsg.ActionClick += RunAsAdmin_OnActionClick;
                Snackbar.MessageQueue.Enqueue(snackbarMsg);
                IsSysDns.IsChecked = false;
            }
        }

        private void IsSysDns_Unchecked(object sender, RoutedEventArgs e)
        {
            if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                SysDnsSet.ResetDns();
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "已将 DNS 重置为自动获取" });
            }
        }

        private void IsLog_Checked(object sender, RoutedEventArgs e)
        {
            DnsSettings.DebugLog = true;
            Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "记录日志:" + DnsSettings.DebugLog });
        }

        private void IsLog_Unchecked(object sender, RoutedEventArgs e)
        {
            DnsSettings.DebugLog = false;
            Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "记录日志:" + DnsSettings.DebugLog });
        }

        private void DnsEnable_Checked(object sender, RoutedEventArgs e)
        {
            MyDnsServer.Start();
            Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "DNS 服务器已启动" });
            NotifyIcon.Text = @"AuroraDNS - Running";
        }

        private void DnsEnable_Unchecked(object sender, RoutedEventArgs e)
        {
            MyDnsServer.Stop();
            Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "DNS 服务器已停止" });
            NotifyIcon.Text = @"AuroraDNS - Stop";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Snackbar.MessageQueue.Enqueue(new TextBlock(){Text = "未完成"});
        }

        private void RunAsAdmin_OnActionClick(object sender, RoutedEventArgs e)
        {
            DnsEnable.IsChecked = false;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = GetType().Assembly.Location,
                Verb = "runas"
            };
            Process.Start(startInfo);
            Environment.Exit(Environment.ExitCode);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                FadeIn(0.25);
                ShowInTaskbar = true;
            }
            else if (WindowState == WindowState.Minimized)
                ShowInTaskbar = false;
        }

        private void FadeIn(double sec)
        {
            var fadeInStoryboard = new Storyboard();
            DoubleAnimation fadeInAnimation = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromSeconds(sec)));
            Storyboard.SetTarget(fadeInAnimation, this);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(OpacityProperty));
            fadeInStoryboard.Children.Add(fadeInAnimation);

            Dispatcher.BeginInvoke(new Action(fadeInStoryboard.Begin), DispatcherPriority.Render, null);
        }

        private void NotifyIconOnDoubleClick(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Minimized;
            else if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
        }
    }
}
