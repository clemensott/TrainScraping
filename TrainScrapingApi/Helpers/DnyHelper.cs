using System;
using System.Threading.Tasks;
using TrainScrapingCommon.Models;

namespace TrainScrapingApi.Helpers
{
    static class DnyHelper
    {
        private static Task<int> InsertDnyEntry(Dny dny, DateTime timestamp)
        {
            TimeSpan time = TimeSpan.Parse(dny.Ts);
            decimal minLat = ParseHelper.ParseCoordinate(dny.Y1);
            decimal minLong = ParseHelper.ParseCoordinate(dny.X0);
            decimal maxLat = ParseHelper.ParseCoordinate(dny.Y2);
            decimal maxLong = ParseHelper.ParseCoordinate(dny.X1);
            int trainsCount = int.Parse(dny.N);

            const string sql = @"
                INSERT INTO dnys (""time"", min_latitude, min_longitude, max_latitude, max_longitude, trains_count, timestamp)
                VALUES (@time, @minLat, @minLong, @maxLat, @maxLong, @trainsCount, @timestamp)
                RETURNING id;
            ";
            KeyValueSet parameters = new KeyValueSet()
                .Add("time", time)
                .Add("minLat", minLat)
                .Add("minLong", minLong)
                .Add("maxLat", maxLat)
                .Add("maxLong", maxLong)
                .Add("trainsCount", trainsCount)
                .Add("timestamp", timestamp);

            return DbHelper.ExecuteScalarAsync<int>(sql, parameters);
        }

        private static async Task<int> InsertTrain(string hashId)
        {
            const string getSql = "SELECT id FROM trains WHERE hash_id = @hashId;";
            KeyValueSet parameters = new KeyValueSet("hashId", hashId);

            int? trainId = await DbHelper.ExecuteScalarAsync<int?>(getSql, parameters);
            if (trainId.HasValue) return trainId.Value;

            const string insertSql = "INSERT INTO trains (hash_id) VALUES (@hashId) RETURNING id;";
            return await DbHelper.ExecuteScalarAsync<int>(insertSql, parameters);
        }

        private static async Task<int> InsertTrainDay(DnyTrain train)
        {
            int trainId = await InsertTrain(train.I);
            const string getSql = "SELECT id FROM train_days WHERE train_id = @trainId AND date = @date;";
            KeyValueSet parameters = new KeyValueSet("trainId", trainId, "date", ParseHelper.ParseDate(train.R));

            int? trainDayId = await DbHelper.ExecuteScalarAsync<int?>(getSql, parameters);
            if (trainDayId.HasValue) return trainDayId.Value;

            const string insertSql = @"INSERT INTO train_days (train_id, date) VALUES (@trainId, @date) RETURNING id;";
            return await DbHelper.ExecuteScalarAsync<int>(insertSql, parameters);
        }

        private static async Task<int> InsertTrainInfo(DnyTrain train)
        {
            const string getSql = @"
                SELECT id 
                FROM dny_train_infos
                WHERE name = @name AND destination = @destination AND product_class = @productClass;
            ";
            KeyValueSet parameters = new KeyValueSet()
                .Add("name", train.N)
                .Add("destination", train.L)
                .Add("productClass", int.Parse(train.C));

            int? trainInfoId = await DbHelper.ExecuteScalarAsync<int?>(getSql, parameters);
            if (trainInfoId.HasValue) return trainInfoId.Value;

            const string insertSql = @"
                INSERT INTO dny_train_infos (name, destination, product_class)
                VALUES (@name, @destination, @productClass)
                RETURNING id;
            ";
            return await DbHelper.ExecuteScalarAsync<int>(insertSql, parameters);
        }

        private static async Task InsertDnyTrain(int dnyId, DnyTrain train)
        {
            int trainDayId = await InsertTrainDay(train);
            int dnyTrainInfoId = await InsertTrainInfo(train);

            const string insertSql = @"
                INSERT INTO dny_train_days (dny_id, train_day_id, dny_train_info_id, latitude, longitude, direction, delay)
                VALUES (@dnyId, @trainDayId, @dnyTrainInfoId, @latitude, @longitude, @direction, @delay);
            ";
            KeyValueSet parameters = new KeyValueSet()
                .Add("dnyId", dnyId)
                .Add("trainDayId", trainDayId)
                .Add("dnyTrainInfoId", dnyTrainInfoId)
                .Add("latitude", ParseHelper.ParseCoordinate(train.Y))
                .Add("longitude", ParseHelper.ParseCoordinate(train.X))
                .Add("direction", short.Parse(train.D))
                .Add("delay", train.Rt != null ? (int?)int.Parse(train.Rt) : null);
            await DbHelper.ExecuteNonQueryAsync(insertSql, parameters);
        }

        private static Task SetDnySuccessfull(int dnyId)
        {
            const string sql = "UPDATE dnys SET success = TRUE WHERE id = @id;";
            KeyValueSet parameters = new KeyValueSet("id", dnyId);
            return DbHelper.ExecuteNonQueryAsync(sql, parameters);
        }

        public static async Task Insert(Dny dny, DateTime timestamp)
        {
            int dnyId = await InsertDnyEntry(dny, timestamp);

            foreach (DnyTrain train in dny.T)
            {
                await InsertDnyTrain(dnyId, train);
            }

            await SetDnySuccessfull(dnyId);
        }
    }
}
