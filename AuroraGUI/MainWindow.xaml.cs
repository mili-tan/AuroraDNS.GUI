using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ARSoft.Tools.Net.Dns;
using AuroraGUI.DnsSvr;
using AuroraGUI.Fx;
using AuroraGUI.Tools;
using MaterialDesignThemes.Wpf;
using WinFormMenuItem = System.Windows.Forms.MenuItem;
using WinFormContextMenu = System.Windows.Forms.ContextMenu;
using static System.AppDomain;

// ReSharper disable NotAccessedField.Local

namespace AuroraGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public static string SetupBasePath = CurrentDomain.SetupInformation.ApplicationBase;
        public static IPAddress IntIPAddr;
        public static IPAddress LocIPAddr;
        private static NotifyIcon NotifyIcon;
        private static BackgroundWorker DnsSvrWorker = new BackgroundWorker(){WorkerSupportsCancellation = true};

        public MainWindow()
        {
            InitializeComponent();

            WindowStyle = WindowStyle.SingleBorderWindow;

            if (File.Exists($"{SetupBasePath}config.json"))
                DnsSettings.ReadConfig($"{SetupBasePath}config.json");

            if (DnsSettings.BlackListEnable && File.Exists($"{SetupBasePath}black.list"))
                DnsSettings.ReadBlackList($"{SetupBasePath}black.list");

            if (DnsSettings.WhiteListEnable && File.Exists($"{SetupBasePath}white.list"))
                DnsSettings.ReadWhiteList($"{SetupBasePath}white.list");

            if (DnsSettings.WhiteListEnable && File.Exists($"{SetupBasePath}rewrite.list"))
                DnsSettings.ReadWhiteList($"{SetupBasePath}rewrite.list");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            #pragma warning disable CS0162 //未实装
            if (false)
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => true;

            switch (1.2F)
            {
                case 1:
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                    break;
                case 1.1F:
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
                    break;
                case 1.2F:
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    break;
                default:
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    break;
            }
            #pragma warning restore CS0162

            LocIPAddr = IPAddress.Parse(IpTools.GetLocIp());
            IntIPAddr = IPAddress.Parse(IpTools.GetIntIp());

            DnsServer myDnsServer = new DnsServer(DnsSettings.ListenIp, 10, 10);
            myDnsServer.QueryReceived += QueryResolve.ServerOnQueryReceived;
            DnsSvrWorker.DoWork += (sender, args) => myDnsServer.Start();
            DnsSvrWorker.Disposed += (sender, args) => myDnsServer.Stop();
            
            NotifyIcon = new NotifyIcon(){Text = @"AuroraDNS",Visible = true,
                Icon = Properties.Resources.AuroraWhite};
            WinFormMenuItem showItem = new WinFormMenuItem("最小化 / 恢复", MinimizedNormal);
            WinFormMenuItem restartItem = new WinFormMenuItem("重新启动", (sender, args) =>
            {
                DnsSvrWorker.Dispose();
                Process.Start(new ProcessStartInfo {FileName = GetType().Assembly.Location});
                Environment.Exit(Environment.ExitCode);
            });
            WinFormMenuItem notepadLogItem = new WinFormMenuItem("查阅日志", (sender, args) =>
            {
                if (File.Exists(
                    $"{SetupBasePath}Log/{DateTime.Today.Year}{DateTime.Today.Month}{DateTime.Today.Day}.log")
                )
                    Process.Start(new ProcessStartInfo("notepad.exe",
                        $"{SetupBasePath}Log/{DateTime.Today.Year}{DateTime.Today.Month}{DateTime.Today.Day}.log"));
            });
            WinFormMenuItem abootItem = new WinFormMenuItem("关于…", (sender, args) => new AboutWindow().ShowDialog());
            WinFormMenuItem updateItem = new WinFormMenuItem("检查更新…", (sender, args) => MyTools.CheckUpdate(GetType().Assembly.Location));
            WinFormMenuItem settingsItem = new WinFormMenuItem("设置…", (sender, args) => new SettingsWindow().ShowDialog());
            WinFormMenuItem exitItem = new WinFormMenuItem("退出", (sender, args) => Environment.Exit(Environment.ExitCode));

            NotifyIcon.ContextMenu =
                new WinFormContextMenu(new[]
                {
                    showItem, notepadLogItem, new WinFormMenuItem("-"), abootItem, updateItem, settingsItem, new WinFormMenuItem("-"), restartItem, exitItem
                });

            NotifyIcon.DoubleClick += MinimizedNormal;

            if (MyTools.IsNslookupLocDns())
                IsSysDns.ToolTip = "已设为系统 DNS";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
            WindowBlur.SetEnabled(this, true);
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width - 1;
            Top = desktopWorkingArea.Bottom - Height - 0;

            FadeIn(0.50);
            Visibility = Visibility.Visible;

            if (!MyTools.PortIsUse(53))
            {
                IsLog.IsChecked = DnsSettings.DebugLog;
                if (Equals(DnsSettings.ListenIp, IPAddress.Any))
                    IsGlobal.IsChecked = true;

                DnsEnable.IsChecked = true;

                if (File.Exists($"{SetupBasePath}config.json"))
                    WindowState = WindowState.Minimized;
            }
            else
            {
                Snackbar.IsActive = true;
                if (Process.GetProcessesByName(System.Windows.Forms.Application.CompanyName).Length > 1)
                {
                    Snackbar.Message = new SnackbarMessage() {Content = "DNS 服务器无法启动:端口被占用"};
                    NotifyIcon.Text = @"AuroraDNS - [端口被占用]";
                }
                else
                {
                    Snackbar.Message = new SnackbarMessage() { Content = "DNS 服务器无法启动:可能已有一个实例正在运行, 请不要重复启动" };
                    NotifyIcon.Text = @"AuroraDNS - [请不要重复启动]";
                }

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
                IsSysDns.ToolTip = "已设为系统 DNS";
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
                IsSysDns.ToolTip = "设为系统 DNS";
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

            if (DnsSettings.WhiteListEnable && File.Exists("white.list"))
                DnsSettings.ReadWhiteList();

            if (DnsSettings.WhiteListEnable && File.Exists("rewrite.list"))
                DnsSettings.ReadWhiteList("rewrite.list");
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
    }
}
