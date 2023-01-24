namespace TrainScrapingCommon.Models.Dnys
{
    public class DnyTrain
    {
        /// <summary>
        /// Longitude (e.g.: 15624421 => 15.624421)
        /// </summary>
        public string X { get; set; }

        /// <summary>
        /// Latitude (e.g.: 46712598 => 46.712598)
        /// </summary>
        public string Y { get; set; }

        /// <summary>
        /// Name (e.g.: REX 1983)
        /// </summary>
        public string N { get; set; }

        /// <summary>
        /// Train ID (e.g.: 84/286240/18/19/181)
        /// </summary>
        public string I { get; set; }

        /// <summary>
        /// Direction (e.g.: 29) (0 => east, 8 => north, 16 => west, 24 => south)
        /// </summary>
        public string D { get; set; }

        /// <summary>
        /// Product class (e.g.: 16)
        /// </summary>
        public string C { get; set; }

        /// <summary>
        /// Date (e.g.: 16.12.2022)
        /// </summary>
        public string R { get; set; }

        /// <summary>
        /// Delay (in minutes)
        /// </summary>
        public string Rt { get; set; }

        /// <summary>
        /// Destination (e.g.: Bischofshofen Bahnhof)
        /// </summary>
        public string L { get; set; }
    }
}
