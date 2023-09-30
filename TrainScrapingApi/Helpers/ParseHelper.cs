namespace TrainScrapingApi.Helpers
{
    static class ParseHelper
    {
        /// <summary>
        /// Parses Date with Format: DD.MM.YY
        /// </summary>
        /// <param name="raw"></param>
        /// <returns>Date</returns>
        public static DateTime ParseDate(string raw)
        {
            try
            {
                string[] parts = raw.Split('.');
                int day = int.Parse(parts[0]);
                int month = int.Parse(parts[1]);
                int year = int.Parse(parts[2]);

                return new DateTime(2000 + year, month, day);
            }
            catch (Exception e)
            {
                throw new FormatException("Wrong format", e);
            }
        }

        /// <summary>
        /// Parses coordinate without decimal point to coordinate with decimal point.
        /// </summary>
        /// <param name="raw">coordinate without decimal point</param>
        /// <returns>Coordinate</returns>
        public static decimal ParseCoordinate(string raw)
        {
            int no = int.Parse(raw);
            return no / (decimal)1000000;
        }
    }
}
