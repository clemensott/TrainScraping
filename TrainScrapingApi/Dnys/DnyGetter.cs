using System.Data;
using TrainScrapingApi.DB;
using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingApi.Dnys
{
    static class DnyGetter
    {
        public static async Task<IEnumerable<DnyMeta>> GetMetas(DateTime start, DateTime? end, int limit)
        {
            string whereEnd = end.HasValue ? " timestamp < @end" : string.Empty;
            string sql = $"SELECT * FROM dnys WHERE timestamp >= @start{whereEnd} ORDER BY timestamp LIMIT @limit;";
            KeyValueSet parameters = new KeyValueSet("start", start, "limit", limit);

            if (end.HasValue) parameters.Add("end", end.Value);

            IDataRecord[] result = await DbHelper.DefaultConnection.ExecuteSelectAllAsync(sql, parameters);
            return result.Select(GetDnyMeta);
        }

        private static DnyMeta GetDnyMeta(IDataRecord data)
        {
            return new DnyMeta()
            {
                Id = data.GetValue<int>("id"),
                Ts = data.GetValue<TimeSpan>("time").ToString(),
                X0 = data.GetCoor("min_longitude"),
                X1 = data.GetCoor("max_longitude"),
                Y1 = data.GetCoor("min_latitude"),
                Y2 = data.GetCoor("max_latitude"),
                N = data.GetValue<short>("trains_count").ToString(),
                Timestamp = data.GetValue<DateTime>("timestamp"),
            };
        }

        public static async Task<IEnumerable<Dny>> GetDnys(int[] ids)
        {
            if (ids.Length == 0) return Enumerable.Empty<Dny>();

            string whereIds = DbHelper.Format(ids, "@id{0}", ",");
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
                .Add(DbHelper.GetParameters("id", ids));

            IDataRecord[] result = await DbHelper.DefaultConnection.ExecuteSelectAllAsync(sql, parameters);
            IEnumerable<IGrouping<int, IDataRecord>> groups = result.GroupBy(d => d.GetValue<int>("id"));
            return groups.Select(GetDny);
        }

        private static Dny GetDny(IGrouping<int, IDataRecord> group)
        {
            IDataRecord first = group.First();
            return new Dny()
            {
                Id = group.Key,
                Ts = first.GetValue<TimeSpan>("time").ToString(),
                X0 = first.GetCoor("min_longitude"),
                X1 = first.GetCoor("max_longitude"),
                Y1 = first.GetCoor("min_latitude"),
                Y2 = first.GetCoor("max_latitude"),
                N = first.GetValue<short>("trains_count").ToString(),
                Timestamp = first.GetValue<DateTime>("timestamp"),
                T = group.Select(GetDnyTrain).ToArray(),
            };
        }

        private static DnyTrain GetDnyTrain(IDataRecord data)
        {
            return new DnyTrain()
            {
                X = data.GetCoor("longitude"),
                Y = data.GetCoor("latitude"),
                N = data.GetValue<string>("name"),
                I = data.GetValue<string>("hash_id"),
                D = data.GetValue<short>("direction").ToString(),
                C = data.GetValue<short>("product_class").ToString(),
                R = data.GetValue<DateTime>("date").ToString("dd.MM.YY"),
                Rt = data.GetValue<int>("delay").ToString(),
            };
        }

        private static string GetCoor(this IDataRecord record, string name)
        {
            return record.GetValue<decimal>(name).ToString().Replace(".", "").Replace(",", "");
        }
    }
}
