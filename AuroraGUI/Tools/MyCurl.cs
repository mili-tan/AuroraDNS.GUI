using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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
                {
                    webRequest.AllowAutoRedirect = AllowAutoRedirect;
                    webRequest.KeepAlive = true;
                }

                return request;
            }
        }

        public class Http2Handler : WinHttpHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                request.Version = new Version("2.0");
                return base.SendAsync(request, cancellationToken);
            }
        }

        public static string GetByMWebClient(string url, bool proxyEnable = false, IWebProxy wProxy = null)
        {
            MWebClient mWebClient = new MWebClient { Headers = { ["User-Agent"] = "AuroraDNSC/0.1"} };
            //if (bool) webClient.AllowAutoRedirect = false;
            if (proxyEnable) mWebClient.Proxy = wProxy;
            return mWebClient.DownloadString(url);
        }

        public static string GetByHttp2Client(string url, bool proxyEnable = false, IWebProxy wProxy = null)
        {
            var mHttp2Handel = new Http2Handler {WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy};
            if (proxyEnable) mHttp2Handel.Proxy = wProxy;
            HttpClient mHttpClient = new HttpClient(mHttp2Handel);
            mHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AuroraDNSC/0.1");
            mHttpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
            return mHttpClient.GetStringAsync(url).Result;
        }
    }
}
