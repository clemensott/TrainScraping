using System.Collections;
using System.Collections.Generic;

namespace TrainScrapingApi.DB
{
    public class KeyValueSet : IEnumerable<KeyValuePair<string, object>>
    {
        private readonly List<KeyValuePair<string, object>> dict;

        public KeyValueSet()
        {
            dict = new List<KeyValuePair<string, object>>();
        }

        public KeyValueSet(IEnumerable<KeyValuePair<string, object>> src) : this()
        {
            foreach (KeyValuePair<string, object> pair in src) Add(pair.Key, pair.Value);
        }

        public KeyValueSet(string key1, object value1) : this((key1, value1))
        {
        }

        public KeyValueSet(string key1, object value1, string key2, object value2) : this((key1, value1), (key2, value2))
        {
        }

        public KeyValueSet(string key1, object value1, string key2, object value2, string key3, object value3)
            : this((key1, value1), (key2, value2), (key3, value3))
        {
        }

        public KeyValueSet(string key1, object value1, string key2, object value2, string key3, object value3, string key4, object value4)
            : this((key1, value1), (key2, value2), (key3, value3), (key4, value4))
        {
        }

        public KeyValueSet(params (string key, object value)[] pairs) : this()
        {
            foreach ((string key, object value) in pairs) Add(key, value);
        }

        public KeyValueSet Add(string key, object value = null)
        {
            dict.Add(new KeyValuePair<string, object>(key, value));

            return this;
        }

        public KeyValueSet Add(IEnumerable<KeyValuePair<string, object>> src)
        {
            foreach (KeyValuePair<string, object> pair in src) Add(pair.Key, pair.Value);

            return this;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
