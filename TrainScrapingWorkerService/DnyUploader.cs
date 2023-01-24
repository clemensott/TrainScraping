using Newtonsoft.Json;
using TrainScrapingWorkerService.Configuration;
using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingWorkerService
{
    class DnyUploader : RegularTask
    {
        private readonly Config config;
        private readonly API api;
        private readonly ILogger logger;

        public DnyUploader(Config config, ILogger logger) : base(TimeSpan.FromSeconds(config.DnyUploadIntervalSeconds))
        {
            this.config = config;
            this.logger = logger;

            api = new API(config.ApiBaseUrl, config.ApiToken);
        }

        private static string? GetFirstFile(DnyScrapingConfig config)
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
            logger.LogInformation($"DnyUploader:MoveFileToArchive:{file}");
            MoveFile(file, config.ArchiveFolder);
        }

        public void MoveFileToError(DnyScrapingConfig config, string file)
        {
            logger.LogInformation($"DnyUploader:MoveFileToError:{file}");
            MoveFile(file, config.ErrorFolder);
        }

        public override async Task Execute()
        {
            if (!await api.Ping())
            {
                logger.LogInformation("DnyUploader:UploadOne:api_not_reachable");
                return;
            }

            foreach (DnyScrapingConfig dnyConfig in config.DNYs)
            {
                string? file = null;
                try
                {
                    file = GetFirstFile(dnyConfig);
                    if (file == null) continue;

                    logger.LogInformation($"DnyUploader:UploadOne:file:{file}");

                    DateTime timestamp = ParseTimestamp(file);
                    string json = File.ReadAllText(file);
                    Dny? dny = JsonConvert.DeserializeObject<Dny>(json);

                    if (dny != null && await api.PostDny(dny, timestamp)) MoveFileToArchive(dnyConfig, file);
                    else MoveFileToError(dnyConfig, file);
                    break;
                }
                catch (Exception exc)
                {
                    logger.LogInformation(exc.ToString());
                    if (file != null)
                    {
                        MoveFileToError(dnyConfig, file);
                    }
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            api.Dispose();
        }
    }
}
