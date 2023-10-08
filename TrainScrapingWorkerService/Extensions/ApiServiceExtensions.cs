using TrainScrapingWorkerService.Configuration;
using TrainScrapingWorkerService.Services;

namespace TrainScrapingWorkerService.Extensions
{
    static class ApiServiceExtensions
    {
        public static IApiService CreateApi(this Config config)
        {
            return config.ApiType switch
            {
                ApiTypeConfig.Rest => new RestApiService(config.ApiBaseUrl, config.ApiToken),
                ApiTypeConfig.InfluxDB => new InfluxApiService(config.ApiBaseUrl, config.ApiToken, config.ApiBucket, config.ApiOrg, config.ScraperName),
                ApiTypeConfig.QuestDB => new QuestDbApiService(config.ApiBaseUrl, config.ScraperName),
                _ => throw new ArgumentException(),
            };
        }
    }
}
