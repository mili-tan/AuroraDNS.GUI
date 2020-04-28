using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using AuroraGUI.DnsSvr;
using Microsoft.Win32;
using static System.Net.NetworkInformation.NetworkInterface;
using static System.Net.NetworkInformation.OperationalStatus;

#pragma warning disable IDE0044

namespace AuroraGUI.Tools
{
    static class SysDnsSet
    {
        public static void SetDns(string dnsAddr,string backupDnsAddr)
        {
            if (backupDnsAddr == IPAddress.Any.ToString()) backupDnsAddr = "";
            foreach (var network in GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus != Up) continue;
                RegistryKey reg = Registry.LocalMachine.CreateSubKey(
                    @"SYSTEM\ControlSet001\Services\Tcpip\Parameters\Interfaces\" + network.Id);
                try
                {
                    if (reg.GetValue("NameServer") != null) reg.SetValue("NameServer", $"{dnsAddr},{backupDnsAddr}");
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
        }

        public static void SetDnsCmd(string dnsAddr, string backupDnsAddr)
        {
            if (backupDnsAddr == IPAddress.Any.ToString()) backupDnsAddr = "";
            var cmd = "";

            foreach (var network in GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus == Up)
                {
                    cmd += $"netsh interface ip set dns \"{network.Name}\" source=static addr={dnsAddr} validate=no" + Environment.NewLine;
                    cmd += $"netsh interface ip add dns \"{network.Name}\" addr={backupDnsAddr} validate=no" + Environment.NewLine;
                    if ((Equals(DnsSettings.ListenIp, IPAddress.IPv6Loopback) ||
                         Equals(DnsSettings.ListenIp, IPAddress.IPv6Any)) && IPAddress.IsLoopback(IPAddress.Parse(dnsAddr)))
                    {
                        cmd +=
                            $"netsh interface ipv6 set dnsserver \"{network.Name}\" source=static addr=::1 validate=no" +
                            Environment.NewLine;
                        cmd +=
                            $"netsh interface ipv6 set dnsserver \"{network.Name}\" source=static addr=::ffff:{backupDnsAddr} validate=no" +
                            Environment.NewLine;
                    }
                }
            }

            var filename = Path.GetTempPath() + "setdns.cmd";
            File.Create(filename).Close();
            File.WriteAllText(filename, cmd, Encoding.Default);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = filename,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(startInfo).Exited += (o, args) => { File.Delete(filename); };
        }

        public static void ResetDns()
        {
            var backupDnsAddr = Equals(DnsSettings.SecondDnsIp, IPAddress.Any)
                ? "" : DnsSettings.SecondDnsIp.ToString();
            foreach (var network in GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus != Up) continue;
                RegistryKey reg = Registry.LocalMachine.CreateSubKey(
                    @"SYSTEM\ControlSet001\Services\Tcpip\Parameters\Interfaces\" + network.Id);
                try
                {
                    if (reg.GetValue("NameServer") != null) reg.SetValue("NameServer", "");
                    if (!network.GetIPProperties().GetIPv4Properties().IsDhcpEnabled)
                        reg.SetValue("NameServer", backupDnsAddr);
                }
                catch (NetworkInformationException e)
                {
                    MyTools.BackgroundLog(network.Name + e);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
        }

        public static void ResetDnsCmd()
        {
            var backupDnsAddr = Equals(DnsSettings.SecondDnsIp, IPAddress.Any) 
                ? "" : DnsSettings.SecondDnsIp.ToString();
            var cmd = "";

            foreach (var network in GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus != Up) continue;
                cmd += $"netsh interface ipv6 delete dns \"{network.Name}\" all" + Environment.NewLine;
                cmd += $"netsh interface ip delete dns \"{network.Name}\" all" + Environment.NewLine;
                cmd += $"netsh interface ip set dns \"{network.Name}\" source=dhcp" + Environment.NewLine;
                try
                {
                    if (!network.GetIPProperties().GetIPv4Properties().IsDhcpEnabled)
                        cmd +=
                            $"netsh interface ip set dns \"{network.Name}\" source=static addr={backupDnsAddr} validate=no" +
                            Environment.NewLine;
                }
                catch (Exception e)
                {
                    MyTools.BackgroundLog(network.Name + e);
                }
            }

            var filename = Path.GetTempPath() + "setdns.cmd";
            File.Create(filename).Close();
            File.WriteAllText(filename, cmd, Encoding.Default);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = filename,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(startInfo).Exited += (o, args) => { File.Delete(filename); };
        }

    }
}
