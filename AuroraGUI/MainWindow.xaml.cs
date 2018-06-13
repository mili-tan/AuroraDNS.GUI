using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AuroraGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        internal enum AccentState
        {
            ACCENT_ENABLE_BLURBEHIND = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableBlur();
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width - 5;
            Top = desktopWorkingArea.Bottom - Height - 5;
        }

        internal void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);
            var accent = new AccentPolicy {AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND};
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        private void IsSysDns_Checked(object sender, RoutedEventArgs e)
        {
            SysDnsSet.SetDns("1.1.1.1","1.0.0.1");
        }

        private void IsSysDns_Unchecked(object sender, RoutedEventArgs e)
        {
            SysDnsSet.ResetDns();
        }

        private void IsGlobal_Checked(object sender, RoutedEventArgs e)
        {
            DnsSettings.ListenIp = IPAddress.Any;
        }

        private void IsGlobal_Unchecked(object sender, RoutedEventArgs e)
        {
            DnsSettings.ListenIp = IPAddress.Loopback;
        }

        private void IsLog_Checked(object sender, RoutedEventArgs e)
        {
            DnsSettings.DebugLog = true;
        }

        private void IsLog_Unchecked(object sender, RoutedEventArgs e)
        {
            DnsSettings.DebugLog = false;
        }
    }
}
