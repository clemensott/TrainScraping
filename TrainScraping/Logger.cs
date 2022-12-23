using System;
using System.IO;

namespace TrainScraping
{
    class Logger
    {
        private static string path = Path.Combine(AppContext.BaseDirectory, "train_scraping.log");

        public static void Log(string text)
        {
            try
            {
                File.AppendAllText(path, text + "\r\n");
            }
            catch { }
        }
    }
}
