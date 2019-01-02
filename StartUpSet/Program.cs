using System.Linq;

namespace StartUpSet
{
    static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Contains("unset"))
                AuroraGUI.Tools.StartUpSet.SetRunWithStart(false, args[1], args[2]);
            else if (args.Contains("set"))
                AuroraGUI.Tools.StartUpSet.SetRunWithStart(true, args[1], args[2]);
            else if (args.Contains("get"))
                AuroraGUI.Tools.StartUpSet.GetRunWithStart(args[1]);
        }
    }
}
