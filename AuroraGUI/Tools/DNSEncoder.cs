using System;
using System.Reflection;
using System.Threading.Tasks;
using ARSoft.Tools.Net.Dns;

namespace AuroraGUI.Tools
{
    internal static class DNSEncoder
    {
        private static MethodInfo info;

        public static byte[] Encode(DnsMessage dnsQMsg)
        {
            dnsQMsg.IsRecursionAllowed = true;
            dnsQMsg.IsRecursionDesired = true;
            dnsQMsg.TransactionID = Convert.ToUInt16(new Random(DateTime.Now.Millisecond).Next(1, 10));
            var args = new object[] {false, null};
            if (info == null)
                Parallel.ForEach(new DnsMessage().GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic), mInfo =>
                {
                    if (mInfo.ToString() == "Int32 Encode(Boolean, Byte[] ByRef)")
                        info = mInfo;
                });
            info.Invoke(dnsQMsg, args);
            //var dnsBytes = args[1] as byte[];
            //if (dnsBytes[2] == 0) dnsBytes[2] = 1;
            return args[1] as byte[];
        }
    }
}
