using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using AuroraGUI.DnsSvr;
using MojoUnity;

namespace AuroraGUI.Tools
{
    static class IpTools
    {
        public static bool IsIp(string ip) => IPAddress.TryParse(ip,out _);

        public static bool InSameLaNet(IPAddress ipA, IPAddress ipB) =>
            ipA.GetHashCode() % 65536L == ipB.GetHashCode() % 65536L;

        public static string GetLocIp()
        {
            var tcpClient = new TcpClient { ReceiveTimeout = 2500, SendTimeout = 2500 };
            var addressUri = new Uri(DnsSettings.HttpsDnsUrl);
            try
            {
                tcpClient.Connect(addressUri.DnsSafeHost, addressUri.Port);
                return ((IPEndPoint) tcpClient.Client.LocalEndPoint).Address.ToString();
            }
            catch (Exception e)
            {
                MyTools.BackgroundLog("Try Connect:" + e);
                try
                {
                    try
                    {
                        tcpClient.Connect(ResolveNameIpAddress(addressUri.DnsSafeHost), addressUri.Port);
                        return ((IPEndPoint)tcpClient.Client.LocalEndPoint).Address.ToString();
                    }
                    catch
                    {
                        addressUri = new Uri(DnsSettings.SecondHttpsDnsUrl);
                        tcpClient.Connect(ResolveNameIpAddress(addressUri.DnsSafeHost), addressUri.Port);
                        return ((IPEndPoint)tcpClient.Client.LocalEndPoint).Address.ToString();
                    }
                }
                catch
                {
                    return MessageBox.Show(
                        $"Error: 尝试连接远端 DNS over HTTPS 服务器发生错误{Environment.NewLine}点击“确定”以重试连接,点击“取消”放弃连接使用备用 DNS 服务器测试。" +
                        $"{Environment.NewLine}Original error: " + e.Message, @"错误", MessageBoxButton.OKCancel) == MessageBoxResult.OK
                        ? GetLocIp() : GetLocIpUdp();
                }
            }
        }

        public static string GetLocIpUdp()
        {
            try
            {
                var udpClient = new UdpClient();
                udpClient.Connect(DnsSettings.SecondDnsIp, 53);
                return ((IPEndPoint)udpClient.Client.LocalEndPoint).Address.ToString();
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    $"Error: 尝试连接远端备用 DNS 服务器失败，请检查您的设置与互联网连接。" +
                    $"{Environment.NewLine}Original error: " + e.Message, @"错误");
                return "192.168.1.1";
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
                try
                {
                    DnsRecordBase ipMsg;
                    if (DnsSettings.StartupOverDoH)
                        ipMsg = QueryResolve.ResolveOverHttpsByDnsJson(IPAddress.Any.ToString(),
                            name, "https://1.0.0.1/dns-query", DnsSettings.ProxyEnable, DnsSettings.WProxy).list[0];
                    else
                        ipMsg = new DnsClient(DnsSettings.SecondDnsIp, 5000).Resolve(DomainName.Parse(name))
                            .AnswerRecords[0];

                    switch (ipMsg.RecordType)
                    {
                        case RecordType.A when ipMsg is ARecord msg1:
                            return msg1.Address;
                        case RecordType.CName:
                        {
                            if (ipMsg is CNameRecord msg) name = msg.CanonicalName.ToString();
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        var ipMsg = QueryResolve.ResolveOverHttpsByDnsJson(IPAddress.Any.ToString(),
                            name, "https://1.0.0.1/dns-query", DnsSettings.ProxyEnable, DnsSettings.WProxy).list[0];
                        switch (ipMsg.RecordType)
                        {
                            case RecordType.A when ipMsg is ARecord msg1:
                                return msg1.Address;
                            case RecordType.CName:
                            {
                                if (ipMsg is CNameRecord msg) name = msg.CanonicalName.ToString();
                                break;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        MyTools.BackgroundLog(exception.ToString());
                        return IPAddress.Any;
                    }
                    MyTools.BackgroundLog(e.ToString());
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
                    countryCode = "";

                if (locStr.Contains("\"organization\""))
                    organization = json.AsObjectGetString("organization");
                else if (locStr.Contains("\"as\""))
                    organization = json.AsObjectGetString("as");
                else if (locStr.Contains("\"org\""))
                    organization = json.AsObjectGetString("org");
                else
                    organization = "";

                if (!organization.ToUpper().Contains("AS") && locStr.Contains("\"asn\""))
                {
                    try
                    {
                        organization = $"AS{json.AsObjectGetInt("asn")} {organization}";
                    }
                    catch
                    {
                        organization = $"AS{json.AsObjectGetString("asn")} {organization}";
                    }
                }

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
