using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using AuroraGUI.DnsSvr;
using Microsoft.Win32;

namespace AuroraGUI
{
    /// <summary>
    /// ExpertWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ExpertWindow
    {
        public bool OnExpert;

        public ExpertWindow()
        {
            InitializeComponent();
            Snackbar.IsActive = true;
            Card.Effect = new BlurEffect() { Radius = 10 , RenderingBias = RenderingBias.Performance };
        }

        private void SnackbarMessage_OnActionClick(object sender, RoutedEventArgs e)
        {
            Card.IsEnabled = true;
            Snackbar.IsActive = false;
            Card.Effect = null;
            OnExpert = true;

            ChinaList.IsChecked = DnsSettings.ChinaListEnable;
            DisabledV4.IsChecked = DnsSettings.Ipv4Disable;
            DisabledV6.IsChecked = DnsSettings.Ipv6Disable;
            AllowSelfSigned.IsChecked = DnsSettings.AllowSelfSignedCert;
            NotAllowAutoRedirect.IsChecked = !DnsSettings.AllowAutoRedirect;
        }

        private void ReadDoHListButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "list files (*.list)|*.list|txt files (*.txt)|*.txt|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(File.ReadAllText(openFileDialog.FileName)))
                        Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"Error: 无效的空文件。" });
                    else
                    {
                        File.Copy(openFileDialog.FileName, $"{MainWindow.SetupBasePath}doh.list");
                        Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"导入成功!" });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: 无法写入文件 {Environment.NewLine}Original error: " + ex.Message);
                }
            }
        }

        private void ReadDNSListButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "list files (*.list)|*.list|txt files (*.txt)|*.txt|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() != true) return;
            try
            {
                if (string.IsNullOrWhiteSpace(File.ReadAllText(openFileDialog.FileName)))
                    Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"Error: 无效的空文件。" });
                else
                {
                    File.Copy(openFileDialog.FileName, $"{MainWindow.SetupBasePath}dns.list");
                    Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"导入成功!" });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: 无法写入文件 {Environment.NewLine}Original error: " + ex.Message);
            }
        }

        private void ReadChinaListButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "list files (*.list)|*.list|txt files (*.txt)|*.txt|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() != true) return;
            try
            {
                if (string.IsNullOrWhiteSpace(File.ReadAllText(openFileDialog.FileName)))
                    Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"Error: 无效的空文件。" });
                else
                {
                    File.Copy(openFileDialog.FileName, $"{MainWindow.SetupBasePath}china.list");
                    Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"导入成功!" });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: 无法写入文件 {Environment.NewLine}Original error: " + ex.Message);
            }
        }

        private void DisabledV4_OnClick(object sender, RoutedEventArgs e)
        {
            if (DisabledV4.IsChecked == null) return;
            DnsSettings.Ipv4Disable = DisabledV4.IsChecked.Value;

            if (!DisabledV4.IsChecked.Value) return;
            DisabledV6.IsChecked = false;
            DnsSettings.Ipv6Disable = false;
        }

        private void DisabledV6_OnClick(object sender, RoutedEventArgs e)
        {
            if (DisabledV6.IsChecked == null) return;
            DnsSettings.Ipv6Disable = DisabledV6.IsChecked.Value;

            if (!DisabledV6.IsChecked.Value) return;
            DisabledV4.IsChecked = false;
            DnsSettings.Ipv4Disable = false;
        }

        private void ChinaList_OnClick(object sender, RoutedEventArgs e)
        {
            if (ChinaList.IsChecked == null) return;
            DnsSettings.ChinaListEnable = ChinaList.IsChecked.Value;
        }

        private void AllowSelfSigned_OnClick(object sender, RoutedEventArgs e)
        {
            if (AllowSelfSigned.IsChecked == null) return;
            DnsSettings.AllowSelfSignedCert = AllowSelfSigned.IsChecked.Value;
        }

        private void NotAllowAutoRedirect_OnClick(object sender, RoutedEventArgs e)
        {
            if (NotAllowAutoRedirect.IsChecked == null) return;
            DnsSettings.AllowAutoRedirect = !NotAllowAutoRedirect.IsChecked.Value;
        }
    }
}
