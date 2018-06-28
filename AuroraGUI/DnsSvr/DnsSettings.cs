using System.IO;
using System.Net;
using MojoUnity;

namespace AuroraGUI
{
    static class DnsSettings
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
            WhiteListEnable = configJson.AsObjectGetBool("WhiteList");
            ProxyEnable = configJson.AsObjectGetBool("ProxyEnable");
            EDnsCustomize = configJson.AsObjectGetBool("EDnsCustomize");
            DebugLog = configJson.AsObjectGetBool("DebugLog");
            HttpsDnsUrl = configJson.AsObjectGetString("HttpsDns");

            if (EDnsCustomize)
                EDnsIp = IPAddress.Parse(configJson.AsObjectGetString("EDnsClientIp"));
            if (ProxyEnable)
                WProxy = new WebProxy(configJson.AsObjectGetString("Proxy"));

        }
    }
}
