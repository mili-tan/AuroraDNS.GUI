using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ARSoft.Tools.Net;
using AuroraGUI.Tools;
using MojoUnity;

namespace AuroraGUI.DnsSvr
{
    class DnsSettings
    {
        public static List<DomainName> BlackList;
        public static List<DomainName> ChinaList;
        public static Dictionary<DomainName, string> WhiteList = new Dictionary<DomainName, string>();

        public static string HttpsDnsUrl = "https://dns.cloudflare.com/dns-query";
        public static string SecondHttpsDnsUrl = "https://1.0.0.1/dns-query";
        public static IPAddress ListenIp = IPAddress.Loopback;
        public static int ListenPort = 53;
        public static IPAddress EDnsIp = IPAddress.Any;
        public static IPAddress SecondDnsIp = IPAddress.Parse("1.1.1.1");
        public static bool EDnsCustomize = false;
        public static bool ProxyEnable  = false;
        public static bool DebugLog = false;
        public static bool BlackListEnable  = false;
        public static bool WhiteListEnable  = false;
        public static bool ChinaListEnable = false;
        public static bool DnsMsgEnable = false;
        public static bool DnsCacheEnable = false;
        public static bool Http2Enable = false;
        public static bool AutoCleanLogEnable = false;
        public static bool Ipv6Disable = false;
        public static bool Ipv4Disable = false;
        public static bool StartupOverDoH = false;
        public static bool AllowSelfSignedCert = false;
        public static bool AllowAutoRedirect = true;
        public static bool HTTPStatusNotify = false;
        public static bool TtlRewrite = false;
        public static int TtlMinTime = 300;
        public static WebProxy WProxy = new WebProxy("127.0.0.1:1080");

        public static void ReadConfig(string path)
        {
            string configStr = File.ReadAllText(path);
            JsonValue configJson = Json.Parse(configStr);

            if (configStr.Contains("\"SecondDns\""))
                SecondDnsIp = IPAddress.Parse(configJson.AsObjectGetString("SecondDns"));
            if (configStr.Contains("\"SecondHttpsDns\""))
                SecondHttpsDnsUrl = configJson.AsObjectGetString("SecondHttpsDns");
            if (configStr.Contains("\"EnableDnsMessage\""))
                DnsMsgEnable = configJson.AsObjectGetBool("EnableDnsMessage");
            if (configStr.Contains("\"EnableDnsCache\""))
                DnsCacheEnable = configJson.AsObjectGetBool("EnableDnsCache");
            if (configStr.Contains("\"EnableHttp2\""))
                Http2Enable = configJson.AsObjectGetBool("EnableHttp2");
            if (configStr.Contains("\"EnableAutoCleanLog\""))
                AutoCleanLogEnable = configJson.AsObjectGetBool("EnableAutoCleanLog");
            if (configStr.Contains("\"Port\""))
                ListenPort = configJson.AsObjectGetInt("Port", 53);
            if (configStr.Contains("\"ChinaList\""))
                ChinaListEnable = configJson.AsObjectGetBool("ChinaList");
            if (configStr.Contains("\"StartupOverDoH\""))
                StartupOverDoH = configJson.AsObjectGetBool("StartupOverDoH");
            if (configStr.Contains("\"AllowSelfSignedCert\""))
                AllowSelfSignedCert = configJson.AsObjectGetBool("AllowSelfSignedCert");
            if (configStr.Contains("\"AllowAutoRedirect\""))
                AllowAutoRedirect = configJson.AsObjectGetBool("AllowAutoRedirect");
            if (configStr.Contains("\"HTTPStatusNotify\""))
                HTTPStatusNotify = configJson.AsObjectGetBool("HTTPStatusNotify");

            if (configStr.Contains("\"Ipv6Disable\""))
                Ipv6Disable = configJson.AsObjectGetBool("Ipv6Disable");
            if (configStr.Contains("\"Ipv4Disable\""))
                Ipv4Disable = configJson.AsObjectGetBool("Ipv4Disable");

            if (configStr.Contains("\"TTLRewrite\""))
                TtlRewrite = configJson.AsObjectGetBool("TTLRewrite");
            if (configStr.Contains("\"TTLMinTime\""))
                TtlMinTime = configJson.AsObjectGetInt("TTLMinTime");

            ListenIp = IPAddress.Parse(configJson.AsObjectGetString("Listen"));
            BlackListEnable = configJson.AsObjectGetBool("BlackList");
            WhiteListEnable = configJson.AsObjectGetBool("RewriteList");
            ProxyEnable = configJson.AsObjectGetBool("ProxyEnable");
            EDnsCustomize = configJson.AsObjectGetBool("EDnsCustomize");
            DebugLog = configJson.AsObjectGetBool("DebugLog");
            HttpsDnsUrl = configJson.AsObjectGetString("HttpsDns").Trim();

            if (EDnsCustomize)
                EDnsIp = IPAddress.Parse(configJson.AsObjectGetString("EDnsClientIp"));
            if (ProxyEnable)
                WProxy = new WebProxy(configJson.AsObjectGetString("Proxy"));
        }

        public static void ReadBlackList(string path = "black.list")
        {
            string[] blackListStrs = File.ReadAllLines(path);
            BlackList = Array.ConvertAll(blackListStrs, DomainName.Parse).ToList();
        }

        public static void ReadChinaList(string path = "china.list")
        {
            string[] chinaListStrs = File.ReadAllLines(path);
            ChinaList = Array.ConvertAll(chinaListStrs, DomainName.Parse).ToList();
        }

        public static void ReadWhiteList(string path = "white.list")
        {
            string[] whiteListStrs = File.ReadAllLines(path);
            foreach (var itemStr in whiteListStrs)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(itemStr))
                    {
                        var strings = itemStr.Split(' ', ',', '\t');
                        if (!WhiteList.ContainsKey(DomainName.Parse(strings[1])))
                            WhiteList.Add(DomainName.Parse(strings[1]), strings[0]);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static void ReadWhiteListWeb(string webUrl)
        {
            try
            {
                string[] whiteListStrs = new WebClient().DownloadString(webUrl).Split('\n');
                foreach (var itemStr in whiteListStrs)
                {
                    var strings = itemStr.Split(' ', ',', '\t');
                    try
                    {
                        if (!WhiteList.ContainsKey(DomainName.Parse(strings[1])))
                            WhiteList.Add(DomainName.Parse(strings[1]), strings[0]);
                    }
                    catch (Exception e)
                    {
                        MyTools.BackgroundLog(e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                MyTools.BackgroundLog(e.ToString());
            }
        }

        public static void ReadWhiteListSubscribe(string path)
        {
            string[] whiteListSubStrs = File.ReadAllLines(path);
            foreach (var item in whiteListSubStrs)
            {
                if (item.ToLower().Contains("http://") || item.ToLower().Contains("https://"))
                    ReadWhiteListWeb(item);
            }
        }
    }

    class UrlSettings
    {
        public static string GeoIpApi = "https://api.ip.sb/geoip/";
        public static string WhatMyIpApi = "https://api.ipify.org/";
        public static string MDnsList = "https://cdn.jsdelivr.net/gh/mili-tan/AuroraDNS.GUI/List/DNS.list";
        public static string MDohList = "https://cdn.jsdelivr.net/gh/mili-tan/AuroraDNS.GUI/List/DoH.list";

        public static void ReadConfig(string path)
        {
            string configStr = File.ReadAllText(path);
            JsonValue configJson = Json.Parse(configStr);

            if (configStr.Contains("\"GeoIPAPI\""))
                GeoIpApi = configJson.AsObjectGetString("GeoIPAPI");
            if (configStr.Contains("\"WhatMyIPAPI\""))
                WhatMyIpApi = configJson.AsObjectGetString("WhatMyIPAPI");
            if (configStr.Contains("\"DNSList\""))
                MDnsList = configJson.AsObjectGetString("DNSList");
            if (configStr.Contains("\"DoHList\""))
                MDohList = configJson.AsObjectGetString("DoHList");
        }
    }
}
