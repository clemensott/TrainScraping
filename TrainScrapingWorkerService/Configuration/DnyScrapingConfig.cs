namespace TrainScrapingWorkerService.Configuration
{
    public class DnyScrapingConfig
    {
        public int IntervalSeconds { get; set; }

        public string DownloadFolder { get; set; } = string.Empty;

        public string ArchiveFolder { get; set; } = string.Empty;

        public string ErrorFolder { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = string.Empty;

        public string HttpMethod { get; set; } = string.Empty;

        public DnyHeaderConfig[] Headers { get; set; } = new DnyHeaderConfig[0];

        public DnySearchParamConfig[] SearchParams { get; set; } = new DnySearchParamConfig[0];
    }
}
