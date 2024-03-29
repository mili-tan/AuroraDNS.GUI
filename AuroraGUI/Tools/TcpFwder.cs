﻿using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ARSoft.Tools.Net.Dns;
using AuroraGUI.DnsSvr;

namespace AuroraGUI.Tools
{
    // ReSharper disable once UnusedMember.Global

    internal class IPv4Listener
    {
        private int LocalProt { get; }
        private IPAddress LocalIp { get; }

        public static DnsServer DNS;
        public static bool Running;

        public IPv4Listener(IPAddress LocalIp, int LocalProt)
        {
            this.LocalIp = LocalIp;
            this.LocalProt = LocalProt;
        }

        public void Run()
        {
            DNS = new DnsServer(new IPEndPoint(LocalIp, LocalProt), 8, 8);
            DNS.QueryReceived += QueryResolve.ServerOnQueryReceived;
            var bgw = new BackgroundWorker();
            bgw.DoWork += (o, args) => DNS.Start();
            bgw.RunWorkerAsync();
            Running = true;
        }
    }
}
