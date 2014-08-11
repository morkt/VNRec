//! \file       VNRecLinearRegression.cs
//! \date       Mon Aug 11 05:55:52 2014
//! \brief      Linear regression recommendations algorithm implementation.
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
    public class VNRecLinearRegression : IRecAlgorithm, IDisposable
    {
        private readonly SQLiteCommand m_statement;

        public VNRecLinearRegression (SQLiteConnection conn)
        {
            m_statement = conn.CreateCommand();
        }

        public void Update (IDictionary<int, int> totalGameVotes)
        {
            Trace.WriteLine ("Update VN popularity stats...");

            m_statement.Parameters.Clear();
            m_statement.CommandText = @"DROP TABLE IF EXISTS GameRegression";
            m_statement.ExecuteNonQuery();
            m_statement.CommandText = @"CREATE TABLE GameRegression ( gameId integer not null, secondGameId integer not null, gradient double not null, constant double not null, overlap integer not null, correlation double not null, PRIMARY KEY (gameId,secondGameId) )";
            m_statement.ExecuteNonQuery();
            m_statement.CommandText = 
                @"SELECT COUNT(*) AS total, SUM(Vote1.score) AS x, SUM(Vote1.score*Vote1.score) AS x2, "+
                @" SUM(Vote2.score) AS y, SUM(Vote2.score*Vote2.score) AS y2, SUM(Vote1.score*Vote2.score) AS xy "+
                @"FROM votes AS Vote1 INNER JOIN votes AS Vote2 "+
                @"WHERE Vote1.user_id=Vote2.user_id AND Vote1.title_id=? AND Vote2.title_id=?";
            m_statement.Parameters.Add (new SQLiteParameter (DbType.Int32));
            m_statement.Parameters.Add (new SQLiteParameter (DbType.Int32));

            using (var sql_insert = new SQLiteCommand (@"INSERT INTO GameRegression (gameId,secondGameId,gradient,constant,overlap,correlation) VALUES (?,?,?,?,?,?)", m_statement.Connection))
            {
                for (int i = 0; i < 6; ++i)
                    sql_insert.Parameters.Add (new SQLiteParameter());
                using (var transaction = sql_insert.Connection.BeginTransaction())
                {
                    foreach (var votes in totalGameVotes.Where (v => v.Value >= 150))
                    {
                        foreach (var j in totalGameVotes.Where (v => votes.Key != v.Key && v.Value >= 150))
                        {
                            m_statement.Parameters[0].Value = votes.Key;
                            m_statement.Parameters[1].Value = j.Key;
                            using (var reader = m_statement.ExecuteReader())
                            {
                                if (!reader.Read())
                                    continue;
                                int total = reader.GetInt32 (reader.GetOrdinal ("total"));
                                if (total < 5)
                                    continue;
                                double x = reader.GetDouble (reader.GetOrdinal ("x"));
                                double y = reader.GetDouble (reader.GetOrdinal ("y"));
                                double x2 = reader.GetDouble (reader.GetOrdinal ("x2"));
                                double y2 = reader.GetDouble (reader.GetOrdinal ("y2"));
                                double xy = reader.GetDouble (reader.GetOrdinal ("xy"));
                                double denominator = total * x2 - x * x;
                                double corr_denominator = Math.Sqrt (total*x2-x*x) * Math.Sqrt (total*y2-y*y);
                                if (denominator != 0 && corr_denominator != 0)
                                {
                                    double constant = (y*x2 - x*xy) / denominator;
                                    double gradient = (total*xy - x*y) / denominator;
                                    double correlation = (total*xy - x*y) / corr_denominator;

                                    //System.out.println("Calculate "+result.getInt("total")+" "+result.getDouble("x")+" "+result.getDouble("x2"));
                                    //System.out.println("constant: "+constant+" gradient: "+gradient+" correlation: "+correlation);
                                    sql_insert.Parameters[0].Value = votes.Key;
                                    sql_insert.Parameters[1].Value = j.Key;
                                    sql_insert.Parameters[2].Value = gradient;
                                    sql_insert.Parameters[3].Value = constant;
                                    sql_insert.Parameters[4].Value = total;
                                    sql_insert.Parameters[5].Value = correlation;
                                    sql_insert.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    transaction.Commit();
                }
            }
            Trace.WriteLine ("Done update");
        }

        public IEnumerable<Tuple<int, double>> FindRecommendations (VNDB.User pUser, int amount)
        {
            var gameScore = new Dictionary<int, double>();
            var confidence = new Dictionary<int, double>();

            m_statement.CommandText = "SELECT secondGameId, gradient, constant, overlap, correlation FROM GameRegression WHERE gameId=?";
            m_statement.Parameters.Clear();
            m_statement.Parameters.Add (new SQLiteParameter (DbType.Int32));

            foreach (var vote in pUser.Votes)
            {
                m_statement.Parameters[0].Value = vote.Key;
                using (var reader = m_statement.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int overlap = reader.GetInt32 (reader.GetOrdinal ("overlap"));
                        if (overlap >= 15)
                        {
                            //double weight = Math.abs(Math.log(result.getInt("overlap")/5)*result.getDouble("correlation"));
                            //double weight = ((result.getInt("overlap"))*(10+Math.abs(result.getDouble("correlation"))));
                            double weight = Math.Abs (Math.Log (overlap));
                            //double weight = Math.abs(result.getDouble("correlation"));
                            //double weight = 1;

                            int id = reader.GetInt32 (reader.GetOrdinal ("secondGameId"));
                            double gradient = reader.GetDouble (reader.GetOrdinal ("gradient"));
                            double constant = reader.GetDouble (reader.GetOrdinal ("constant"));
                            var score = weight * (constant + vote.Value * gradient);
                            if (gameScore.ContainsKey (id))
                            {
                                gameScore[id]  += score;
                                confidence[id] += weight;
                            }
                            else
                            {
                                gameScore[id]  = score;
                                confidence[id] = weight;
                            }
                        }
                    }
                }
            }
            var recommend = new List<Tuple<int, double>> (amount);
            var used = new HashSet<int> (pUser.Votes.Keys);

            for (int i = 0; i < amount; i++)
            {
                double bestScore = 0;
                int bestRecc = -1;

                foreach (var game in confidence.Where (s => !used.Contains (s.Key) && s.Value >= 1))
                {
                    int j = game.Key;
                    //double score = gameScore[j]/confidence[j];
                    double score = gameScore[j] / game.Value - VNDB.VNDBCache.TotalGameRating[j] / Math.Max (1.0, VNDB.VNDBCache.TotalGameVotes[j]);

                    if ((bestRecc < 0 || score > bestScore) && gameScore[j]/game.Value >= 65)
                    {
                        bestScore = score;
                        bestRecc = j;
                    }
                }
                double prediction = 0;
                if (bestRecc >= 0)
                {
                    prediction = gameScore[bestRecc]/Math.Max (1.0, confidence[bestRecc])/10;
                    //recommend[i][2] = confidence[bestRecc];
//                    recommend[i][2] = gameScore[bestRecc]/Math.max(1,confidence[bestRecc]);
//                    recommend[i][3] = confidence[bestRecc];
                    used.Add (bestRecc);
                    recommend.Add (new Tuple<int, double> (bestRecc, prediction));
                }
            }
            return recommend;
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
