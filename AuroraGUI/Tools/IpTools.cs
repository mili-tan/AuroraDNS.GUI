using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace AuroraGUI
{
    static class IpTools
    {
        public static bool IsIp(string ip)
        {
            return Regex.IsMatch(ip, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
        }

        public static bool InSameLaNet(IPAddress ipA, IPAddress ipB)
        {
            return ipA.GetHashCode() % 65536L == ipB.GetHashCode() % 65536L;
        }

        public static string GetLocIp()
        {
            try
            {
                using (TcpClient tcpClient = new TcpClient())
                {

                    if (DnsSettings.HttpsDnsUrl.Split('/')[2].Contains(":"))
                        tcpClient.Connect(DnsSettings.HttpsDnsUrl.Split('/', ':')[3],
                            Convert.ToInt32(DnsSettings.HttpsDnsUrl.Split('/', ':')[4]));
                    else
                        tcpClient.Connect(DnsSettings.HttpsDnsUrl.Split('/')[2], 5555);
                    

                    //MessageBox.Show(DtcpClient.Client.LocalEndPoint).Address.ToString());
                    return ((IPEndPoint) tcpClient.Client.LocalEndPoint).Address.ToString();
                }
            }
            catch (Exception e)
            {
                MyTools.BgwLog("Try Connect:" + e);
                if (MessageBox.Show("Error: 尝试连接远端 DNS over HTTPS 服务器发生错误\n\r请检查 DNS over HTTPS 服务器是否有效\n\r点击“确定”以重试连接,点击“取消”放弃连接使用预设地址\n\rOriginal error: "
                                    + e.Message,"错误",MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    return GetLocIp();
                }
                return "192.168.0.1";
            }
        }

        public static string GetIntIp()
        {
            try
            {
                return Thread.CurrentThread.CurrentCulture.Name == "zh-CN"
                    ? new WebClient().DownloadString("http://members.3322.org/dyndns/getip").Trim()
                    : new WebClient().DownloadString("https://api.ipify.org").Trim();
            }
            catch (Exception e)
            {
                MyTools.BgwLog("Try Connect:" + e);
                if (MessageBox.Show("Error: 尝试获取公网IP地址失败\n\r点击“确定”以重试连接,点击“取消”放弃连接使用预设地址\n\rOriginal error: " 
                                    + e.Message) == MessageBoxResult.OK)
                {
                    return GetIntIp();
                }
                return IPAddress.Any.ToString();
            }
        }
    }
}
