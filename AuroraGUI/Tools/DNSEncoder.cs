using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ARSoft.Tools.Net.Dns;

namespace AuroraGUI.Tools
{
    class DNSEncoder
    {
        private static MethodInfo info;

        public static byte[] Encode(DnsMessage dnsQMsg)
        {
            var args = new object[] {false, null};
            if (info == null)
                foreach (var mInfo in dnsQMsg.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                    if (mInfo.ToString() == "Int32 Encode(Boolean, Byte[] ByRef)")
                        info = mInfo;
            info.Invoke(dnsQMsg, args);

            if ((args[1] as byte[])[2] == 0) (args[1] as byte[])[2] = 1;
            return args[1] as byte[];
        }
    }
}
