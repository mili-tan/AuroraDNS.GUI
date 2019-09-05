using System.IO;
using System.Windows;
using System.Windows.Controls;
using AuroraGUI.DnsSvr;

// ReSharper disable LocalizableElement

namespace AuroraGUI.Forms
{
    public partial class ListL10NWindow
    {
        public ListL10NWindow()
        {
            InitializeComponent();

            DNSListURL.Text = UrlSettings.MDnsList;
            DoHListURL.Text = UrlSettings.MDohList;
            WhatMyIPURL.Text = UrlSettings.WhatMyIpApi;
            GeoIPURL.Text = UrlSettings.GeoIpApi;
        }

        public void ButtonSave_OnClick(object sender, RoutedEventArgs e)
        {
            UrlSettings.MDnsList = DNSListURL.Text.Trim();
            UrlSettings.MDohList = DoHListURL.Text.Trim();
            UrlSettings.WhatMyIpApi = WhatMyIPURL.Text.Trim();
            UrlSettings.GeoIpApi = GeoIPURL.Text.Trim();

            File.WriteAllText($"{MainWindow.SetupBasePath}url.json",
                "{\n  " +
                $"\"GeoIPAPI\" : \"{UrlSettings.GeoIpApi}\",\n  " +
                $"\"WhatMyIPAPI\" : \"{UrlSettings.WhatMyIpApi}\",\n  " +
                $"\"DNSList\" : \"{UrlSettings.MDnsList}\",\n  " +
                $"\"DoHList\" : \"{UrlSettings.MDohList}\" \n" +
                "}");
            Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"设置已保存!" });
        }
    }
}
