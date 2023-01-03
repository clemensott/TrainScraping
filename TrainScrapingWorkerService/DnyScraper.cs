using System.Net;
using System.Net.Http.Headers;
using System.Text;
using TrainScrapingWorkerService.Configuration;

namespace TrainScrapingWorkerService
{
    class DnyScraper : RegularTask
    {
        private readonly static Random rnd = new Random();

        private readonly HttpClient client;
        private readonly DnyScrapingConfig config;
        private readonly ILogger logger;

        public DnyScraper(DnyScrapingConfig config, ILogger logger) : base(TimeSpan.FromSeconds(config.IntervalSeconds))
        {
            this.config = config;
            this.logger = logger;

            client = CreateHttpClient(config);
        }

        private static HttpClient CreateHttpClient(DnyScrapingConfig config)
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            HttpClient client = new HttpClient(handler);
            client.BaseAddress = new Uri(config.BaseUrl);
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

            foreach (DnyHeaderConfig header in config.Headers)
            {
                switch (header.Key)
                {
                    case "Host":
                        client.DefaultRequestHeaders.Host = header.Value;
                        break;
                    case "Referrer":
                        client.DefaultRequestHeaders.Referrer = new Uri(header.Value);
                        break;
                    default:
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                        break;
                }
            }

            return client;
        }

        private static string CreateSearchParam(DnySearchParamConfig param)
        {
            string value;
            if (param.MinValue.HasValue && param.MaxValue.HasValue)
            {
                value = rnd.Next(param.MinValue.Value, param.MaxValue.Value).ToString();
            }
            else
            {
                value = param.Value ?? string.Empty;
            }

            return $"{WebUtility.UrlEncode(param.Key)}={WebUtility.UrlEncode(value)}";
        }

        private string CreateSearchParams()
        {
            IEnumerable<string> searchParams = config.SearchParams.Select(CreateSearchParam);
            return "?" + string.Join("&", searchParams);
        }

        private void CreateDirectory()
        {
            Directory.CreateDirectory(config.DownloadFolder);
        }

        public override async Task Execute()
        {
            try
            {
                logger.LogInformation("SnyScraper:Scrape:start");

                string searchParams = CreateSearchParams();
                logger.LogInformation($"SnyScraper:Scrape:searchParams:{searchParams}");

                using HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(config.HttpMethod), searchParams);
                using HttpResponseMessage result = await client.SendAsync(request).ConfigureAwait(false);
                if (!result.IsSuccessStatusCode)
                {
                    logger.LogInformation($"SnyScraper:Scrape:request_error:StatusCode={result.StatusCode}");
                    return;
                }

                string path = Path.Combine(config.DownloadFolder, $"{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss}.json");
                string content = await result.Content.ReadAsStringAsync();

                CreateDirectory();
                File.WriteAllText(path, content, Encoding.UTF8);
            }
            catch (Exception e)
            {
                logger.LogInformation(e.ToString());
            }
            finally
            {
                logger.LogInformation("SnyScraper:Scrape:end");
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            client.Dispose();
        }
    }
}
