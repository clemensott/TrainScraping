using System;
using System.IO;
using System.Xml.Serialization;

namespace TrainScraping.Configuration
{
    public class Config
    {
        private static XmlSerializer serializer = new XmlSerializer(typeof(Config));

        public string ApiBaseUrl { get; set; }

        public string ApiToken { get; set; }
        
        public int DnyUploadIntervalSeconds { get; set; }

        public DnyScrapingConfig[] DNYs { get; set; }

        public static Config Load()
        {
            return Load(Path.Combine(AppContext.BaseDirectory, "config.xml"));
        }

        public static Config Load(string path)
        {
                string xmlText = File.ReadAllText(path);

                return (Config)serializer.Deserialize(new StringReader(xmlText));
        }

        public void Serialize()
        {
            StringWriter writer = new StringWriter();
            serializer.Serialize(writer, this);
            string xml = writer.ToString();
        }
    }
}
