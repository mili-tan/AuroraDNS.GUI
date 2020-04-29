using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ARSoft.Tools.Net.Dns;

namespace AuroraGUI.Tools
{
    // ReSharper disable once UnusedMember.Global
    internal class TcpFwder
    {
        private int LocalProt { get; }
        private IPAddress LocalIp { get; }
        private int TargetPort { get; }
        private IPAddress TargetIp { get; }
        public TcpFwder(IPAddress LocalIp, int LocalProt, IPAddress TargetIp, int TargetPort)
        {
            this.LocalIp = LocalIp;
            this.LocalProt = LocalProt;
            this.TargetIp = TargetIp;
            this.TargetPort = TargetPort;
        }

        // ReSharper disable once UnusedMember.Global
        public void Run()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(LocalIp, LocalProt));
            serverSocket.Listen(10000);
            new Thread(Listen).Start(serverSocket);
        }

        private void Listen(object obj)
        {
            Socket serverSocket = (Socket)obj;

            while (true)
            {
                Socket tcp1 = serverSocket.Accept();
                Socket tcp2 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                tcp2.Connect(new IPEndPoint(TargetIp, TargetPort));
                ThreadPool.QueueUserWorkItem(SwapMsg, new ThSock
                {
                    Tcp1 = tcp2,
                    Tcp2 = tcp1
                });
                ThreadPool.QueueUserWorkItem(SwapMsg, new ThSock
                {
                    Tcp1 = tcp1,
                    Tcp2 = tcp2
                });
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public void SwapMsg(object obj)
        {
            ThSock mSocket = (ThSock)obj;
            while (true)
            {
                try
                {
                    byte[] result = new byte[1024];
                    int num = mSocket.Tcp2.Receive(result, result.Length, SocketFlags.None);
                    if (num == 0) 
                    {
                        if (mSocket.Tcp1.Connected) mSocket.Tcp1.Close();
                        if (mSocket.Tcp2.Connected) mSocket.Tcp2.Close();
                        break;
                    }
                    mSocket.Tcp1.Send(result, num, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    if (mSocket.Tcp1.Connected) mSocket.Tcp1.Close();
                    if (mSocket.Tcp2.Connected) mSocket.Tcp2.Close();
                    break;
                }
            }
        }

        public class ThSock
        {
            public Socket Tcp1 { get; set; }
            public Socket Tcp2 { get; set; }
        }
    }

    internal class Fwder
    {
        private int LocalProt { get; }
        private IPAddress LocalIp { get; }
        private IPAddress TargetIp { get; }
        public Fwder(IPAddress LocalIp, int LocalProt, IPAddress TargetIp)
        {
            this.LocalIp = LocalIp;
            this.LocalProt = LocalProt;
            this.TargetIp = TargetIp;
        }

        public void Run()
        {
            var dns = new DnsServer(new IPEndPoint(LocalIp, LocalProt), 10, 10);
            dns.QueryReceived += OnDnsOnQueryReceived;
            Task.Run(dns.Start);
        }

        private async Task OnDnsOnQueryReceived(object sender, QueryReceivedEventArgs e)
        {
            try
            {
                if (!(e.Query is DnsMessage query)) return;
                e.Response = await new DnsClient(TargetIp, 1000).SendMessageAsync(query);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
