using System.Diagnostics;
using System.IO;

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
            Topmost = true;
            VerText.Text += FileVersionInfo.GetVersionInfo(GetType().Assembly.Location).FileVersion;
            VerText.Text += $" ({File.GetLastWriteTime(GetType().Assembly.Location)})";
        }
    }
}
