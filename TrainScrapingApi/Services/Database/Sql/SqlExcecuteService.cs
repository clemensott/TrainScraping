using Npgsql;
using System.Data;
using System.Data.Common;

namespace TrainScrapingApi.Services.Database.Sql
{
    public class SqlExcecuteService : IDbExecuteService
    {
        private readonly string connectionString;
        private NpgsqlConnection? defaultConnection;

        private readonly SemaphoreSlim lockHandlerSem = new SemaphoreSlim(1);
        private readonly IDictionary<NpgsqlConnection, SemaphoreSlim> lockedConnections = new Dictionary<NpgsqlConnection, SemaphoreSlim>();

        public long StatementCount { get; private set; } = 0;

        public string LastSql { get; private set; } = string.Empty;

        public DateTime LastTimestamp { get; private set; } = DateTime.MinValue;

        public NpgsqlConnection DefaultConnection
        {
            get
            {
                if (defaultConnection == null || defaultConnection.State == ConnectionState.Broken)
                {
                    defaultConnection = new NpgsqlConnection(connectionString);
                }
                return defaultConnection;
            }
        }

        public SqlExcecuteService(IAppConfigService appConfigService)
        {
            connectionString = appConfigService.ConnectionString;
        }

        private async Task LockConnection(NpgsqlConnection connection)
        {
            try
            {
                await lockHandlerSem.WaitAsync();

                SemaphoreSlim? connectionSem;
                if (!lockedConnections.TryGetValue(connection, out connectionSem))
                {
                    connectionSem = new SemaphoreSlim(1);
                    lockedConnections.Add(connection, connectionSem);
                }
                await connectionSem.WaitAsync();
            }
            finally
            {
                lockHandlerSem.Release();
            }
        }

        private async Task UnlockConnection(NpgsqlConnection connection)
        {
            try
            {
                await lockHandlerSem.WaitAsync();

                SemaphoreSlim? connectionSem;
                if (lockedConnections.TryGetValue(connection, out connectionSem))
                {
                    connectionSem.Release();
                }
            }
            finally
            {
                lockHandlerSem.Release();
            }
        }

        private static NpgsqlCommand GetCommand(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>>? parameters)
        {
            NpgsqlCommand command = new NpgsqlCommand(sql, connection);

            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> pair in parameters)
                {
                    command.Parameters.AddWithValue(pair.Key, pair.Value ?? DBNull.Value);
                }
            }

            return command;
        }

        private static async Task CheckConnectionState(NpgsqlConnection connection)
        {
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }
        }

        public Task<int> ExecuteNonQueryAsync(string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true)
        {
            return ExecuteNonQueryAsync(DefaultConnection, sql, parameters, isWrite);
        }

        public async Task<int> ExecuteNonQueryAsync(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true)
        {
            try
            {
                using NpgsqlCommand command = GetCommand(connection, sql, parameters);
                await LockConnection(connection);
                await CheckConnectionState(connection);
                UpdateDebug(sql, parameters);

                return await command.ExecuteNonQueryAsync();
            }
            finally
            {
                await UnlockConnection(connection);
            }
        }

        public Task<T?> ExecuteScalarAsync<T>(string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true)
        {
            return ExecuteScalarAsync<T>(DefaultConnection, sql, parameters, isWrite);
        }

        public async Task<T?> ExecuteScalarAsync<T>(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true)
        {
            return (T?)await ExecuteScalarAsync(connection, sql, parameters);
        }

        public Task<object?> ExecuteScalarAsync(string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true)
        {
            return ExecuteScalarAsync(DefaultConnection, sql, parameters, isWrite);
        }

        public async Task<object?> ExecuteScalarAsync(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true)
        {
            try
            {
                using NpgsqlCommand command = GetCommand(connection, sql, parameters);
                await LockConnection(connection);
                await CheckConnectionState(connection);
                UpdateDebug(sql, parameters);

                return await command.ExecuteScalarAsync();
            }
            finally
            {
                await UnlockConnection(connection);
            }
        }

        public Task<IDataRecord[]> ExecuteSelectAllAsync(string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true)
        {
            return ExecuteSelectAllAsync(DefaultConnection, sql, parameters, isWrite);
        }

        public async Task<IDataRecord[]> ExecuteSelectAllAsync(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true)
        {
            try
            {
                using NpgsqlCommand command = GetCommand(connection, sql, parameters);
                await LockConnection(connection);
                await CheckConnectionState(connection);
                UpdateDebug(sql, parameters);

                using DbDataReader reader = await command.ExecuteReaderAsync();
                return reader.Cast<IDataRecord>().ToArray();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteSelectFirstAsync Error: " + e.Message);
                throw;
            }
            finally
            {
                await UnlockConnection(connection);
            }
        }

        public Task<IDataRecord?> ExecuteSelectFirstAsync(string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true)
        {
            return ExecuteSelectFirstAsync(DefaultConnection, sql, parameters, isWrite);
        }

        public async Task<IDataRecord?> ExecuteSelectFirstAsync(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>>? parameters = null, bool isWrite = true)
        {
            try
            {
                using NpgsqlCommand command = GetCommand(connection, sql, parameters);
                await LockConnection(connection);
                await CheckConnectionState(connection);
                UpdateDebug(sql, parameters);

                using DbDataReader reader = await command.ExecuteReaderAsync();
                return reader.Cast<IDataRecord>().FirstOrDefault();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteSelectFirstAsync Error: " + e.Message);
                throw;
            }
            finally
            {
                await UnlockConnection(connection);
            }
        }

        private static string GetEscapedValue(object value)
        {
            if (value is null) return "null";
            if (value is DateTime dateTime) return dateTime.ToString("u");
            if (value is bool || value is byte || value is short || value is int || value is long) return value.ToString();
            return $"'{value.ToString().Replace("'", "''")}'";
        }

        private void UpdateDebug(string sql, IEnumerable<KeyValuePair<string, object>>? parameters)
        {
            if (parameters != null)
            {
                foreach (var pair in parameters.OrderByDescending(p => p.Key.Length))
                {
                    sql = sql.Replace($"@{pair.Key}", GetEscapedValue(pair.Value));
                }
            }
            LastSql = sql;
            LastTimestamp = DateTime.Now;
            StatementCount++;

            System.Diagnostics.Debug.WriteLine(sql);
        }

        public object GetDebugInfo()
        {
            return new
            {
                StatementCount,
                LastSql,
                LastTimestamp,
            };
        }

        public async Task RunTransaction(Func<NpgsqlConnection, Task> func)
        {
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            NpgsqlTransaction? transaction = null;
            try
            {
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();

                await func(connection);

                await transaction.CommitAsync();
            }
            catch
            {
                transaction?.Rollback();
            }
            finally
            {
                await connection.CloseAsync();
                connection.Dispose();
                lockedConnections.Remove(connection);
            }
        }
    }
}
