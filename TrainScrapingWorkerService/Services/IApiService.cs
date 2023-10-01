using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingWorkerService.Services
{
    internal interface IApiService : IDisposable
    {
        public string BaseUrl { get; }

        public string Token { get; }

        Task<bool> Ping();

        Task<bool> PostDny(DnyPost dny, DateTime timestamp);
    }
}
