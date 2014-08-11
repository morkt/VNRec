//! \file       VNDBUser.cs
//! \date       Sun Aug 10 22:18:41 2014
//! \brief      VNDB user interface.
//

using System.Collections.Generic;

namespace VNDB
{
    public class User
    {
        public int                      Id { get; set; }
        public string                 Name { get; set; }
        public IDictionary<int, int> Votes { get; set; }

        public User (int id)
        {
            Id = id;
            Votes = new Dictionary<int, int>();
        }
    }
}
