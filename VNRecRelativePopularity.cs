//! \file       VNRecRelativePopularity.cs
//! \date       Mon Aug 11 03:30:45 2014
//! \brief      Relative popularity recommendation algorithm implementation.
//
// Original code by raistlin.
//
// .Net port by morkt (c) 2014
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;

namespace VNRec
{
    public class VNRecRelativePopularity : IRecAlgorithm, IDisposable
    {
        private readonly SQLiteCommand m_statement;

        public VNRecRelativePopularity (SQLiteConnection conn)
        {
            m_statement = conn.CreateCommand();
        }

        public void Update (IDictionary<int, int> totalGameVotes, IDictionary<int, VNDB.User> users)
        {
            Trace.WriteLine ("Update VN popularity stats...");

            m_statement.Parameters.Clear();
            m_statement.CommandText = "DROP TABLE IF EXISTS GameOverlap";
            m_statement.ExecuteNonQuery();
            m_statement.CommandText = "CREATE TABLE GameOverlap ( gameId integer not null, secondGameId integer not null, popularity double not null, PRIMARY KEY (gameId,secondGameId) )";
            m_statement.ExecuteNonQuery();

            int totalRemainUserVotes = 0;
            var remainUserVotes = new Dictionary<int, int> (totalGameVotes.Count);
            foreach (var user in users.Values)
            {
                totalRemainUserVotes += user.Votes.Count;
                foreach (var title_id in user.Votes.Keys)
                {
                    if (remainUserVotes.ContainsKey (title_id))
                        remainUserVotes[title_id]++;
                    else
                        remainUserVotes[title_id] = 1;
                }
            }
            //System.out.println("totalRemainUserVotes:"+totalRemainUserVotes);

            m_statement.CommandText = "INSERT INTO GameOverlap (gameId,secondGameId,popularity) VALUES (?,?,?)";
            m_statement.Parameters.Add (m_statement.CreateParameter());
            m_statement.Parameters.Add (m_statement.CreateParameter());
            m_statement.Parameters.Add (m_statement.CreateParameter());

            using (var transaction = m_statement.Connection.BeginTransaction())
            {
                foreach (var title in totalGameVotes.Where (t => t.Value >= 150))
                {
                    var relatedVotes = new Dictionary<int, int> (totalGameVotes.Count);
                    int totalRelatedVotes = 0;

                    foreach (var user in users.Values)
                    {
                        bool found = false;
                        foreach (var title_id in user.Votes.Keys)
                        {
                            if (title_id == title.Key)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            totalRelatedVotes += user.Votes.Count-1;
                            foreach (var title_id in user.Votes.Keys)
                            {
                                if (relatedVotes.ContainsKey (title_id))
                                    relatedVotes[title_id]++;
                                else
                                    relatedVotes[title_id] = 1;
                            }
                        }
                    }
                    foreach (var j in totalGameVotes.Where (t => t.Value >= 150))
                    {
                        if (title.Key != j.Key)
                        {
                            int related = 0, remain = 0;
                            relatedVotes.TryGetValue (j.Key, out related);
                            remainUserVotes.TryGetValue (j.Key, out remain);
                            double relativePopularity = related / (double)totalRelatedVotes - remain / (double)totalRemainUserVotes;
                            if (relativePopularity >= 0.001)
                            {
                                m_statement.Parameters[0].Value = title.Key;
                                m_statement.Parameters[1].Value = j.Key;
                                m_statement.Parameters[2].Value = relativePopularity;
                                m_statement.ExecuteNonQuery();
                            }
                        }
                    }
                }
                transaction.Commit();
            }
            Trace.WriteLine ("Done update");
        }

        public IEnumerable<Tuple<int, double>> FindRecommendations (VNDB.User pUser, int amount)
        {
            var gameScore = new Dictionary<int, double> ();
            var used = new HashSet<int> (pUser.Votes.Keys);

            m_statement.CommandText = "SELECT secondGameId, popularity FROM GameOverlap WHERE gameId=?";
            m_statement.Parameters.Clear();
            m_statement.Parameters.Add (new SQLiteParameter (DbType.Int32));

            foreach (var vote in pUser.Votes)
            {
                m_statement.Parameters[0].Value = vote.Key;
                using (var reader = m_statement.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32 (0);
                        double value = (vote.Value - 50) * reader.GetDouble (1);
                        if (gameScore.ContainsKey (id))
                            gameScore[id] += value;
                        else
                            gameScore[id] = value;
                    }
                }
            }

            return gameScore.Where (g => !used.Contains (g.Key) && g.Value > 0)
                            .OrderByDescending (g => g.Value)
                            .Take (amount)
                            .Select (r => new Tuple<int, double> (r.Key, r.Value));
        }

        #region IDisposable Members
        bool disposed = false;

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    m_statement.Dispose();
                }
                disposed = true;
            }
        }
        #endregion
    }
}
