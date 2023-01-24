using System;
using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingCommon.Models.RequestBody
{
    public class PostDnyBody : RequestBodyBase
    {
        public DateTime Timestamp { get; set; }

        public DnyPost Dny { get; set; }
    }
}
