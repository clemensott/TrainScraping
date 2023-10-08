using System.Xml.Serialization;

namespace TrainScrapingWorkerService.Configuration
{
    public class Config
    {
        private static XmlSerializer serializer = new XmlSerializer(typeof(Config));

        /// <summary>
        /// In what type of API should the data be uploaded
        /// </summary>
        public ApiTypeConfig ApiType { get; set; } = ApiTypeConfig.Rest;

        public string ApiBaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Part of the credentials for Rest, IndluxDB and QuestDB
        /// </summary>
        public string ApiToken { get; set; } = string.Empty;

        /// <summary>
        /// Bucket name for InfluxDB
        /// </summary>
        public string ApiBucket { get; set; } = string.Empty;

        /// <summary>
        /// Org for InfluxDB
        /// </summary>
        public string ApiOrg { get; set; } = string.Empty;

        /// <summary>
        /// Name of Scraper for InfluxDB and QuestDB
        /// </summary>
        public string ScraperName { get; set; } = string.Empty;

        public int DnyUploadIntervalSeconds { get; set; }

        public DnyScrapingConfig[] DNYs { get; set; } = new DnyScrapingConfig[0];

        public static Config Load()
        {
            return Load(Path.Combine(AppContext.BaseDirectory, "config.xml"));
        }

        public static Config Load(string path)
        {
            string xmlText = File.ReadAllText(path);

            return (serializer.Deserialize(new StringReader(xmlText)) as Config) ?? throw new Exception("Config is null");
        }

        public void Serialize()
        {
            StringWriter writer = new StringWriter();
            serializer.Serialize(writer, this);
            string xml = writer.ToString();
        }
    }
}
