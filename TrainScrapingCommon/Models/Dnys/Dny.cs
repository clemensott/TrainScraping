using System;

namespace TrainScrapingCommon.Models.Dnys
{
    public class Dny : DnyPost
    {
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
