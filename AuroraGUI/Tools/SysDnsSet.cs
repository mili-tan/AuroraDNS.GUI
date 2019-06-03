using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;
using static System.Net.NetworkInformation.NetworkInterface;
using static System.Net.NetworkInformation.OperationalStatus;

#pragma warning disable IDE0044

namespace AuroraGUI.Tools
{
    static class SysDnsSet
    {
        private static readonly ManagementClass MgClass = new ManagementClass("Win32_NetworkAdapterConfiguration"); 
        private static readonly ManagementObjectCollection MgCollection = MgClass.GetInstances();

        public static void SetDns(string dnsAddr,string backupDnsAddr)
        {
            foreach (var item in MgCollection)
            {
                var mgObjItem = (ManagementObject)item;
                if (!(bool)mgObjItem["IPEnabled"])
                    continue;

                var parameters = mgObjItem.GetMethodParameters("SetDNSServerSearchOrder");
                parameters["DNSServerSearchOrder"] = new[] { dnsAddr, backupDnsAddr };
                mgObjItem.InvokeMethod("SetDNSServerSearchOrder", parameters, null);
                break;
            }
        }

        public static void SetDnsCmd(string dnsAddr, string backupDnsAddr)
        {
            var cmd = "";

            foreach (var network in GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus == Up)
                {
                    cmd += $"netsh interface ip set dns \"{network.Name}\" source=static addr={dnsAddr}" + Environment.NewLine;
                    cmd += $"netsh interface ip add dns \"{network.Name}\" addr={backupDnsAddr}" + Environment.NewLine;
                }
            }
            File.Create("setdns.cmd").Close();
            File.WriteAllText("setdns.cmd", cmd, Encoding.Default);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "setdns.cmd",
                Verb = "runas",
                CreateNoWindow = true
            };
            Process.Start(startInfo).Exited += (o, args) => { File.Delete("setdns.cmd"); };
        }

        public static void ResetDns()
        {
            foreach (var item in MgCollection)
            {
                var mgObjItem = (ManagementObject)item;
                if (!(bool)mgObjItem["IPEnabled"])
                    continue;

                mgObjItem.InvokeMethod("SetDNSServerSearchOrder", null);
                break;
            }
        }
    }
}
