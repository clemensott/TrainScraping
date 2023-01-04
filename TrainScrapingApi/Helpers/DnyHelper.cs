using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrainScrapingApi.DB;
using TrainScrapingApi.Models;
using TrainScrapingCommon.Models;

namespace TrainScrapingApi.Helpers
{
    static class DnyHelper
    {
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        private static Task<int> InsertDnyEntry(NpgsqlConnection connection, Dny dny, DateTime timestamp)
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

            return connection.ExecuteScalarAsync<int>(sql, parameters);
        }

        private static async Task InsertTrains(NpgsqlConnection connection, IList<DnyTrainContainer> containers)
        {
            const string dataColumns = "hash_id";
            ILookup<string, DnyTrainContainer> lookup = containers.ToLookup(c => c.Train.I);

            string getValues = DbHelper.Format(lookup.Count, "@hashId{0}", ",");
            string getSql = $"SELECT id, {dataColumns} FROM trains WHERE hash_id in ({getValues});";
            KeyValueSet getParameters = new KeyValueSet()
                .Add(DbHelper.GetParameters("hashId", lookup.Select(p => p.Key)));

            IDataRecord[] getResult = await connection.ExecuteSelectAllAsync(getSql, getParameters);
            SetTrainIds(getResult);

            string[] missingHashIds = containers.Where(c => c.TrainId == 0).Select(c => c.Train.I).Distinct().ToArray();
            if (missingHashIds.Length == 0) return;

            string insertValues = DbHelper.Format(missingHashIds, "(@hashId{0})", ",");
            string insertSql = $"INSERT INTO trains ({dataColumns}) VALUES {insertValues} RETURNING id, {dataColumns};";
            KeyValueSet insertParameters = new KeyValueSet()
                .Add(DbHelper.GetParameters("hashId", missingHashIds));

            IDataRecord[] insertResult = await connection.ExecuteSelectAllAsync(insertSql, insertParameters);
            SetTrainIds(insertResult);

            if (containers.Any(c => c.TrainId == 0)) throw new Exception("Missing train id");

            void SetTrainIds(IDataRecord[] result)
            {
                foreach (IDataRecord data in result)
                {
                    int id = data.GetValue<int>("id");
                    string hashId = data.GetValue<string>("hash_id");

                    foreach (DnyTrainContainer container in lookup[hashId])
                    {
                        container.TrainId = id;
                    }
                }
            }
        }

        private static async Task InsertTrainDays(NpgsqlConnection connection, IList<DnyTrainContainer> containers)
        {
            await InsertTrains(connection, containers);

            const string dataColumns = "train_id, date";
            ILookup<int, DnyTrainContainer> lookup = containers.ToLookup(c => c.TrainId);

            string getWheres = DbHelper.Format(containers, "train_id = @trainId{0} AND date = @date{0}", " OR ");
            string getSql = $"SELECT id, {dataColumns} FROM train_days WHERE {getWheres};";
            KeyValueSet getParameters = new KeyValueSet()
                .Add(DbHelper.GetParameters("trainId", containers.Select(c => c.TrainId)))
                .Add(DbHelper.GetParameters("date", containers.Select(c => c.Date)));

            IDataRecord[] getResult = await connection.ExecuteSelectAllAsync(getSql, getParameters);
            SetTrainDayIds(getResult);

            DnyTrainContainer[] missingContainers = containers
                .Where(c => c.TrainDayId == 0)
                .Distinct(TrainDayEqualityComparer.Instance)
                .ToArray();
            if (missingContainers.Length == 0) return;

            string insertValues = DbHelper.Format(missingContainers, "(@trainId{0}, @date{0})", ",");
            string insertSql = $"INSERT INTO train_days ({dataColumns}) VALUES {insertValues} RETURNING id, {dataColumns};";
            KeyValueSet insertParameters = new KeyValueSet()
                .Add(DbHelper.GetParameters("trainId", missingContainers.Select(c => c.TrainId)))
                .Add(DbHelper.GetParameters("date", missingContainers.Select(c => c.Date)));

            IDataRecord[] insertResult = await connection.ExecuteSelectAllAsync(insertSql, insertParameters);
            SetTrainDayIds(insertResult);

            if (containers.Any(c => c.TrainDayId == 0)) throw new Exception("Missing train_day id");

            void SetTrainDayIds(IDataRecord[] result)
            {
                foreach (IDataRecord data in result)
                {
                    int id = data.GetValue<int>("id");
                    int trainId = data.GetValue<int>("train_id");
                    DateTime date = data.GetValue<DateTime>("date");

                    foreach (DnyTrainContainer container in lookup[trainId])
                    {
                        if (container.Date == date) container.TrainDayId = id;
                    }
                }
            }
        }

        private static async Task InsertTrainInfo(NpgsqlConnection connection, DnyTrainContainer[] containers)
        {
            const string dataColumns = "name, destination, product_class";
            ILookup<string, DnyTrainContainer> lookup = containers.ToLookup(c => c.Train.N);

            string getWheres = DbHelper.Format(containers, "name = @name{0} AND destination = @destination{0} AND product_class = @productClass{0}", " OR ");
            string getSql = $"SELECT id, {dataColumns} FROM dny_train_infos WHERE {getWheres};";
            KeyValueSet getParameters = new KeyValueSet()
                .Add(DbHelper.GetParameters("name", containers.Select(c => c.Train.N)))
                .Add(DbHelper.GetParameters("destination", containers.Select(c => c.Train.L)))
                .Add(DbHelper.GetParameters("productClass", containers.Select(c => c.ProductClass)));

            IDataRecord[] getResult = await connection.ExecuteSelectAllAsync(getSql, getParameters);
            SetDnyTrainInfoIds(getResult);

            DnyTrainContainer[] missingContainers = containers
                .Where(c => c.DnyTrainInfoId == 0)
                .Distinct(DnyTrainInfoEqualityComparer.Instance)
                .ToArray();
            if (missingContainers.Length == 0) return;

            string insertValues = DbHelper.Format(missingContainers, "(@name{0}, @destination{0}, @productClass{0})", ",");
            string insertSql = $"INSERT INTO dny_train_infos ({dataColumns}) VALUES {insertValues} RETURNING id, {dataColumns};";
            KeyValueSet insertParameters = new KeyValueSet()
                .Add(DbHelper.GetParameters("name", missingContainers.Select(c => c.Train.N)))
                .Add(DbHelper.GetParameters("destination", missingContainers.Select(c => c.Train.L)))
                .Add(DbHelper.GetParameters("productClass", missingContainers.Select(c => c.ProductClass)));

            IDataRecord[] insertResult = await connection.ExecuteSelectAllAsync(insertSql, insertParameters);
            SetDnyTrainInfoIds(insertResult);

            if (containers.Any(c => c.DnyTrainInfoId == 0)) throw new Exception("Missing dny_train_info id");

            void SetDnyTrainInfoIds(IDataRecord[] result)
            {
                foreach (IDataRecord data in result)
                {
                    int id = data.GetValue<int>("id");
                    string name = data.GetValue<string>("name");
                    string destination = data.GetValue<string>("destination");
                    short productClass = data.GetValue<short>("product_class");

                    foreach (DnyTrainContainer container in lookup[name])
                    {
                        if (container.Train.L == destination && container.ProductClass == productClass) container.DnyTrainInfoId = id;
                    }
                }
            }
        }

        private static Task InsertDnyTrains(NpgsqlConnection connection, int dnyId, DnyTrain[] allTrains)
        {
            return DbHelper.DoInGroups(allTrains, 1000, async group =>
            {
                DnyTrainContainer[] containers = group.Select(t => new DnyTrainContainer(t)).ToArray();
                await InsertTrainDays(connection, containers);
                await InsertTrainInfo(connection, containers);

                string insertValues = DbHelper.Format(containers,
                    "(@dnyId, @trainDayId{0}, @dnyTrainInfoId{0}, @latitude{0}, @longitude{0}, @direction{0}, @delay{0})", ",");
                string insertSql = $@"
                    INSERT INTO dny_train_days (dny_id, train_day_id, dny_train_info_id, latitude, longitude, direction, delay)
                    VALUES {insertValues};
                ";
                KeyValueSet parameters = new KeyValueSet("dnyId", dnyId)
                    .Add(DbHelper.GetParameters("trainDayId", containers.Select(c => c.TrainDayId)))
                    .Add(DbHelper.GetParameters("dnyTrainInfoId", containers.Select(c => c.DnyTrainInfoId)))
                    .Add(DbHelper.GetParameters("latitude", containers.Select(c => c.Latitude)))
                    .Add(DbHelper.GetParameters("longitude", containers.Select(c => c.Longitude)))
                    .Add(DbHelper.GetParameters("direction", containers.Select(c => c.Direction)))
                    .Add(DbHelper.GetParameters("delay", containers.Select(c => c.Delay)));
                await connection.ExecuteNonQueryAsync(insertSql, parameters);
            });
        }

        private static Task SetDnySuccessfull(NpgsqlConnection connection, int dnyId)
        {
            const string sql = "UPDATE dnys SET success = TRUE WHERE id = @id;";
            KeyValueSet parameters = new KeyValueSet("id", dnyId);
            return connection.ExecuteNonQueryAsync(sql, parameters);
        }

        public static async Task Insert(Dny dny, DateTime timestamp)
        {
            try
            {
                await semaphore.WaitAsync();
                await DbHelper.RunTransaction(async connection =>
                {
                    int dnyId = await InsertDnyEntry(connection, dny, timestamp);
                    await InsertDnyTrains(connection, dnyId, dny.T);
                    await SetDnySuccessfull(connection, dnyId);
                });
            }
            finally
            {
                semaphore.Release();
            }
        }

        class TrainDayEqualityComparer : IEqualityComparer<DnyTrainContainer>
        {
            private static TrainDayEqualityComparer instance;

            public static TrainDayEqualityComparer Instance => instance ??= new TrainDayEqualityComparer();


            public bool Equals([AllowNull] DnyTrainContainer x, [AllowNull] DnyTrainContainer y)
            {
                if (x == y) return true;
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                return x.TrainId == y.TrainId && x.Date == y.Date;
            }

            public int GetHashCode([DisallowNull] DnyTrainContainer obj)
            {
                return HashCode.Combine(obj.TrainId, obj.Date);
            }
        }

        class DnyTrainInfoEqualityComparer : IEqualityComparer<DnyTrainContainer>
        {
            private static DnyTrainInfoEqualityComparer instance;

            public static DnyTrainInfoEqualityComparer Instance => instance ??= new DnyTrainInfoEqualityComparer();


            public bool Equals([AllowNull] DnyTrainContainer x, [AllowNull] DnyTrainContainer y)
            {
                if (x == y) return true;
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                return x.Train.N == y.Train.N &&
                    x.Train.L == y.Train.L &&
                    x.ProductClass == y.ProductClass;
            }

            public int GetHashCode([DisallowNull] DnyTrainContainer obj)
            {
                return HashCode.Combine(obj.Train.N, obj.Train.L, obj.ProductClass);
            }
        }
    }
}
