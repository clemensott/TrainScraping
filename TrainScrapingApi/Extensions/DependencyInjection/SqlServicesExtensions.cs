using TrainScrapingApi.Services.Database.Sql;
using TrainScrapingApi.Services.Database;

namespace TrainScrapingApi.Extensions.DependencyInjection
{
    static class SqlServicesExtensions
    {
        public static void AddSqlServices(this IServiceCollection services)
        {
            services.AddSingleton<IDbExecuteService, SqlExcecuteService>();
            services.AddSingleton<IUsersRepo, SqlUsersRepo>();
            services.AddSingleton<IDnyRepo, SqlDnyRepo>();
        }
    }
}
