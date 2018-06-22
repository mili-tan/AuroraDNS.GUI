using System.Management;
#pragma warning disable IDE0044

namespace AuroraGUI
{
    static class SysDnsSet
    {
        private static ManagementClass mgClass = new ManagementClass("Win32_NetworkAdapterConfiguration"); 
        private static ManagementObjectCollection mgCollection = mgClass.GetInstances();
        public static void SetDns(string dnsAddr,string backupDnsAddr)
        {
            foreach (var item in mgCollection)
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
            foreach (var item in mgCollection)
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
