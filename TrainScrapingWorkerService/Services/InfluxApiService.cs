using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using TrainScrapingCommon.Helpers;
using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingWorkerService.Services
{
    class InfluxApiService : IApiService
    {
        private readonly InfluxDBClient client;

        public string Bucket { get; }

        public string Org { get; }

        public string ScraperId { get; }

        public InfluxApiService(string baseUrl, string token, string bucket, string org, string scraperId)
        {
            Bucket = bucket;
            Org = org;
            ScraperId = scraperId;

            client = new InfluxDBClient(baseUrl, token);
        }

        public Task<bool> Ping()
        {
            return client.PingAsync();
        }

        public async Task PostDny(DnyPost dny, DateTime timestamp)
        {
            WriteApiAsync writeApi = client.GetWriteApiAsync();
            List<PointData> points = new List<PointData>();

            string dnyId = Guid.NewGuid().ToString();
            PointData dnyMeasurement = PointData.Measurement("dny")
                .Tag("scraper_id", ScraperId)
                .Field("id", dnyId)
                .Field("local_time", dny.Ts)
                .Field("min_lat", int.Parse(dny.Y1))
                .Field("max_lat", int.Parse(dny.Y2))
                .Field("min_long", int.Parse(dny.X0))
                .Field("max_long", int.Parse(dny.X1))
                .Field("trains_count", int.Parse(dny.N))
                .Timestamp(timestamp, WritePrecision.S);
            points.Add(dnyMeasurement);

            IDictionary<string, IDictionary<string, DateTime>> nameTimestamps = new Dictionary<string, IDictionary<string, DateTime>>();
            foreach (DnyTrain train in dny.T)
            {
                if (!nameTimestamps.TryGetValue(train.N, out var destinationTimestamps))
                {
                    destinationTimestamps = new Dictionary<string, DateTime>();
                    nameTimestamps.Add(train.N, destinationTimestamps);
                }

                // Workaround: If a train with same name and destination exists in DNY than change timestamp
                // because it would override previous entries in InfluxDB because of equal Tags
                if (destinationTimestamps.TryGetValue(train.L, out var sendTimestamp)) sendTimestamp = sendTimestamp.AddSeconds(1);
                else sendTimestamp = timestamp;

                destinationTimestamps[train.L] = sendTimestamp;

                PointData trainMeasurement = PointData.Measurement("dny_train")
                    .Tag("name", train.N)
                    .Tag("destination", train.L)
                    .Field("train_id", train.I)
                    .Field("dny_id", dnyId)
                    .Field("lat", int.Parse(train.X))
                    .Field("long", int.Parse(train.Y))
                    .Field("direction", int.Parse(train.D))
                    .Field("product_class", int.Parse(train.C))
                    .Field("date", ParseHelper.ParseDate(train.R).ToString("yyyy-MM-dd"))
                    .Field("delay", string.IsNullOrEmpty(train.Rt) ? null : int.Parse(train.Rt))
                    .Timestamp(sendTimestamp, WritePrecision.S);

                points.Add(trainMeasurement);
            }

            await writeApi.WritePointsAsync(points, Bucket, Org);
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
