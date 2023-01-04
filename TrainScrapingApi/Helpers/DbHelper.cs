using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TrainScrapingApi.Helpers
{
    static class DbHelper
    {
        private static readonly string connectionString = File.ReadAllText("connectionString.txt");
        private static readonly SemaphoreSlim semConnection = new SemaphoreSlim(1);
        private static NpgsqlConnection writeConnection;

        public static long StatementCount { get; private set; } = 0;

        public static string LastSql { get; private set; }

        public static DateTime LastTimestamp { get; private set; }

        public static int WriteAvailableCount => semConnection.CurrentCount;

        private static async Task<NpgsqlConnection> GetWriteConnection()
        {
            await semConnection.WaitAsync();

            if (writeConnection == null || writeConnection.State == ConnectionState.Closed)
            {
                writeConnection = new NpgsqlConnection(connectionString);
                await writeConnection.OpenAsync();
            }

            return writeConnection;
        }

        private static void UnlockConnection(NpgsqlConnection connection)
        {
            semConnection.Release();
        }

        private static async Task<NpgsqlCommand> GetCommand(string sql, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            NpgsqlCommand command = new NpgsqlCommand(sql, await GetWriteConnection());

            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> pair in parameters)
                {
                    command.Parameters.AddWithValue(pair.Key, pair.Value ?? DBNull.Value);
                }
            }

            UpdateDebug(sql, parameters);

            return command;
        }

        public static async Task<int> ExecuteNonQueryAsync(string sql,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            try
            {
                using (NpgsqlCommand command = await GetCommand(sql, parameters))
                {
                    try
                    {
                        return await command.ExecuteNonQueryAsync();
                    }
                    finally
                    {
                        UnlockConnection(command.Connection);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteNonQueryAsync Error: " + e.Message);
                throw;
            }
        }

        public static async Task<T> ExecuteScalarAsync<T>(string sql,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            return (T)await ExecuteScalarAsync(sql, parameters);
        }

        public static async Task<object> ExecuteScalarAsync(string sql,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            try
            {
                using (NpgsqlCommand command = await GetCommand(sql, parameters))
                {
                    try
                    {
                        return await command.ExecuteScalarAsync();
                    }
                    finally
                    {
                        UnlockConnection(command.Connection);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteScalarAsync Error: " + e.Message);
                throw;
            }
        }

        public static async Task<IDataRecord> ExecuteSelectFirstAsync(string sql,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            try
            {
                using (NpgsqlCommand command = await GetCommand(sql, parameters))
                {
                    try
                    {
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            return reader.Cast<IDataRecord>().FirstOrDefault();
                        }
                    }
                    finally
                    {
                        UnlockConnection(command.Connection);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteSelectFirstAsync Error: " + e.Message);
                throw;
            }
        }

        public static async Task<IDataRecord[]> ExecuteSelectAllAsync(string sql,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            try
            {
                using (NpgsqlCommand command = await GetCommand(sql, parameters))
                {
                    try
                    {
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            return reader.Cast<IDataRecord>().ToArray();
                        }
                    }
                    finally
                    {
                        UnlockConnection(command.Connection);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteSelectFirstAsync Error: " + e.Message);
                throw;
            }
        }

        private static string GetEscapedValue(object value)
        {
            if (value is null) return "null";
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
    }
}
