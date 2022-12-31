using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TrainScraping.Configuration;

namespace TrainScraping
{
    class DnyScraper : IDisposable
    {
        private readonly static Random rnd = new Random();

        private readonly Timer timer;
        private readonly HttpClient client;
        private readonly DnyScrapingConfig config;

        public DnyScraper(DnyScrapingConfig config)
        {
            timer = new Timer();
            timer.Elapsed += Timer_Elapsed;
            if (config.IntervalSeconds > 0)
            {
                timer.Interval = config.IntervalSeconds * 1000;
            }

            this.config = config;
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

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Scrape();
        }

        public void StartTimer()
        {
            if (config.IntervalSeconds > 0)
            {
                timer.Start();
            }
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

        public async Task Scrape()
        {
            try
            {
                Logger.Log("SnyScraper:Scrape:start");

                string searchParams = CreateSearchParams();
                Logger.Log($"SnyScraper:Scrape:searchParams:{searchParams}");

                using (HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(config.HttpMethod), searchParams))
                {

                    using (HttpResponseMessage result = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        if (!result.IsSuccessStatusCode)
                        {
                            Logger.Log($"SnyScraper:Scrape:request_error:StatusCode={result.StatusCode}");
                            return;
                        }

                        string path = Path.Combine(config.DownloadFolder, $"{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss}.json");
                        string content = await result.Content.ReadAsStringAsync();

                        CreateDirectory();
                        File.WriteAllText(path, content, Encoding.UTF8);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
            }
            finally
            {
                Logger.Log("SnyScraper:Scrape:end");
            }
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Dispose();
            client.Dispose();
        }
    }
}
