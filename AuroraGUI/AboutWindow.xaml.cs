using System.Diagnostics;
using System.IO;
using System.Windows;

namespace AuroraGUI
{
    /// <summary>
    /// AboutWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AboutWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
            VerText.Text += FileVersionInfo.GetVersionInfo(GetType().Assembly.Location).FileVersion;
            VerText.Text += $" ({File.GetLastWriteTime(GetType().Assembly.Location)})";
        }

        private void ButtonCredits_OnClick(object sender, RoutedEventArgs e) 
            => Process.Start($"https://github.com/mili-tan/AuroraDNS.GUI/raw/master/CREDITS");

        private void ButtonAbout_OnClick(object sender, RoutedEventArgs e)
            => Process.Start($"https://dns.mili.one");
    }
}
