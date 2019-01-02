using System.Management;

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
