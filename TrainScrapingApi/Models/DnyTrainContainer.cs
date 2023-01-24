using System;
using System.Diagnostics;
using TrainScrapingApi.Helpers;
using TrainScrapingCommon.Models.Dnys;

namespace TrainScrapingApi.Models
{
    public class DnyTrainContainer
    {
        public int TrainId { get; set; }

        public int TrainDayId { get; set; }

        public int DnyTrainInfoId { get; set; }

        
        public DnyTrain Train { get; }

        public DateTime Date => ParseHelper.ParseDate(Train.R);

        public short ProductClass => short.Parse(Train.C);

        public short Direction => short.Parse(Train.D);
        
        public decimal Longitude => ParseHelper.ParseCoordinate(Train.X);
        
        public decimal Latitude => ParseHelper.ParseCoordinate(Train.Y);

        public int? Delay => Train.Rt != null ? (int?)int.Parse(Train.Rt) : null;

        public DnyTrainContainer(DnyTrain train)
        {
            Train = train;
        }
    }
}
