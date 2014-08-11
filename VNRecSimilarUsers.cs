//! \file       VNRecSimilarUsers.cs
//! \date       Sun Aug 10 22:28:31 2014
//! \brief      Similar users recommendation algorithm.
//
// Original code by raistlin.
//
// .Net port by morkt (c) 2014
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace VNRec
{
    public class VNRecSimilarUsers : IRecAlgorithm
    {
        private readonly IDictionary<int, VNDB.User> Users;
        public int MostSimilarUser { get; private set; }

        const int SimilarUsersLimit = 100;

        public VNRecSimilarUsers (IDictionary<int, VNDB.User> users)
        {
            Users = users;
        }

        public IEnumerable<Tuple<int, double>> FindRecommendations (VNDB.User pUser, int amount)
        {
            var similarUsers = new List<int> (SimilarUsersLimit);
            var similarScore = new List<double> (SimilarUsersLimit);
            MostSimilarUser = -1;

            foreach (var compareUser in Users.Values)
            {
                int overlap = 0;
                double scoreDiff = 0;

                if (pUser.Id == compareUser.Id)
                    continue;

                foreach (var vote in pUser.Votes)
                {
                    var altVotes = compareUser.Votes.Where (v => v.Key == vote.Key);
                    if (altVotes.Any())
                    {
                        var altVote = altVotes.First();
                        scoreDiff += (vote.Value-altVote.Value)*(vote.Value-altVote.Value);
                        overlap++;
                    }
                }
                if (overlap > 0)
                {
                    scoreDiff = Math.Sqrt (scoreDiff / overlap) / 100;
                    //double score = overlap/(10+scoreDiff);
                    double score = Math.Log (overlap) / (1 + scoreDiff);
                    //double score = Math.log(overlap*(1+overlap/(double)compareUser.votes.size()))/(1+scoreDiff);

                    int j = similarScore.FindIndex (s => score > s);
                    if (-1 != j)
                    {
                        if (SimilarUsersLimit == similarScore.Count)
                        {
                            similarUsers.RemoveAt (SimilarUsersLimit-1);
                            similarScore.RemoveAt (SimilarUsersLimit-1);
                        }
                        similarUsers.Insert (j, compareUser.Id);
                        similarScore.Insert (j, score);
                    }
                    else if (similarScore.Count < SimilarUsersLimit)
                    {
                        similarUsers.Add (compareUser.Id);
                        similarScore.Add (score);
                    }
                }
            }
            if (0 == similarUsers.Count)
                return new Tuple<int, double>[0]; // empty enumerable

            var gameScore = new Dictionary<int, double>();
            MostSimilarUser = similarUsers[0];

            //for (int i=0; i<10; i++)
            //	System.out.println("Similar user ("+i+"): "+users.get(similarUsers[i]).id+" "+similarScore[i]);
            for (int i = 0; i < similarUsers.Count; i++)
            {
                var user = Users[similarUsers[i]];
                foreach (var vote in user.Votes)
                {
                    if (gameScore.ContainsKey (vote.Key))
                        gameScore[vote.Key] += vote.Value * similarScore[i];
                    else
                        gameScore[vote.Key] = vote.Value * similarScore[i];
                }
            }
            var used = new HashSet<int> (pUser.Votes.Keys);
            return gameScore.Where (g => !used.Contains (g.Key))
                            .OrderByDescending (g => g.Value)
                            .Take (amount)
                            .Select (r => new Tuple<int, double> (r.Key, r.Value));
        }
    }
}
