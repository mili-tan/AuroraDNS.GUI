using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AuroraGUI.Tools
{
    class TcpFwder
    {
        int LocalProt { get; }
        IPAddress LocalIp { get; }
        int TargetPort { get; }
        IPAddress TargetIp { get; }
        public TcpFwder(IPAddress LocalIp, int LocalProt, IPAddress TargetIp, int TargetPort)
        {
            this.LocalIp = LocalIp;
            this.LocalProt = LocalProt;
            this.TargetIp = TargetIp;
            this.TargetPort = TargetPort;
        }

        public void Run()
        {
            //服务器IP地址  
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(LocalIp, LocalProt));
            serverSocket.Listen(10000);
            new Thread(Listen).Start(serverSocket);
        }

        //监听客户端连接
        private void Listen(object obj)
        {
            Socket serverSocket = (Socket)obj;

            while (true)
            {
                Socket tcp1 = serverSocket.Accept();
                Socket tcp2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                tcp2.Connect(new IPEndPoint(TargetIp, TargetPort));
                //目标主机返回数据
                ThreadPool.QueueUserWorkItem(SwapMsg, new thSock
                {
                    tcp1 = tcp2,
                    tcp2 = tcp1
                });
                //中间主机请求数据
                ThreadPool.QueueUserWorkItem(SwapMsg, new thSock
                {
                    tcp1 = tcp1,
                    tcp2 = tcp2
                });
            }
        }

        ///两个 tcp 连接 交换数据，一发一收
        public void SwapMsg(object obj)
        {
            thSock mSocket = (thSock)obj;
            while (true)
            {
                try
                {
                    byte[] result = new byte[1024];
                    int num = mSocket.tcp2.Receive(result, result.Length, SocketFlags.None);
                    if (num == 0) //接受空包关闭连接
                    {
                        if (mSocket.tcp1.Connected) mSocket.tcp1.Close();
                        if (mSocket.tcp2.Connected) mSocket.tcp2.Close();
                        break;
                    }
                    mSocket.tcp1.Send(result, num, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    if (mSocket.tcp1.Connected) mSocket.tcp1.Close();
                    if (mSocket.tcp2.Connected) mSocket.tcp2.Close();
                    break;
                }
            }
        }

        public class thSock
        {
            public Socket tcp1 { get; set; }
            public Socket tcp2 { get; set; }
        }
    }
}
