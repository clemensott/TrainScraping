using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TrainScraping
{
    public partial class TrainScrapingService : ServiceBase
    {
        private Config config;
        private readonly Timer timer;
        private readonly HttpClient client;

        public TrainScrapingService()
        {
            InitializeComponent();

            timer = new Timer();
            timer.Elapsed += Timer_Elapsed;

            client = CreateHttpClient();
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            HttpClient client = new HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de-DE"));
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de", 0.9));
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.8));
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.7));
            client.DefaultRequestHeaders.Connection.Add("keep-alive");
            client.DefaultRequestHeaders.Add("DNT", "1");
            client.DefaultRequestHeaders.Host = "zugradar.oebb.at";
            client.DefaultRequestHeaders.Add("Origin", "http://zugradar.oebb.at");
            client.DefaultRequestHeaders.Referrer = new Uri("http://zugradar.oebb.at/bin/help.exe/dn?tpl=livefahrplan");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("X-Prototype-Version", "1.5.0");
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            return client;
        }

        protected override void OnStart(string[] args)
        {
            config = Config.Load();

            Logger.Log($"DnyInterval={config.DnyInterval.TotalSeconds}");
            Logger.Log($"DnyDownloadPath={config.DnyDownloadPath}");
            Logger.Log($"DnyURL={config.DnyURL}");

            timer.Interval = config.DnyInterval.TotalMilliseconds;
            timer.Start();

            ScrapeDny();
        }

        protected override void OnStop()
        {
            timer.Stop();
            timer.Dispose();

            client.Dispose();
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await ScrapeDny();
        }

        private async Task ScrapeDny()
        {
            try
            {
                Logger.Log("ScrapeDny:start");

                if (string.IsNullOrWhiteSpace(config.DnyURL))
                {
                    Logger.Log("ScrapeDny:no_dny_url");
                    return;
                }

                using (HttpResponseMessage result = await client.PostAsync(config.DnyURL, null).ConfigureAwait(false))
                {
                    if (!result.IsSuccessStatusCode)
                    {
                        Logger.Log($"ScrapeDny:request_error:StatusCode={result.StatusCode}");
                        return;
                    }

                    string path = Path.Combine(config.DnyDownloadPath, $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss")}.json");
                    string content = await result.Content.ReadAsStringAsync();
                    File.WriteAllText(path, content, Encoding.UTF8);
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
            }
            finally
            {
                Logger.Log("ScrapeDny:end");
            }
        }
    }
}
