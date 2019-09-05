using System.Diagnostics;
using Microsoft.Win32;

namespace AuroraGUI.Tools
{
    class UrlReg
    {
        public static void Reg(string urlLink)
        {
            using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + urlLink))
            {
                string applicationLocation = Process.GetCurrentProcess().MainModule.FileName;

                key.SetValue("", "URL:" + urlLink);
                key.SetValue("URL Protocol", "");

                using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                {
                    defaultIcon.SetValue("", applicationLocation + ",1");
                }

                using (var commandKey = key.CreateSubKey(@"shell\open\command"))
                {
                    commandKey.SetValue("", "\"" + applicationLocation + "\" \"%1\"");
                }

                key.Close();
            }
        }
        public static void UnReg(string UrlLink)
        {
            Registry.CurrentUser.DeleteSubKeyTree("SOFTWARE\\Classes\\" + UrlLink);
        }
    }
}
