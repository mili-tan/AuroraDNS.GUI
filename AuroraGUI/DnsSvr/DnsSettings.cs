using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ARSoft.Tools.Net;
using MojoUnity;

namespace AuroraGUI.DnsSvr
{
    class DnsSettings
    {
        public static List<DomainName> BlackList;
        public static Dictionary<DomainName, string> WhiteList;

        public static string HttpsDnsUrl = "https://dns.cloudflare.com/dns-query";
        public static string SecondHttpsDnsUrl = "https://1.0.0.1/dns-query";
        public static IPAddress ListenIp = IPAddress.Loopback;
        public static IPAddress EDnsIp = IPAddress.Any;
        public static IPAddress SecondDnsIp = IPAddress.Parse("1.1.1.1");
        public static bool EDnsCustomize = false;
        public static bool ProxyEnable  = false;
        public static bool DebugLog = false;
        public static bool BlackListEnable  = false;
        public static bool WhiteListEnable  = false;
        public static bool ViaDnsMsg = false;
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
                ViaDnsMsg = configJson.AsObjectGetBool("EnableDnsMessage");

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

        public static void ReadWhiteList(string path = "white.list")
        {
            string[] whiteListStrs = File.ReadAllLines(path);
            WhiteList = whiteListStrs.Select(
                itemStr => itemStr.Split(' ', ',', '\t')).ToDictionary(
                whiteSplit => DomainName.Parse(whiteSplit[1]),
                whiteSplit => whiteSplit[0]);
        }
    }

    class UrlSettings
    {
        public static string GeoIpApi = "https://api.ip.sb/geoip/";
        public static string WhatMyIpApi = "http://whatismyip.akamai.com/";
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
