using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TrainScrapingApi.DB
{
    static class DbHelper
    {
        private static readonly string connectionString = Environment
            .GetEnvironmentVariable("TRAIN_SCRAPING_API_CONNECTION_STRING") ?? throw new Exception("No connectionString");
        private static NpgsqlConnection defaultConnection;

        private static readonly SemaphoreSlim lockHandlerSem = new SemaphoreSlim(1);
        private static readonly IDictionary<NpgsqlConnection, SemaphoreSlim> lockedConnections =
            new Dictionary<NpgsqlConnection, SemaphoreSlim>();

        public static long StatementCount { get; private set; } = 0;

        public static string LastSql { get; private set; } = string.Empty;

        public static DateTime LastTimestamp { get; private set; } = DateTime.MinValue;

        public static NpgsqlConnection DefaultConnection
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

        public static async Task RunTransaction(Func<NpgsqlConnection, Task> func)
        {
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            NpgsqlTransaction transaction = null;
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

        private static async Task LockConnection(NpgsqlConnection connection)
        {
            try
            {
                await lockHandlerSem.WaitAsync();

                SemaphoreSlim connectionSem;
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

        private static async Task UnlockConnection(NpgsqlConnection connection)
        {
            try
            {
                await lockHandlerSem.WaitAsync();

                SemaphoreSlim connectionSem;
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

        private static NpgsqlCommand GetCommand(NpgsqlConnection connection, string sql, IEnumerable<KeyValuePair<string, object>> parameters)
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


        public static async Task<int> ExecuteNonQueryAsync(this NpgsqlConnection connection, string sql,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
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

        public static async Task<T> ExecuteScalarAsync<T>(this NpgsqlConnection connection, string sql,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            return (T)await ExecuteScalarAsync(connection, sql, parameters);
        }


        public static async Task<object> ExecuteScalarAsync(this NpgsqlConnection connection, string sql,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
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

        public static async Task<IDataRecord> ExecuteSelectFirstAsync(this NpgsqlConnection connection, string sql,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
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

        public static async Task<IDataRecord[]> ExecuteSelectAllAsync(this NpgsqlConnection connection, string sql,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
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

        private static string GetEscapedValue(object value)
        {
            if (value is null) return "null";
            if (value is DateTime dateTime) return dateTime.ToString("u");
            if (value is bool || value is byte || value is short || value is int || value is long) return value.ToString();
            return $"'{value.ToString().Replace("'", "''")}'";
        }

        private static void UpdateDebug(string sql, IEnumerable<KeyValuePair<string, object>> parameters)
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

        public static async Task DoInGroups<T>(IEnumerable<T> src, int maxCount, Func<IList<T>, Task> func)
        {
            List<T> list = src.ToList();

            while (list.Count > 0)
            {
                T[] group = list.Take(maxCount).ToArray();
                list.RemoveRange(0, group.Length);

                await func(group);
            }
        }

        public static string Format(int count, string format, string seperator)
        {
            return string.Join(seperator, Enumerable.Range(0, count).Select(i => string.Format(format, i)));
        }

        public static string Format<T>(ICollection<T> src, string format, string seperator)
        {
            return Format(src.Count, format, seperator);
        }

        public static IEnumerable<KeyValuePair<string, object>> GetParameters<T>(string keyPrefix, IEnumerable<T> values)
        {
            int i = 0;
            return values.Select(v => new KeyValuePair<string, object>(keyPrefix + i++, v));
        }

        public static T GetValue<T>(this IDataRecord record, string name, T defaultValue = default(T))
        {
            object value = record[name];

            return value is DBNull ? defaultValue : (T)value;
        }
    }
}
