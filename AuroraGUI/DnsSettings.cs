using System.Net;

namespace AuroraGUI
{
    class DnsSettings
    {
        public static string HttpsDnsUrl = "https://1.0.0.1/dns-query";
        public static IPAddress ListenIp = IPAddress.Loopback;
        public static IPAddress EDnsIp = IPAddress.Any;
        public static bool EDnsPrivacy  = false;
        public static bool ProxyEnable  = false;
        public static bool DebugLog = false;
        public static bool BlackListEnable  = false;
        public static bool WhiteListEnable  = false;
        public static WebProxy WProxy = new WebProxy("127.0.0.1:1080");
    }
}
