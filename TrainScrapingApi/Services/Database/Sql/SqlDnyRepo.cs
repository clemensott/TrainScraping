using Npgsql;
using System.Data;
using TrainScrapingApi.Extensions.ModelConverting;
using TrainScrapingApi.Helpers.Comparers;
using TrainScrapingApi.Helpers.DB;
using TrainScrapingApi.Models;
using TrainScrapingCommon.Helpers;
using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingApi.Services.Database.Sql
{
    public class SqlDnyRepo : IDnyRepo
    {
        private readonly SemaphoreSlim insertSemaphore = new SemaphoreSlim(1);
        private readonly IDbExecuteService dbExecuteService;

        public SqlDnyRepo(IDbExecuteService dbExecuteService)
        {
            this.dbExecuteService = dbExecuteService;
        }

        public async Task<IEnumerable<DnyMeta>> GetMetas(DateTime start, DateTime? end, int limit)
        {
            string whereEnd = end.HasValue ? " timestamp < @end" : string.Empty;
            string sql = $"SELECT * FROM dnys WHERE timestamp >= @start{whereEnd} ORDER BY timestamp LIMIT @limit;";
            KeyValueSet parameters = new KeyValueSet("start", start, "limit", limit);

            if (end.HasValue) parameters.Add("end", end.Value);

            IDataRecord[] result = await dbExecuteService.ExecuteSelectAllAsync(sql, parameters);
            return result.Select(SqlModelConverterExtensions.GetDnyMeta);
        }

        public async Task<IEnumerable<Dny>> GetDnys(int[] ids)
        {
            if (ids.Length == 0) return Enumerable.Empty<Dny>();

            string whereIds = SqlQueryHelper.Format(ids, "@id{0}", ",");
            string sql = @$"
                SELECT d.id              as id,
                       d.time            as time,
                       d.min_latitude    as min_latitude,
                       d.min_longitude   as min_longitude,
                       d.max_latitude    as max_latitude,
                       d.max_longitude   as max_longitude,
                       d.trains_count    as trains_count,
                       d.timestamp       as timestamp,
                       dtd.longitude     as longitude,
                       dtd.latitude      as latitude,
                       dti.name          as name,
                       t.hash_id         as hash_id,
                       dtd.direction     as direction,
                       dti.product_class as product_class,
                       td.date           as date,
                       dtd.delay         as delay,
                       dti.destination   as destination,
                       false             as last
                FROM dnys d
                         JOIN dny_train_days dtd ON d.id = dtd.dny_id
                         JOIN train_days td ON dtd.train_day_id = td.id
                         JOIN trains t ON td.train_id = t.id
                         JOIN dny_train_infos dti ON dtd.dny_train_info_id = dti.id
                WHERE d.id IN ({whereIds});
            ";
            KeyValueSet parameters = new KeyValueSet()
                .Add(SqlQueryHelper.GetParameters("id", ids));

            IDataRecord[] result = await dbExecuteService.ExecuteSelectAllAsync(sql, parameters);
            IEnumerable<IGrouping<int, IDataRecord>> groups = result.GroupBy(d => d.GetValue<int>("id"));
            return groups.Select(SqlModelConverterExtensions.GetDny);
        }

        private Task<int> InsertDnyEntry(NpgsqlConnection connection, DnyPost dny, DateTime timestamp)
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

            return dbExecuteService.ExecuteScalarAsync<int>(connection, sql, parameters);
        }

        private async Task InsertTrains(NpgsqlConnection connection, IList<DnyTrainContainer> containers)
        {
            const string dataColumns = "hash_id";
            ILookup<string, DnyTrainContainer> lookup = containers.ToLookup(c => c.Train.I);

            string getValues = SqlQueryHelper.Format(lookup.Count, "@hashId{0}", ",");
            string getSql = $"SELECT id, {dataColumns} FROM trains WHERE hash_id in ({getValues});";
            KeyValueSet getParameters = new KeyValueSet()
                .Add(SqlQueryHelper.GetParameters("hashId", lookup.Select(p => p.Key)));

            IDataRecord[] getResult = await dbExecuteService.ExecuteSelectAllAsync(connection, getSql, getParameters);
            SetTrainIds(getResult);

            string[] missingHashIds = containers.Where(c => c.TrainId == 0).Select(c => c.Train.I).Distinct().ToArray();
            if (missingHashIds.Length == 0) return;

            string insertValues = SqlQueryHelper.Format(missingHashIds, "(@hashId{0})", ",");
            string insertSql = $"INSERT INTO trains ({dataColumns}) VALUES {insertValues} RETURNING id, {dataColumns};";
            KeyValueSet insertParameters = new KeyValueSet()
                .Add(SqlQueryHelper.GetParameters("hashId", missingHashIds));

            IDataRecord[] insertResult = await dbExecuteService.ExecuteSelectAllAsync(connection, insertSql, insertParameters);
            SetTrainIds(insertResult);

            if (containers.Any(c => c.TrainId == 0)) throw new Exception("Missing train id");

            void SetTrainIds(IDataRecord[] result)
            {
                foreach (IDataRecord data in result)
                {
                    int id = data.GetValue<int>("id");
                    string hashId = data.GetValue<string>("hash_id") ?? string.Empty;

                    foreach (DnyTrainContainer container in lookup[hashId])
                    {
                        container.TrainId = id;
                    }
                }
            }
        }

        private async Task InsertTrainDays(NpgsqlConnection connection, IList<DnyTrainContainer> containers)
        {
            await InsertTrains(connection, containers);

            const string dataColumns = "train_id, date";
            ILookup<int, DnyTrainContainer> lookup = containers.ToLookup(c => c.TrainId);

            string getWheres = SqlQueryHelper.Format(containers, "train_id = @trainId{0} AND date = @date{0}", " OR ");
            string getSql = $"SELECT id, {dataColumns} FROM train_days WHERE {getWheres};";
            KeyValueSet getParameters = new KeyValueSet()
                .Add(SqlQueryHelper.GetParameters("trainId", containers.Select(c => c.TrainId)))
                .Add(SqlQueryHelper.GetParameters("date", containers.Select(c => c.Date)));

            IDataRecord[] getResult = await dbExecuteService.ExecuteSelectAllAsync(connection, getSql, getParameters);
            SetTrainDayIds(getResult);

            DnyTrainContainer[] missingContainers = containers
                .Where(c => c.TrainDayId == 0)
                .Distinct(TrainDayEqualityComparer.Instance)
                .ToArray();
            if (missingContainers.Length == 0) return;

            string insertValues = SqlQueryHelper.Format(missingContainers, "(@trainId{0}, @date{0})", ",");
            string insertSql = $"INSERT INTO train_days ({dataColumns}) VALUES {insertValues} RETURNING id, {dataColumns};";
            KeyValueSet insertParameters = new KeyValueSet()
                .Add(SqlQueryHelper.GetParameters("trainId", missingContainers.Select(c => c.TrainId)))
                .Add(SqlQueryHelper.GetParameters("date", missingContainers.Select(c => c.Date)));

            IDataRecord[] insertResult = await dbExecuteService.ExecuteSelectAllAsync(connection, insertSql, insertParameters);
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

        private async Task InsertTrainInfo(NpgsqlConnection connection, DnyTrainContainer[] containers)
        {
            const string dataColumns = "name, destination, product_class";
            ILookup<string, DnyTrainContainer> lookup = containers.ToLookup(c => c.Train.N);

            string getWheres = SqlQueryHelper.Format(containers, "name = @name{0} AND destination = @destination{0} AND product_class = @productClass{0}", " OR ");
            string getSql = $"SELECT id, {dataColumns} FROM dny_train_infos WHERE {getWheres};";
            KeyValueSet getParameters = new KeyValueSet()
                .Add(SqlQueryHelper.GetParameters("name", containers.Select(c => c.Train.N)))
                .Add(SqlQueryHelper.GetParameters("destination", containers.Select(c => c.Train.L)))
                .Add(SqlQueryHelper.GetParameters("productClass", containers.Select(c => c.ProductClass)));

            IDataRecord[] getResult = await dbExecuteService.ExecuteSelectAllAsync(connection, getSql, getParameters);
            SetDnyTrainInfoIds(getResult);

            DnyTrainContainer[] missingContainers = containers
                .Where(c => c.DnyTrainInfoId == 0)
                .Distinct(DnyTrainInfoEqualityComparer.Instance)
                .ToArray();
            if (missingContainers.Length == 0) return;

            string insertValues = SqlQueryHelper.Format(missingContainers, "(@name{0}, @destination{0}, @productClass{0})", ",");
            string insertSql = $"INSERT INTO dny_train_infos ({dataColumns}) VALUES {insertValues} RETURNING id, {dataColumns};";
            KeyValueSet insertParameters = new KeyValueSet()
                .Add(SqlQueryHelper.GetParameters("name", missingContainers.Select(c => c.Train.N)))
                .Add(SqlQueryHelper.GetParameters("destination", missingContainers.Select(c => c.Train.L)))
                .Add(SqlQueryHelper.GetParameters("productClass", missingContainers.Select(c => c.ProductClass)));

            IDataRecord[] insertResult = await dbExecuteService.ExecuteSelectAllAsync(connection, insertSql, insertParameters);
            SetDnyTrainInfoIds(insertResult);

            if (containers.Any(c => c.DnyTrainInfoId == 0)) throw new Exception("Missing dny_train_info id");

            void SetDnyTrainInfoIds(IDataRecord[] result)
            {
                foreach (IDataRecord data in result)
                {
                    int id = data.GetValue<int>("id");
                    string name = data.GetValue<string>("name") ?? string.Empty;
                    string destination = data.GetValue<string>("destination") ?? string.Empty;
                    short productClass = data.GetValue<short>("product_class");

                    foreach (DnyTrainContainer container in lookup[name])
                    {
                        if (container.Train.L == destination && container.ProductClass == productClass) container.DnyTrainInfoId = id;
                    }
                }
            }
        }

        private Task InsertDnyTrains(NpgsqlConnection connection, int dnyId, DnyTrain[] allTrains)
        {
            return SqlQueryHelper.DoInGroups(allTrains, 1000, async group =>
            {
                DnyTrainContainer[] containers = group.Select(t => new DnyTrainContainer(t)).ToArray();
                await InsertTrainDays(connection, containers);
                await InsertTrainInfo(connection, containers);

                string insertValues = SqlQueryHelper.Format(containers,
                    "(@dnyId, @trainDayId{0}, @dnyTrainInfoId{0}, @latitude{0}, @longitude{0}, @direction{0}, @delay{0})", ",");
                string insertSql = $@"
                    INSERT INTO dny_train_days (dny_id, train_day_id, dny_train_info_id, latitude, longitude, direction, delay)
                    VALUES {insertValues};
                ";
                KeyValueSet parameters = new KeyValueSet("dnyId", dnyId)
                    .Add(SqlQueryHelper.GetParameters("trainDayId", containers.Select(c => c.TrainDayId)))
                    .Add(SqlQueryHelper.GetParameters("dnyTrainInfoId", containers.Select(c => c.DnyTrainInfoId)))
                    .Add(SqlQueryHelper.GetParameters("latitude", containers.Select(c => c.Latitude)))
                    .Add(SqlQueryHelper.GetParameters("longitude", containers.Select(c => c.Longitude)))
                    .Add(SqlQueryHelper.GetParameters("direction", containers.Select(c => c.Direction)))
                    .Add(SqlQueryHelper.GetParameters("delay", containers.Select(c => c.Delay)));
                await dbExecuteService.ExecuteNonQueryAsync(connection, insertSql, parameters);
            });
        }

        private Task SetDnySuccessfull(NpgsqlConnection connection, int dnyId)
        {
            const string sql = "UPDATE dnys SET success = TRUE WHERE id = @id;";
            KeyValueSet parameters = new KeyValueSet("id", dnyId);
            return dbExecuteService.ExecuteNonQueryAsync(connection, sql, parameters);
        }

        public async Task Insert(DnyPost dny, DateTime timestamp)
        {
            try
            {
                await insertSemaphore.WaitAsync();
                await dbExecuteService.RunTransaction(async connection =>
                {
                    int dnyId = await InsertDnyEntry(connection, dny, timestamp);
                    await InsertDnyTrains(connection, dnyId, dny.T);
                    await SetDnySuccessfull(connection, dnyId);
                });
            }
            finally
            {
                insertSemaphore.Release();
            }
        }
    }
}
