namespace TrainScrapingCommon.Models
{
    public class Dny
    {
        /// <summary>
        /// Trains
        /// </summary>
        public DnyTrain[] T { get; set; }

        /// <summary>
        /// Time (e.g.: 19:48:12)
        /// </summary>
        public string Ts { get; set; }

        /// <summary>
        /// Min Longitude (e.g.: -26573730 => -26.573730)
        /// </summary>
        public string X0 { get; set; }

        /// <summary>
        /// Max Longitude (e.g.: 56834473 => -56.834473)
        /// </summary>
        public string X1 { get; set; }

        /// <summary>
        /// Min Latitude (e.g.: 68886595 => 68.886595)
        /// </summary>
        public string Y1 { get; set; }

        /// <summary>
        /// Min Latitude (e.g.: 34251118 => 34.251118)
        /// </summary>
        public string Y2 { get; set; }

        /// <summary>
        /// Count of Trains (e.g. 237)
        /// </summary>
        public string N { get; set; }
    }
}
