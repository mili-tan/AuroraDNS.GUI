using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
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
                    {
                        tcpClient.Connect(DnsSettings.HttpsDnsUrl.Split('/', ':')[3],
                            Convert.ToInt32(DnsSettings.HttpsDnsUrl.Split('/', ':')[4]));
                    }
                    else
                    {
                        tcpClient.Connect(DnsSettings.HttpsDnsUrl.Split('/')[2], 443);
                    }

                    //MessageBox.Show(DtcpClient.Client.LocalEndPoint).Address.ToString());
                    return ((IPEndPoint) tcpClient.Client.LocalEndPoint).Address.ToString();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: 尝试连接远端 DNS over HTTPS 服务器发生错误\n\r请检查 DoH 接口是否有效\n\rOriginal error: " + e.Message);
                MyTools.BgwLog("Try Connect:" + e);
                return "192.168.0.1";
            }
        }
    }
}
