//! \file       VNRecAlgorithm.cs
//! \date       Sun Aug 10 22:16:19 2014
//! \brief      VN Recommendation algorithm interface.
//

using System.Collections.Generic;

namespace VNRec
{
    public interface IRecAlgorithm
    {
        IEnumerable<System.Tuple<int, double>> FindRecommendations (VNDB.User user, int amount);
    }
}
