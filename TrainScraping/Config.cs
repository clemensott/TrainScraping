using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TrainScraping
{
    class Config
    {
        const string dnyIntervalKey = "DNY_INTERVAL";
        const string dnyDownloadPathKey = "DNY_DOWNLOAD_PATH";
        const string dnyURLKey = "DNY_URL";
        const string trainInfoURLKey = "TRAIN_INFO_URL";

        public TimeSpan DnyInterval { get; private set; }

        public string DnyDownloadPath { get; private set; }

        public string DnyURL { get; private set; }

        public string TrainInfoURL { get; private set; }

        public Config(TimeSpan dnyInterval, string dnyDownloadPath, string dnyURL, string trainInfoURL)
        {
            DnyDownloadPath = dnyDownloadPath;
            DnyInterval = dnyInterval;
            DnyURL = dnyURL;
            TrainInfoURL = trainInfoURL;
        }

        private static KeyValuePair<string, string> ParseEnvLine(string line)
        {
            int hashIndex = line.IndexOf('#');
            if (hashIndex != -1)
            {
                line = line.Remove(hashIndex);
            }
            string[] parts = line.Split(new char[] { '=' }, 2);
            string key = parts[0].Trim();
            string value = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            return new KeyValuePair<string, string>(key, value);
        }

        private static Dictionary<string, string> ParseEnvLines(IEnumerable<string> lines)
        {
            return lines
                .Select(ParseEnvLine)
                .Where(p => !string.IsNullOrWhiteSpace(p.Key))
                .ToDictionary(p => p.Key, p => p.Value);
        }

        public static Config Load()
        {
            return Load(Path.Combine(AppContext.BaseDirectory, ".env"));
        }

        public static Config Load(string path)
        {
            try
            {
                string[] lines = File.Exists(path) ? File.ReadAllLines(path) : new string[0];
                Dictionary<string, string> env = ParseEnvLines(lines);

                TimeSpan dnyInterval;
                if (!env.ContainsKey(dnyIntervalKey) || !TimeSpan.TryParse(env[dnyIntervalKey], out dnyInterval))
                {
                    dnyInterval = TimeSpan.FromMinutes(10);
                }

                string dnyDownloadPath = env.ContainsKey(dnyDownloadPathKey) ? env[dnyDownloadPathKey] : "./dny_downloads";
                string dnyURL = env.ContainsKey(dnyURLKey) ? env[dnyURLKey] : "";
                string trainInfoURL = env.ContainsKey(trainInfoURLKey) ? env[trainInfoURLKey] : "";

                return new Config(dnyInterval, dnyDownloadPath, dnyURL, trainInfoURL);
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
                throw;
            }
        }
    }
}
