using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingWorkerService.Services
{
    internal interface IApiService : IDisposable
    {
        Task<bool> Ping();

        Task<bool> PostDny(DnyPost dny, DateTime timestamp);
    }
}
