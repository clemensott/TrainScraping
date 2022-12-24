using System;
using System.Collections.Generic;

namespace TrainScraping.Configuration
{
    public class DnyScrapingConfig
    {
        public int IntervalSeconds { get; set; }

        public string DownloadFolder { get; set; }

        public string BaseUrl { get; set; }

        public string HttpMethod { get; set; }

        public DnyHeaderConfig[] Headers { get; set; }

        public DnySearchParamConfig[] SearchParams { get; set; }
}
}
