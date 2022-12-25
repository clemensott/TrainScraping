using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using TrainScraping.Configuration;
using TrainScrapingCommon.Models;

namespace TrainScraping
{
    class DnyUploader : IDisposable
    {
        private readonly Config config;
        private readonly API api;
        private readonly Timer timer;

        public DnyUploader(Config config)
        {
            this.config = config;

            api = new API(config.ApiBaseUrl, config.ApiToken);

            timer = new Timer();
            timer.Interval = config.DnyUploadIntervalSeconds * 1000;
            timer.Elapsed += Timer_Elapsed;
        }

        public void StartTimer()
        {
            if (timer.Interval > 0)
            {
                timer.Start();
            }
        }

        private string GetFirstFile(DnyScrapingConfig config)
        {
            return Directory.EnumerateFiles(config.DownloadFolder).Where(path => path.EndsWith(".json")).FirstOrDefault();
        }

        private static DateTime ParseTimestamp(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string[] parts = fileName.Split('T');

            string[] dateParts = parts[0].Split('-');
            int year = int.Parse(dateParts[0]);
            int month = int.Parse(dateParts[1]);
            int day = int.Parse(dateParts[2]);

            string[] timeParts = parts[1].Split('-');
            int hour = int.Parse(timeParts[0]);
            int minute = int.Parse(timeParts[1]);
            int second = int.Parse(timeParts[2]);

            return new DateTime(year, month, day, hour, minute, second);
        }

        private static void MoveFile(string file, string destFolder)
        {
            try
            {
                Directory.CreateDirectory(destFolder);
                string destPath = Path.Combine(destFolder, Path.GetFileName(file));
                File.Move(file, destPath);
            }
            catch { }
        }

        public void MoveFileToArchive(DnyScrapingConfig config, string file)
        {
            Logger.Log($"DnyUploader:MoveFileToArchive:{file}");
            MoveFile(file, config.ArchiveFolder);
        }

        public void MoveFileToError(DnyScrapingConfig config, string file)
        {
            Logger.Log($"DnyUploader:MoveFileToError:{file}");
            MoveFile(file, config.ErrorFolder);
        }

        public async Task UploadOne()
        {
            if (!await api.Ping())
            {
                Logger.Log("DnyUploader:UploadOne:api_not_reachable");
                return;
            }

            foreach (DnyScrapingConfig dnyConfig in config.DNYs)
            {
                string file = null;
                try
                {
                    file = GetFirstFile(dnyConfig);
                    if (file == null) continue;

                    Logger.Log($"DnyUploader:UploadOne:file:{file}");

                    DateTime timestamp = ParseTimestamp(file);
                    string json = File.ReadAllText(file);
                    Dny dny = JsonConvert.DeserializeObject<Dny>(json);

                    if (await api.PostDny(dny, timestamp)) MoveFileToArchive(dnyConfig, file);
                    else MoveFileToError(dnyConfig, file);
                    break;
                }
                catch (Exception exc)
                {
                    Logger.Log(exc.ToString());
                    if (file != null)
                    {
                        MoveFileToError(dnyConfig, file);
                    }
                }
            }
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await UploadOne();
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Dispose();

            api.Dispose();
        }
    }
}
