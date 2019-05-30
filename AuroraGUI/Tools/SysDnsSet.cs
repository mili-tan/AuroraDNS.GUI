using System;
using System.Diagnostics;
using System.Management;
using System.Net;
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
            foreach (var network in GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus == Up)
                {
                    new Process
                    {
                        StartInfo = new ProcessStartInfo("netsh.exe",
                            $"interface ip set dns \"{network.Name}\" source=static addr={IPAddress.Any}")
                    }.Start();

                    new Process
                    {
                        StartInfo = new ProcessStartInfo("netsh.exe",
                            $"interface ip add dns \"{network.Name}\" addr={IPAddress.Any}")
                    }.Start();
                }
            }
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
