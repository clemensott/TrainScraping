using System.Data;
using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingApi.Extensions.ModelConverting
{
    static class SqlModelConverterExtensions
    {
        public static T? GetValue<T>(this IDataRecord record, string name, T? defaultValue = default)
        {
            object value = record[name];

            return value is DBNull ? defaultValue : (T)value;
        }

        public static DnyMeta GetDnyMeta(this IDataRecord data)
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

        public static Dny GetDny(IGrouping<int, IDataRecord> group)
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

        public static DnyTrain GetDnyTrain(IDataRecord data)
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

        public static string GetCoor(this IDataRecord record, string name)
        {
            return record.GetValue<decimal>(name).ToString().Replace(".", "").Replace(",", "");
        }
    }
}
