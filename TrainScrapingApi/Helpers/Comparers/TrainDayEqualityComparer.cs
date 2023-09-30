using System.Diagnostics.CodeAnalysis;
using TrainScrapingApi.Models;

namespace TrainScrapingApi.Helpers.Comparers
{
    class TrainDayEqualityComparer : IEqualityComparer<DnyTrainContainer>
    {
        private static TrainDayEqualityComparer? instance;

        public static TrainDayEqualityComparer Instance => instance ??= new TrainDayEqualityComparer();


        public bool Equals([AllowNull] DnyTrainContainer x, [AllowNull] DnyTrainContainer y)
        {
            if (x == y) return true;
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            return x.TrainId == y.TrainId && x.Date == y.Date;
        }

        public int GetHashCode([DisallowNull] DnyTrainContainer obj)
        {
            return HashCode.Combine(obj.TrainId, obj.Date);
        }
    }
}
