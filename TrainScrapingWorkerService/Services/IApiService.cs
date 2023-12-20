using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingWorkerService.Services
{
    internal interface IApiService : IDisposable
    {
        Task<bool> Ping();

        Task PostDny(DnyPost dny, DateTime timestamp);
    }
}
