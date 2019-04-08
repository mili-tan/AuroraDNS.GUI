using System;
using System.Windows;
using System.Windows.Threading;
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
                    $"发生了可能致命的严重错误，请从以下错误信息汲取灵感。{Environment.NewLine}" +
                    $"程序可能即将中止运行。{Environment.NewLine}" + e.ExceptionObject,
                    "意外的错误。", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MyTools.BackgroundLog(e.Exception.ToString());
            MessageBoxResult msgResult = MessageBox.Show(
                $"未经处理的异常错误，请从以下错误信息汲取灵感。{Environment.NewLine}" +
                $"点击取消中止程序运行。{Environment.NewLine}" + e.Exception,
                "意外的错误。", MessageBoxButton.OKCancel, MessageBoxImage.Error);

            if (MessageBoxResult.OK == msgResult)
                e.Handled = true;
            else
                Shutdown(1);
        }
    }
}
