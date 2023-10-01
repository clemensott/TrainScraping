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

        public string BaseUrl { get; }

        public string Token { get; }

        public string Bucket { get; }

        public string Org { get; }

        public string ScraperId { get; }

        public InfluxApiService(string baseUrl, string token, string bucket, string org, string scraperId)
        {
            BaseUrl = baseUrl;
            Token = token;
            Bucket = bucket;
            Org = org;
            ScraperId = scraperId;

            client = new InfluxDBClient(BaseUrl, token);
        }

        public Task<bool> Ping()
        {
            return client.PingAsync();
        }

        public async Task<bool> PostDny(DnyPost dny, DateTime timestamp)
        {
            try
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

                foreach (DnyTrain train in dny.T)
                {
                    PointData trainMeasurement = PointData.Measurement("dny_train")
                        .Tag("train_id", train.I)
                        .Field("dny_id", dnyId)
                        .Field("lat", int.Parse(train.X))
                        .Field("long", int.Parse(train.Y))
                        .Field("name", train.N)
                        .Field("direction", int.Parse(train.D))
                        .Field("product_class", int.Parse(train.C))
                        .Field("date", ParseHelper.ParseDate(train.R).ToString("yyyy-MM-dd"))
                        .Field("delay", string.IsNullOrEmpty(train.Rt) ? null : int.Parse(train.Rt))
                        .Field("destination", train.L)
                        .Timestamp(timestamp, WritePrecision.S);

                    points.Add(trainMeasurement);
                }

                await writeApi.WritePointsAsync(points, Bucket, Org);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
