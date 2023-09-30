namespace TrainScrapingApi.Services
{
    public class AppEnvConfigService : IAppConfigService
    {
        public string ConnectionString { get; }

        public AppEnvConfigService()
        {
            string? connectionString = Environment.GetEnvironmentVariable("TRAIN_SCRAPING_API_CONNECTION_STRING");
            ConnectionString = connectionString ?? throw new Exception("No connectionString");
        }
    }
}
