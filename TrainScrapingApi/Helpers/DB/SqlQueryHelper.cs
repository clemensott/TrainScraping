using System.Data;

namespace TrainScrapingApi.Helpers.DB
{
    static class SqlQueryHelper
    {
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
    }
}
