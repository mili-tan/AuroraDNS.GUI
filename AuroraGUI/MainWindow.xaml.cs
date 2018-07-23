using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using MessageBox = System.Windows.MessageBox;
using WinFormMenuItem = System.Windows.Forms.MenuItem;
using WinFormContextMenu = System.Windows.Forms.ContextMenu;
using WinFormIcon = System.Drawing.Icon;

// ReSharper disable NotAccessedField.Local

namespace AuroraGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public static IPAddress IntIPAddr;
        public static IPAddress LocIPAddr;
        private static NotifyIcon NotifyIcon;
        private static BackgroundWorker DnsSvrWorker = new BackgroundWorker(){WorkerSupportsCancellation = true};

        public MainWindow()
        {
            InitializeComponent();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            WindowStyle = WindowStyle.SingleBorderWindow;

            if (File.Exists("config.json"))
                DnsSettings.ReadConfig("config.json");

            if (DnsSettings.BlackListEnable && File.Exists("black.list"))
                DnsSettings.ReadBlackList();

            if (DnsSettings.BlackListEnable && File.Exists("white.list"))
                DnsSettings.ReadWhiteList();

            LocIPAddr = IPAddress.Parse(IpTools.GetLocIp());
            try
            {
                IntIPAddr = IPAddress.Parse(Thread.CurrentThread.CurrentCulture.Name == "zh-CN"
                    ? new WebClient().DownloadString("http://members.3322.org/dyndns/getip").Trim()
                    : new WebClient().DownloadString("https://api.ipify.org").Trim());
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error: 尝试获取公网IP地址失败 \n\rOriginal error: " + ex.Message);
                IntIPAddr = IPAddress.Any;
            }

            DnsServer myDnsServer = new DnsServer(DnsSettings.ListenIp, 10, 10);
            myDnsServer.QueryReceived += QueryResolve.ServerOnQueryReceived;
            DnsSvrWorker.DoWork += (sender, args) => myDnsServer.Start();
            DnsSvrWorker.Disposed += (sender, args) => myDnsServer.Stop();
            
            NotifyIcon = new NotifyIcon(){Text = @"AuroraDNS",Visible = true,
                Icon = WinFormIcon.ExtractAssociatedIcon(GetType().Assembly.Location) };
            WinFormMenuItem showItem = new WinFormMenuItem("最小化 / 恢复", MinimizedNormal);
            WinFormMenuItem abootItem = new WinFormMenuItem("关于", (sender, args) => new AboutWindow().ShowDialog());
            WinFormMenuItem exitItem = new WinFormMenuItem("退出", (sender, args) => Environment.Exit(Environment.ExitCode));
            NotifyIcon.ContextMenu = new WinFormContextMenu(new[] {showItem, abootItem, exitItem});
            NotifyIcon.DoubleClick += MinimizedNormal;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
            WindowBlur.SetEnabled(this, true);
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width - 5;
            Top = desktopWorkingArea.Bottom - Height - 5;

            FadeIn(0.50);
            Visibility = Visibility.Visible;

            if (!MyTools.PortIsUse(53))
            {
                IsLog.IsChecked = DnsSettings.DebugLog;
                if (Equals(DnsSettings.ListenIp, IPAddress.Any))
                    IsGlobal.IsChecked = true;

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
            if (DnsSvrWorker.IsBusy)
            {
                DnsSvrWorker.Dispose();
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "监听地址:" + IPAddress.Any });
                DnsSvrWorker.RunWorkerAsync();
            }
        }

        private void IsGlobal_Unchecked(object sender, RoutedEventArgs e)
        {
            DnsSettings.ListenIp = IPAddress.Loopback;
            if (DnsSvrWorker.IsBusy)
            {
                DnsSvrWorker.Dispose();
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "监听地址:" + IPAddress.Loopback });
                DnsSvrWorker.RunWorkerAsync();
            }
        }

        private void IsSysDns_Checked(object sender, RoutedEventArgs e)
        {
            if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                SysDnsSet.SetDns("127.0.0.1", DnsSettings.SecondDnsIp.ToString());
                Snackbar.MessageQueue.Enqueue(new TextBlock()
                {
                    Text = "主DNS:" + IPAddress.Loopback + 
                           Environment.NewLine + 
                           "辅DNS:" + DnsSettings.SecondDnsIp
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
            if (DnsSvrWorker.IsBusy)
                Snackbar.MessageQueue.Enqueue(new TextBlock() {Text = "记录日志:" + DnsSettings.DebugLog});
        }

        private void IsLog_Unchecked(object sender, RoutedEventArgs e)
        {
            DnsSettings.DebugLog = false;
            if (DnsSvrWorker.IsBusy)
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "记录日志:" + DnsSettings.DebugLog });
        }

        private void DnsEnable_Checked(object sender, RoutedEventArgs e)
        {
            DnsSvrWorker.RunWorkerAsync();
            if (DnsSvrWorker.IsBusy)
            {
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "DNS 服务器已启动" });
                NotifyIcon.Text = @"AuroraDNS - Running";
            }
        }

        private void DnsEnable_Unchecked(object sender, RoutedEventArgs e)
        {
            DnsSvrWorker.Dispose();
            if (!DnsSvrWorker.IsBusy)
            {
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = "DNS 服务器已停止" });
                NotifyIcon.Text = @"AuroraDNS - Stop";
            }
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();

            IsLog.IsChecked = DnsSettings.DebugLog;
            IsGlobal.IsChecked = Equals(DnsSettings.ListenIp, IPAddress.Any);

            if (DnsSettings.BlackListEnable && File.Exists("black.list"))
                DnsSettings.ReadBlackList();

            if (DnsSettings.BlackListEnable && File.Exists("white.list"))
                DnsSettings.ReadWhiteList();
        }

        private void RunAsAdmin_OnActionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DnsSvrWorker.Dispose();
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
                MyTools.BgwLog(exception.ToString());
            }
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

        private void MinimizedNormal(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Minimized;
            else if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
        }
    }
}
