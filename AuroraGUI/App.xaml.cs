using System;
using System.Windows;
using System.Windows.Threading;

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
            MyTools.BgwLog(e.ExceptionObject.ToString());
            if (e.IsTerminating)
            {
                MessageBox.Show(
                    "发生了可能致命的严重错误，请从以下错误信息汲取灵感。\n\r程序即将中止运行。\n\r" + e.ExceptionObject,
                    "意外的错误。", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MyTools.BgwLog(e.Exception.ToString());
            MessageBoxResult msgResult = MessageBox.Show(
                "未经处理的异常错误，请从以下错误信息汲取灵感。\n\r点击取消中止程序运行。\n\r" + e.Exception,
                "意外的错误。", MessageBoxButton.OKCancel, MessageBoxImage.Error);

            if (MessageBoxResult.OK == msgResult)
                e.Handled = true;
            else
                Shutdown(1);
        }
    }
}
