using System;
using System.Collections.Generic;
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
    }
}
