using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using AuroraGUI.DnsSvr;
using MojoUnity;

namespace AuroraGUI.Tools
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
            var addressUri = new Uri(DnsSettings.HttpsDnsUrl);
            try
            {
                TcpClient tcpClient = new TcpClient();
                tcpClient.Connect(addressUri.DnsSafeHost, addressUri.Port);
                return ((IPEndPoint) tcpClient.Client.LocalEndPoint).Address.ToString();
            }
            catch (Exception e)
            {
                MyTools.BackgroundLog("Try Connect:" + e);
                try
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Connect(ResolveNameIpAddress(addressUri.DnsSafeHost), addressUri.Port);
                    return ((IPEndPoint)tcpClient.Client.LocalEndPoint).Address.ToString();
                }
                catch (Exception exception)
                {
                    return MessageBox.Show(
                               $"Error: 尝试连接远端 DNS over HTTPS 服务器发生错误(DoH-Server){Environment.NewLine}点击“确定”以重试连接,点击“取消”放弃连接使用预设地址。{Environment.NewLine}Original error: "
                               + exception.Message, @"错误", MessageBoxButton.OKCancel) == MessageBoxResult.OK
                        ? GetLocIp() : "192.168.0.1";
                }
            }
        }

        public static string GetIntIp()
        {
            try
            {
                return new MyCurl.MWebClient().DownloadString(UrlSettings.WhatMyIpApi).Trim();
            }
            catch (Exception e)
            {
                MyTools.BackgroundLog("Try Connect:" + e);
                try
                {
                    return new MyCurl.MIpBkWebClient().DownloadString(UrlSettings.WhatMyIpApi).Trim();
                }
                catch (Exception exception)
                {
                    MyTools.BackgroundLog("Try Connect:" + exception);
                    return MessageBox.Show($"Error: 尝试获取公网IP地址失败(WhatMyIP-API){Environment.NewLine}点击“确定”以重试连接,点击“取消”放弃连接使用预设地址。{Environment.NewLine}Original error: "
                                           + exception.Message, @"错误", MessageBoxButton.OKCancel) == MessageBoxResult.OK
                        ? GetIntIp() : IPAddress.Any.ToString();
                }
            }
        }

        public static IPAddress ResolveNameIpAddress(string name)
        {
            if (IPAddress.TryParse(name.TrimEnd('.'), out _)) return IPAddress.Parse(name.TrimEnd('.'));
            while (true)
            {
                var ipMsg = new DnsClient(DnsSettings.SecondDnsIp, 5000).Resolve(DomainName.Parse(name)).AnswerRecords[0];
                if (ipMsg.RecordType == RecordType.A)
                {
                    if (ipMsg is ARecord msg) return msg.Address;
                }
                else if (ipMsg.RecordType == RecordType.CName)
                {
                    if (ipMsg is CNameRecord msg) name = msg.CanonicalName.ToString();
                }
            }
        }

        public static string GeoIpLocal(string ipStr, bool onlyCountryCode = false)
        {
            try
            {
                string locStr = new WebClient().DownloadString(IsIp(ipStr)
                    ? $"{UrlSettings.GeoIpApi}{ipStr}": $"{UrlSettings.GeoIpApi}{Dns.GetHostAddresses(ipStr)[0]}");
                JsonValue json = Json.Parse(locStr);

                string countryCode;
                string organization;

                if (locStr.Contains("\"country_code\""))
                    countryCode = json.AsObjectGetString("country_code");
                else if (locStr.Contains("\"countryCode\""))
                    countryCode = json.AsObjectGetString("countryCode");
                else
                    countryCode = "Unknown";

                if (locStr.Contains("\"organization\""))
                    organization = json.AsObjectGetString("organization");
                else if (locStr.Contains("\"as\""))
                    organization = json.AsObjectGetString("as");
                else if (locStr.Contains("\"org\""))
                    organization = json.AsObjectGetString("org");
                else
                    organization = "";

                if (onlyCountryCode) return countryCode;
                return countryCode + " " + organization;
            }
            catch (Exception e)
            {
                MyTools.BackgroundLog(@"| DownloadString failed : " + e);
                return null;
            }
        }
    }
}
