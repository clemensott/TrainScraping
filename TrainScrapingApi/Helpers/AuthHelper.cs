using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace TrainScrapingApi.Helpers
{
    static class AuthHelper
    {
        public static async Task<bool> AuthAsync(string token)
        {
            const string sql = "SELECT count(id) FROM users WHERE token = @token AND disabled = FALSE;";
            KeyValueSet parameters = new KeyValueSet("token", token);

            object scalar = await DbHelper.ExecuteScalarAsync(sql, parameters);

            return scalar is long id ? id > 0 : false;
        }

        public static async Task<bool> IsAuthenticatedAsync(this ControllerBase controller)
        {
            if (!controller.Request.Headers.ContainsKey("Authorization")) return false;

            string auth = controller.Request.Headers["Authorization"];
            if (!auth.StartsWith("Basic ")) return false;

            string token = auth[6..];
            return await AuthAsync(token);
        }
    }
}
