//! \file       ViewModel.cs
//! \date       Mon Aug 11 00:54:34 2014
//! \brief      vnrec view model implementation.
//

using System;

namespace VNRec
{
    public class Recommendation
    {
        public string Title { get; private set; }
        public double Score { get; private set; }
        public string   Uri { get; private set; }

        public Recommendation (Tuple<int, double> rec)
        {
            VNDB.Entry novel;
            if (VNDB.VNDBCache.Games.TryGetValue (rec.Item1, out novel))
                Title = novel.Title;
            else
                Title = "v"+rec.Item1.ToString();
            Score = rec.Item2;
            Uri = string.Format ("http://vndb.org/v{0}", rec.Item1);
        }
    }

    public class SimilarUser
    {
        public string Name { get; private set; }
        public string  Uri { get; private set; }
        public int      Id { get; private set; }

        public SimilarUser (int user_id)
        {
            Id = user_id;
            Uri = string.Format ("http://vndb.org/u{0}", user_id);
            Name = string.Format ("u{0}", user_id);
        }
    }
}
