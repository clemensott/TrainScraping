using System;

namespace TrainScrapingCommon.Models.RequestBody
{
    public class PostDnyBody : RequestBodyBase
    {
        public DateTime Timestamp { get; set; }

        public Dny Dny { get; set; }
    }
}
