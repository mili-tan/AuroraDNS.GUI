using Microsoft.Win32;

namespace AuroraGUI.Tools
{
    class StartUpSet
    {
        public static void SetRunWithStart(bool started, string name, string path)
        {
            RegistryKey reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            if (started)
            {
                reg.SetValue(name, path);
            }
            else
            {
                reg.DeleteValue(name);
            }
        }

        public static bool GetRunWithStart(string name)
        {
            RegistryKey reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            try
            {
                return !string.IsNullOrWhiteSpace(reg.GetValue(name).ToString());
            }
            catch
            {
                return false;
            }
        }
    }
}
