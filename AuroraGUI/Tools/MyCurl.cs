using System;
using System.Net;

namespace AuroraGUI.Tools
{
    class MyCurl
    {
        public class MWebClient : WebClient
        {
            public bool AllowAutoRedirect { get; set; } = false;
            public int TimeOut { get; set; } = 15000;
            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                request.Timeout = TimeOut;
                if (request is HttpWebRequest webRequest)
                    webRequest.AllowAutoRedirect = AllowAutoRedirect;
                return request;
            }
        }

        public static string GetByMWebClient(string url, bool proxyEnable = false, IWebProxy wProxy = null)
        {
            MWebClient mWebClient = new MWebClient { Headers = { ["User-Agent"] = "AuroraDNSC/0.1" } };
            //if (bool) webClient.AllowAutoRedirect = false;
            if (proxyEnable) mWebClient.Proxy = wProxy;
            return mWebClient.DownloadString(url);
        }
    }
}
