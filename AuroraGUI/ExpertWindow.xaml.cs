using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace AuroraGUI
{
    /// <summary>
    /// ExpertWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ExpertWindow : Window
    {
        public ExpertWindow()
        {
            InitializeComponent();
            WindowBlur.SetEnabled(this, true);
            Snackbar.IsActive = true;
            Card.Effect = new BlurEffect() { Radius = 10 , RenderingBias = RenderingBias.Quality };
        }

        private void SnackbarMessage_OnActionClick(object sender, RoutedEventArgs e)
        {
            Card.IsEnabled = true;
            Snackbar.IsActive = false;
            Card.Effect = null;
        }

        private void ButtonReadDoHList_OnClick(object sender, RoutedEventArgs e)
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
                    MessageBox.Show("Error: 无法写入文件 \n\rOriginal error: " + ex.Message);
                }
            }
        }
    }
}
