using System.Linq;

namespace SysDnsSet
{
    static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Contains("reset"))
                AuroraGUI.Tools.SysDnsSet.ResetDns();
            else if (args.Contains("set"))
                AuroraGUI.Tools.SysDnsSet.SetDns(args[1], args[2]);
        }
    }
}
