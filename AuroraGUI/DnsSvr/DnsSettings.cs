using System;
using System.IO;
using System.Linq;
using System.Net;
using ARSoft.Tools.Net;
using MojoUnity;

namespace AuroraGUI
{
    class DnsSettings
    {
        public static string HttpsDnsUrl = "https://1.0.0.1/dns-query";
        public static IPAddress ListenIp = IPAddress.Loopback;
        public static IPAddress EDnsIp = IPAddress.Any;
        public static IPAddress SecondDnsIp = IPAddress.Parse("1.1.1.1");
        public static bool EDnsCustomize = false;
        public static bool ProxyEnable  = false;
        public static bool DebugLog = false;
        public static bool BlackListEnable  = false;
        public static bool WhiteListEnable  = false;
        public static WebProxy WProxy = new WebProxy("127.0.0.1:1080");

        public static void ReadConfig(string path)
        {
            JsonValue configJson = Json.Parse(File.ReadAllText(path));

            try
            {
                SecondDnsIp = IPAddress.Parse(configJson.AsObjectGetString("SecondDns"));
            }
            catch
            {
                SecondDnsIp = IPAddress.Parse("1.1.1.1");
            }
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
            QueryResolve.BlackList = Array.ConvertAll(blackListStrs, DomainName.Parse).ToList();
        }

        public static void ReadWhiteList(string path = "white.list")
        {
            string[] whiteListStrs = File.ReadAllLines(path);
            QueryResolve.WhiteList = whiteListStrs.Select(
                itemStr => itemStr.Split(' ', ',', '\t')).ToDictionary(
                whiteSplit => DomainName.Parse(whiteSplit[1]),
                whiteSplit => IPAddress.Parse(whiteSplit[0]));
        }
    }
}
