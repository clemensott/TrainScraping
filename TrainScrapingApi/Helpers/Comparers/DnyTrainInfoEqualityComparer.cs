using System.Diagnostics.CodeAnalysis;
using TrainScrapingApi.Models;

namespace TrainScrapingApi.Helpers.Comparers
{
    class DnyTrainInfoEqualityComparer : IEqualityComparer<DnyTrainContainer>
    {
        public static DnyTrainInfoEqualityComparer Instance { get; } = new DnyTrainInfoEqualityComparer();


        public bool Equals([AllowNull] DnyTrainContainer x, [AllowNull] DnyTrainContainer y)
        {
            if (x == y) return true;
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            return x.Train.N == y.Train.N &&
                x.Train.L == y.Train.L &&
                x.ProductClass == y.ProductClass;
        }

        public int GetHashCode([DisallowNull] DnyTrainContainer obj)
        {
            return HashCode.Combine(obj.Train.N, obj.Train.L, obj.ProductClass);
        }
    }
}
