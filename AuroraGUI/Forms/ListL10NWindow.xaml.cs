using System.IO;
using System.Windows;
using AuroraGUI.DnsSvr;

// ReSharper disable LocalizableElement

namespace AuroraGUI.Forms
{
    public partial class ListL10NWindow
    {
        public ListL10NWindow()
        {
            InitializeComponent();
        }

        private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
        {
            UrlSettings.MDnsList = DNSListURL.Text.Trim();
            UrlSettings.MDohList = DoHListURL.Text.Trim();
            UrlSettings.WhatMyIpApi = WhatMyIPURL.Text.Trim();
            UrlSettings.GeoIpApi = GeoIPURL.Text.Trim();

            File.WriteAllText($"{MainWindow.SetupBasePath}url.json",
                "{\n  " +
                $"\"GeoIPAPI\" : {UrlSettings.GeoIpApi},\n  " +
                $"\"WhatMyIPAPI\" : \"{UrlSettings.WhatMyIpApi}\",\n  " +
                $"\"DNSList\" : \"{UrlSettings.MDnsList}\",\n  " +
                $"\"DoHList\" : \"{UrlSettings.MDnsList}\" \n" +
                "}");
            MessageBox.Show(@"设置已保存!");
        }
    }
}
