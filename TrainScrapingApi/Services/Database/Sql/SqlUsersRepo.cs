using TrainScrapingApi.Helpers.DB;

namespace TrainScrapingApi.Services.Database.Sql
{
    public class SqlUsersRepo : IUsersRepo
    {
        private readonly IDbExecuteService dbExecuteService;

        public SqlUsersRepo(IDbExecuteService dbExecuteService)
        {
            this.dbExecuteService = dbExecuteService;
        }

        public async Task<bool> AuthAsync(string token)
        {
            const string sql = "SELECT count(id) FROM users WHERE token = @token AND disabled = FALSE;";
            KeyValueSet parameters = new KeyValueSet("token", token);

            object? scalar = await dbExecuteService.ExecuteScalarAsync(sql, parameters);

            return scalar is long id ? id > 0 : false;
        }
    }
}
