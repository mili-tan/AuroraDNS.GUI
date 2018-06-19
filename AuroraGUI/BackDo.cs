using System;
using System.ComponentModel;
using System.IO;

namespace AuroraGUI
{
    static class BackDo
    {
        public static void BgwLog(string log)
        {
            using (BackgroundWorker worker = new BackgroundWorker())
            {
                worker.DoWork += (o, ea) =>
                {
                    if (!Directory.Exists("Log"))
                    {
                        Directory.CreateDirectory("Log");
                    }
                    File.AppendAllText($"./Log/{DateTime.Today.Year}{DateTime.Today.Month}{DateTime.Today.Day}.log", log + Environment.NewLine);
                };

                worker.RunWorkerAsync();
            }
        }

    }
}
