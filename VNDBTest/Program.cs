using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VNDBTest
{
    class Program
    {
        static void LookupId (string[] args)
        {
            if (0 == args.Length)
            {
                Console.WriteLine ("Usage: vndbtest ID");
                return;
            }
            using (var connection = new VNDB.Connection ())
            using (var session = new VNDB.Session (connection))
            {
                string response = session.Send (string.Format ("get vn basic (id={0})", args[0]));
                Console.WriteLine (response);
            }
        }

        static void CreateCache (string[] args)
        {
            if (0 == args.Length)
            {
                Console.WriteLine ("Usage: vndbtest VNDB-VOTES-DUMP");
                return;
            }
            string path = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filename = Path.Combine (path, "VNDBCache.sqlite");
            SQLiteConnection.CreateFile (filename);
            var db_conn = new SQLiteConnection (string.Format ("Data Source={0};Version=3;", filename));
            db_conn.Open();

            string sql = @"CREATE TABLE votes (title_id INT NOT NULL, user_id INT NOT NULL, score INT NOT NULL, CONSTRAINT user_title PRIMARY KEY (title_id, user_id))";
            using (var command = new SQLiteCommand (sql, db_conn))
                command.ExecuteNonQuery();

            sql = @"CREATE TABLE users (id INTEGER NOT NULL PRIMARY KEY, name TEXT)";
            using (var command = new SQLiteCommand (sql, db_conn))
                command.ExecuteNonQuery();

            sql = @"CREATE TABLE novels (id INTEGER NOT NULL PRIMARY KEY, "+
                                       @"title TEXT, "+
                                       @"original TEXT, "+
                                       @"released TEXT, "+
                                       @"length INTEGER, "+
                                       @"description TEXT)";
            using (var command = new SQLiteCommand (sql, db_conn))
                command.ExecuteNonQuery();

            var line_re = new Regex (@"(\d+)\s+(\d+)\s+(\d+)");
            using (var reader = new StreamReader (args[0]))
            using (var transaction = db_conn.BeginTransaction())
            using (var command = new SQLiteCommand (db_conn))
            {
                Console.WriteLine ("Creating cache {0} ...", filename);
                var title_id = new SQLiteParameter();
                var user_id = new SQLiteParameter();
                var score = new SQLiteParameter();
                command.CommandText = "INSERT INTO votes (title_id, user_id, score) VALUES (?,?,?)";
                command.Parameters.Add (title_id);
                command.Parameters.Add (user_id);
                command.Parameters.Add (score);
                int count = 0;
                for (;;)
                {
                    var line = reader.ReadLine();
                    if (null == line)
                        break;
                    var match = line_re.Match (line);
                    if (!match.Success)
                        continue;
                    title_id.Value = int.Parse (match.Groups[1].Value);
                    user_id.Value = int.Parse (match.Groups[2].Value);
                    score.Value = int.Parse (match.Groups[3].Value);
                    command.ExecuteNonQuery();
                    ++count;
                }
                transaction.Commit();
                Console.WriteLine ("Total of {0} votes added", count);
            }
        }

        static void JsonTest ()
        {
            string response = "{\"num\":1, \"more\":false, \"items\":[{\"id\": 17, \"title\": \"Ever17 -the out of infinity-\", \"original\": null,\"released\": \"2002-08-29\", \"languages\": [\"en\",\"ja\",\"ru\",\"zh\"],\"platforms\": [\"drc\",\"ps2\",\"psp\",\"win\"],\"anime\": []}]}";
            var r = JObject.Parse (response);
            int num = (int)r.GetValue ("num");
            var more = r.GetValue ("more");
            var items = r.GetValue ("items");
            Console.WriteLine (num);
            foreach (var item in items)
            {
                Console.WriteLine ("Title: {0}", item.Value<string> ("title"));
                Console.WriteLine ("Released: {0}", item.Value<string> ("released"));
            }
            IEnumerable<int> array = new int[4] { 4, 3, 6, 1, };
//            string s = JsonConvert.ToString ("quoted \"string\" test");
            string s = JsonConvert.SerializeObject (array);
            Console.WriteLine (s);
        }

        static void Main (string[] args)
        {
            try
            {
//                JsonTest();
                CreateCache (args);
            }
            catch (Exception X)
            {
                Console.Error.WriteLine (X.Message);
            }
        }
    }
}
