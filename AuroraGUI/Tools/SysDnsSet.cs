using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;
using System.Windows;
using Microsoft.Win32;
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
            var cmd = "";

            foreach (var network in GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus == Up)
                {
                    cmd += $"netsh interface ip set dns \"{network.Name}\" source=static addr={dnsAddr} validate=no" + Environment.NewLine;
                    cmd += $"netsh interface ip add dns \"{network.Name}\" addr={backupDnsAddr} validate=no" + Environment.NewLine;
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
            foreach (var item in MgCollection)
            {
                var mgObjItem = (ManagementObject)item;
                if (!(bool)mgObjItem["IPEnabled"])
                    continue;

                mgObjItem.InvokeMethod("SetDNSServerSearchOrder", null);
                break;
            }
        }

        public static void ResetDnsCmd()
        {
            var cmd = "";

            foreach (var network in GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus == Up)
                {
                    cmd += $"netsh interface ip set dns \"{network.Name}\" source=dhcp" + Environment.NewLine;
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
