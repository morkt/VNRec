//! \file       VNDBCache.cs
//! \date       Sun Aug 10 20:58:10 2014
//! \brief      local cache of the VNDB.
//
// Copyright (C) 2014 by morkt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VNDB
{
    public class VNDBCache : IDisposable
    {
        public static readonly Dictionary<int, User>  Users = new Dictionary<int, User>();
        public static readonly Dictionary<int, Entry> Games = new Dictionary<int, Entry>();
        public static readonly Dictionary<int, int> TotalGameVotes = new Dictionary<int, int>();
        public static readonly Dictionary<int, int> TotalGameRating = new Dictionary<int, int>();

        private SQLiteConnection m_connection;

        public SQLiteConnection Connection { get { return m_connection; } }

        public VNDBCache (string db_file)
        {
            m_connection = new SQLiteConnection (string.Format ("Data Source={0};Version=3;", db_file));
            m_connection.Open();

            string sql = @"SELECT title_id, user_id, score, title, original, released "+
                         @"FROM votes LEFT JOIN novels ON votes.title_id=novels.id";
            using (var command = new SQLiteCommand (sql, m_connection))
            using (var reader = command.ExecuteReader())
            while (reader.Read())
            {
                int title_id = reader.GetInt32 (0);
                int user_id = reader.GetInt32 (1);
                int score = reader.GetInt32 (2);
                string title = null;
                string original = null;
                DateTime released = default(DateTime);
                if (!reader.IsDBNull (3)) title = reader.GetString (3);
                if (!reader.IsDBNull (4)) original = reader.GetString (4);
                if (!reader.IsDBNull (5)) released = reader.GetDateTime (5);

                if (!Users.ContainsKey (user_id))
                    Users[user_id] = new User (user_id);
                Users[user_id].Votes[title_id] = score;

                if (!Games.ContainsKey (title_id))
                    Games[title_id] = new Entry
                    {
                        Id = title_id,
                        Title = title,
                        Original = original,
                        Released = released,
                    };
            }
            FilterUsers();
        }

        static private void FilterUsers ()
        {
            int removedUserForVoteCount=0, removedUserForVoteScore=0;
		
            Trace.WriteLine (string.Format ("Initial user count: {0}", Users.Count));
            var to_remove = new List<int>();
            foreach (var user in Users.Values)
            {
                if (user.Votes.Count < 5)
                {
                    to_remove.Add (user.Id);
                    removedUserForVoteCount++;
                    continue;
                }
                int first = user.Votes.First().Value;
                if (!user.Votes.Skip (1).Any (v => v.Value != first))
                {
                    to_remove.Add (user.Id);
                    removedUserForVoteScore++;
                }
                else
                {
                    foreach (var vote in user.Votes)
                    {
                        if (TotalGameVotes.ContainsKey (vote.Key))
                        {
                            TotalGameVotes[vote.Key]++;
                            TotalGameRating[vote.Key] += vote.Value;
                        }
                        else
                        {
                            TotalGameVotes[vote.Key] = 1;
                            TotalGameRating[vote.Key] = vote.Value;
                        }
                    }
                }
            }
            foreach (var id in to_remove)
                Users.Remove (id);

            Trace.WriteLine (string.Format ("Culled user count: {0}, removedUserForVoteCount: {1}, removedUserForVoteScore: {2}", Users.Count, removedUserForVoteCount, removedUserForVoteScore));
        }

        /// <summary>
        /// Request information lookup for specified novel ids.
        /// </summary>
        public void Update (IEnumerable<int> novels)
        {
            novels = novels.Distinct().Where (id => !Games.ContainsKey (id) || null == Games[id].Title);
            if (!novels.Any())
                return;
            using (var conn = new Connection())
            using (var session = new Session (conn))
            {
                int total = novels.Count();
                string query = string.Format ("get vn basic (id={0})", JsonConvert.SerializeObject (novels));
                if (total > 10)
                    query += string.Format (" {{\"results\":{0}}}", total);
                var response = session.Send (query);
                var results = session.ParseResponse (response);
                if (null == results || results.Item1 != "results")
                    return;
                Trace.WriteLine (results.Item2, "server response");
                var r = JObject.Parse (results.Item2);
                var items = r.GetValue ("items");
                if (!items.Any())
                    return;
                using (var transaction = m_connection.BeginTransaction())
                using (var command = m_connection.CreateCommand())
                {
                    var title_id = command.CreateParameter();
                    var title    = command.CreateParameter();
                    var original = command.CreateParameter();
                    var released = command.CreateParameter();
                    command.CommandText = @"INSERT OR REPLACE INTO novels (id, title, original, released) VALUES (?,?,?,?)";
                    command.Parameters.Add (title_id);
                    command.Parameters.Add (title);
                    command.Parameters.Add (original);
                    command.Parameters.Add (released);
                    foreach (var item in items)
                    {
                        var entry = new Entry
                        {
                            Id       = item.Value<int> ("id"),
                            Title    = item.Value<string> ("title"),
                            Original = item.Value<string> ("original"),
                            Released = item.Value<DateTime> ("released"),
                        };
                        Trace.WriteLine (string.Format ("id:{0}, title:{1}", entry.Id, entry.Title));
                        title_id.Value = entry.Id;
                        title.Value = entry.Title;
                        original.Value = entry.Original;
                        released.Value = entry.Released;
                        command.ExecuteNonQuery();
                        Games[entry.Id] = entry;
                    }
                    transaction.Commit();
                }
            }
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
                    m_connection.Close();
                }
                disposed = true;
            }
        }
        #endregion
    }
}
