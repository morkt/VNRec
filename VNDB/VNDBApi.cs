//! \file       VNDBApi.cs
//! \date       Sun Aug 10 17:28:28 2014
//! \brief      VNDB API implementation.
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
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VNDB
{
    public class Connection : IDisposable
    {
        TcpClient m_client;

        public const int    Protocol    = 1;
        public const string Client      = "RndCSharpVndbApi";
        public const float  Version     = 1.0f;

        public const string DefaultHost = "api.vndb.org";
        public const int    DefaultPort = 19534;

        public Connection (string host = DefaultHost, int port = DefaultPort)
        {
            m_client = new TcpClient (host, port);
        }

        public NetworkStream GetStream ()
        {
            return m_client.GetStream();
        }

        public static string GetLoginString ()
        {
            return string.Format ("login {{\"protocol\":{0},\"client\":{1},\"clientver\":{2}}}",
                                  JsonConvert.ToString (Protocol), 
                                  JsonConvert.ToString (Client), JsonConvert.ToString (Version));
        }

        public static string GetLoginString (string username, string password)
        {
            return string.Format ("login {{\"protocol\":{0},\"client\":{1},\"clientver\":{2},\"username\":{3},\"password\":{4}}}",
                                  JsonConvert.ToString (Protocol), 
                                  JsonConvert.ToString (Client), JsonConvert.ToString (Version),
                                  JsonConvert.ToString (username), JsonConvert.ToString (password));
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
                    m_client.Close();
                }
                disposed = true;
            }
        }
        #endregion
    }

    public class Session : IDisposable
    {
        NetworkStream   m_stream;

        public Session (Connection conn)
        {
            m_stream = conn.GetStream();
            Login (Connection.GetLoginString());
        }

        public Session (Connection conn, string username, string password)
        {
            m_stream = conn.GetStream();
            Login (Connection.GetLoginString (username, password));
        }

        private void Login (string message)
        {
            string response = Send (message);
            if ("ok" == response)
                return;
            var error = ParseResponse (response);
            if (null == error)
                throw new VndbException (null, "Unknown server response");
            if ("error" != error.Item1)
                throw new VndbException (error.Item1, "Unknown server response");

            var dict = JObject.Parse (error.Item2);
            string id = null;
            var j_id = dict.GetValue ("id");
            if (null != j_id)
                id = j_id.ToString();
            var j_msg = dict.GetValue ("msg");
            string msg = j_msg != null ? j_msg.ToString() : "Unknown server error";
            throw new VndbException (id , msg);
        }

        static readonly Regex response_re = new Regex (@"^([a-z]+)\s*");

        public Tuple<string, string> ParseResponse (string response)
        {
            var match = response_re.Match (response);
            if (!match.Success)
                return null;
            return new Tuple<string, string> (match.Groups[1].Value, response.Substring (match.Length));
        }

        public string Send (string message)
        {
            int byte_count = Encoding.UTF8.GetByteCount (message);
            byte[] bytes = new byte[byte_count+1];
            Encoding.UTF8.GetBytes (message, 0, message.Length, bytes, 0);
            bytes[byte_count] = 0x04;

            m_stream.Write (bytes, 0, bytes.Length);
            using (var response_buf = new MemoryStream (1024))
            {
                for (;;)
                {
                    int sym = m_stream.ReadByte();
                    if (-1 == sym || 4 == sym)
                        break;
                    response_buf.WriteByte ((byte)sym);
                }
                byte[] buffer = response_buf.GetBuffer();
                int length = (int)response_buf.Length;
                return Encoding.UTF8.GetString (buffer, 0, length);
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
                    m_stream.Close();
                }
                disposed = true;
            }
        }
        #endregion
    }

    public class VndbException : Exception
    {
        public string Id { get; private set; }

        public VndbException (string id, string msg) : base (msg)
        {
            Id = id;
        }
    }
}
