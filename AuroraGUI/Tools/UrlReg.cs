using System.Diagnostics;
using Microsoft.Win32;

namespace AuroraGUI.Tools
{
    class UrlReg
    {
        public static void Reg(string UrlLink, string FriendlyName)
        {
            using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + UrlLink))
            {
                string applicationLocation = Process.GetCurrentProcess().MainModule.FileName;

                key.SetValue("", "URL:" + FriendlyName);
                key.SetValue("URL Protocol", "");

                using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                {
                    defaultIcon.SetValue("", applicationLocation + ",1");
                }

                using (var commandKey = key.CreateSubKey(@"shell\open\command"))
                {
                    commandKey.SetValue("", "\"" + applicationLocation + "\" \"%1\"");
                }
            }
        }
        public static void UnReg(string UrlLink)
        {
            Registry.CurrentUser.DeleteSubKeyTree("SOFTWARE\\Classes\\" + UrlLink);
        }
    }
}
