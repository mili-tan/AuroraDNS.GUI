using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using AuroraGUI.DnsSvr;
using AuroraGUI.Forms;
using AuroraGUI.Tools;

namespace AuroraGUI
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MyTools.BackgroundLog(e.ExceptionObject.ToString());
            if (e.IsTerminating)
            {
                MessageBox.Show(
                    $"发生了可能致命性的严重错误，请从以下错误信息汲取灵感。{Environment.NewLine}" +
                    $"程序可能即将中止运行。{Environment.NewLine}" + e.ExceptionObject,
                    "意外的错误。", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MyTools.BackgroundLog(e.Exception.ToString());
            MessageBoxResult msgResult = MessageBox.Show(
                $"未经处理的异常，请从以下错误信息汲取灵感。{Environment.NewLine}" +
                $"点击取消中止程序运行，点击确定以继续。{Environment.NewLine}" + e.Exception,
                "意外的错误。", MessageBoxButton.OKCancel, MessageBoxImage.Error);

            if (MessageBoxResult.OK == msgResult)
                e.Handled = true;
            else
                Shutdown();
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            string setupBasePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            if (e.Args.Length == 0) return;
            if (e.Args[0].Split(':')[0] == "doh" || e.Args[0].Split(':')[0] == "dns-over-https")
            {
                MessageBoxResult msgResult =
                    MessageBox.Show(
                        $"您确定要将主 DNS over HTTPS 服务器设为 https:{e.Args[0].Split(':')[1]} 吗?" +
                        $"{Environment.NewLine}请确认这是可信的服务器，来源不明的服务器可能将会窃取您的个人隐私，或篡改网页植入恶意软件。请谨慎操作！",
                        "设置 DNS over HTTPS 服务器", MessageBoxButton.OKCancel);
                if (msgResult != MessageBoxResult.OK) return;

                if (File.Exists($"{setupBasePath}config.json"))
                    DnsSettings.ReadConfig($"{setupBasePath}config.json");
                DnsSettings.HttpsDnsUrl = e.Args[0].Replace("dns-over-https:", "https:").Replace("doh:", "https:");
                new SettingsWindow().ButtonSave_OnClick(sender, null);
            }
            else if (e.Args[0].Split(':')[0] == "aurora-doh-list")
            {
                MessageBoxResult msgResult =
                    MessageBox.Show(
                        $"您确定要将 DNS over HTTPS 服务器列表设为 https:{e.Args[0].Split(':')[1]} 吗?" +
                        $"{Environment.NewLine}请确认这是可信的服务器列表，来源不明的服务器可能将会窃取您的个人隐私，或篡改网页植入恶意软件。请谨慎操作！",
                        "设置 DNS over HTTPS 服务器列表", MessageBoxButton.OKCancel);
                if (msgResult != MessageBoxResult.OK) return;

                if (File.Exists($"{setupBasePath}url.json"))
                    UrlSettings.ReadConfig($"{setupBasePath}url.json");
                UrlSettings.MDohList = e.Args[0].Replace("aurora-doh-list:", "https:");
                new ListL10NWindow().ButtonSave_OnClick(sender, null);
            }

            foreach (var item in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
                if (item.Id != Process.GetCurrentProcess().Id)
                    item.Kill();
            Process.Start(new ProcessStartInfo {FileName = GetType().Assembly.Location});
            Shutdown();
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            string setupBasePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            if (!DnsSettings.AutoCleanLogEnable) return;
            foreach (var item in Directory.GetFiles($"{setupBasePath}Log"))
                if (item != $"{setupBasePath}Log" +
                    $"\\{DateTime.Today.Year}{DateTime.Today.Month:00}{DateTime.Today.Day:00}.log")
                    File.Delete(item);
            if (File.Exists(Path.GetTempPath() + "setdns.cmd")) File.Delete(Path.GetTempPath() + "setdns.cmd");
        }
    }
}
