using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingApi.Services.Database
{
    public interface IDnyRepo
    {
        Task<IEnumerable<DnyMeta>> GetMetas(DateTime start, DateTime? end, int limit);

        Task<IEnumerable<Dny>> GetDnys(int[] ids);

        Task Insert(DnyPost dny, DateTime timestamp);
    }
}
