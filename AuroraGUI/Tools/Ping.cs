using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;

namespace AuroraGUI.Tools
{
    static class Ping
    {
        public static List<int> Tcping(string ip,int port)
        {
            var times = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                var socks = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    { Blocking = true, ReceiveTimeout = 500, SendTimeout = 500 };
                IPEndPoint point;
                try
                {
                    point = new IPEndPoint(IPAddress.Parse(ip), port);
                }
                catch
                {
                    point = new IPEndPoint(Dns.GetHostAddresses(ip)[0], port);
                }
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                try
                {
                    var result = socks.BeginConnect(point, null, null);
                    if (!result.AsyncWaitHandle.WaitOne(500, true)) continue;
                }
                catch
                {
                    continue;
                }
                stopWatch.Stop();
                times.Add(Convert.ToInt32(stopWatch.Elapsed.TotalMilliseconds));
                socks.Close();
                Thread.Sleep(50);
            }
            if (times.Count == 0) times.Add(0);
            return times;
        }

        public static List<int> MPing(string ipStr)
        {
            var ping = new System.Net.NetworkInformation.Ping();
            var bufferBytes = Encoding.Default.GetBytes("abcdefghijklmnopqrstuvwabcdefghi");

            var times = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                times.Add(Convert.ToInt32(ping.Send(ipStr, 120, bufferBytes).RoundtripTime));
                Thread.Sleep(50);
            }

            return times;
        }

        public static List<int> DnsTest(string ipStr)
        {
            var times = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                try
                {
                    var msg = new DnsClient(IPAddress.Parse(ipStr), 500).Resolve(DomainName.Parse("example.com"));
                    if (msg == null || msg.ReturnCode != ReturnCode.NoError) continue;
                }
                catch
                {
                    continue;
                }

                stopWatch.Stop();
                var time = Convert.ToInt32(stopWatch.Elapsed.TotalMilliseconds);
                times.Add(time);
                Thread.Sleep(100);
            }
            if (times.Count == 0) times.Add(0);
            return times;
        }

        public static List<int> Curl(string urlStr,string name)
        {
            var webClient = new MyCurl.MWebClient() { TimeOut = 600 };
            var times = new List<int>();
            var ok = true;
            for (int i = 0; i < 4; i++)
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                try
                {
                    webClient.DownloadString(urlStr + $"?ct=application/dns-json&name={name}&type=A");
                }
                catch
                {
                    ok = false;
                }
                stopWatch.Stop();
                if (ok) times.Add(Convert.ToInt32(stopWatch.Elapsed.TotalMilliseconds));
                Thread.Sleep(50);
            }
            if (times.Count == 0) times.Add(0);
            return times;
        }
    }
}
