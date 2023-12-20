using QuestDB;
using TrainScrapingCommon.Helpers;
using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingWorkerService.Services
{
    internal class QuestDbApiService : IApiService
    {
        private readonly HttpClient client;

        public Uri BaseAddress { get; }

        public string ScraperId { get; }

        public QuestDbApiService(string baseUrl, string scraperId)
        {
            BaseAddress = new Uri(baseUrl);
            ScraperId = scraperId;

            client = new HttpClient();
            client.BaseAddress = BaseAddress;
        }

        public async Task<bool> Ping()
        {
            try
            {
                return true;
                var response = await client.GetAsync("/");
                return response.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public async Task PostDny(DnyPost dny, DateTime timestamp)
        {
            TlsMode mode = BaseAddress.Scheme == "https" ? TlsMode.Enable : TlsMode.Disable;
            using LineTcpSender ls = await LineTcpSender.ConnectAsync(BaseAddress.Host, BaseAddress.Port, tlsMode: mode);

            string dnyId = Guid.NewGuid().ToString();
            ls.Table("dnys")
                .Symbol("scraper_id", ScraperId)
                .Column("id", dnyId)
                .Column("local_time", dny.Ts)
                .Column("min_lat", long.Parse(dny.Y1))
                .Column("max_lat", long.Parse(dny.Y2))
                .Column("min_long", long.Parse(dny.X0))
                .Column("max_long", long.Parse(dny.X1))
                .Column("trains_count", long.Parse(dny.N))
                .At(timestamp);

            foreach (DnyTrain train in dny.T)
            {
                ls.Table("dny_trains")
                    .Symbol("train_id", train.I)
                    .Symbol("date", ParseHelper.ParseDate(train.R).ToString("yyyy-MM-dd"))
                    .Symbol("name", train.N)
                    .Symbol("destination", train.L)
                    .Column("dny_id", dnyId)
                    .Column("lat", long.Parse(train.X))
                    .Column("long", long.Parse(train.Y))
                    .Column("direction", long.Parse(train.D))
                    .Column("product_class", long.Parse(train.C));

                if (!string.IsNullOrEmpty(train.Rt))
                {
                    ls.Column("delay", long.Parse(train.Rt));
                }

                ls.At(timestamp);
            }

            await ls.SendAsync();
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
