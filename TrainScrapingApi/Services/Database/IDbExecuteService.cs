using Npgsql;
using System.Data;

namespace TrainScrapingApi.Services.Database
{
    public interface IDbExecuteService
    {
        NpgsqlConnection DefaultConnection { get; }

        object GetDebugInfo();

        Task<int> ExecuteNonQueryAsync(string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true);

        Task<int> ExecuteNonQueryAsync(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true);

        Task<T?> ExecuteScalarAsync<T>(string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true);
        
        Task<T?> ExecuteScalarAsync<T>(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true);

        Task<object?> ExecuteScalarAsync(string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true);
        
        Task<object?> ExecuteScalarAsync(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true);

        Task<IDataRecord?> ExecuteSelectFirstAsync(string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true);
        
        Task<IDataRecord?> ExecuteSelectFirstAsync(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true);

        Task<IDataRecord[]> ExecuteSelectAllAsync(string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true);
        
        Task<IDataRecord[]> ExecuteSelectAllAsync(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true);

        Task RunTransaction(Func<NpgsqlConnection, Task> func);
    }
}
