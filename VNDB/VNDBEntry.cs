//! \file       VNDBEntry.cs
//! \date       Sun Aug 10 17:00:21 2014
//! \brief      VNDB entry class.
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

namespace VNDB
{
    public class Entry
    {
        /// <summary>
        /// Visual novel ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Main title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Original/official title.
        /// </summary>
        public string Original { get; set; }

        /// <summary>
        /// Date of the first release.
        /// </summary>
        public DateTime Released { get; set; }

        /// <summary>
        /// Can be an empty array when nothing has been released yet.
        /// </summary>
        public IEnumerable<string> Languages { get; set; }

        /// <summary>
        /// Language(s) of the first release. Can be an empty array.
        /// </summary>
        public IEnumerable<string> OrigLang { get; set; }

        /// <summary>
        /// Can be an empty array when unknown or nothing has been released yet.
        /// </summary>
        public IEnumerable<string> Platform { get; set; }

        /// <summary>
        /// Aliases, separated by newlines.
        /// </summary>
        public IEnumerable<string> Aliases { get; set; }

        /// <summary>
        /// Length of the game, 1-5
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Description of the VN. Can include formatting codes as described in d9.3.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Contains the following members:
        /// "wikipedia", string, name of the related article on the English Wikipedia.
        /// "encubed", string, the URL-encoded tag used on encubed.
        /// "renai", string, the name part of the url on renai.us.
        /// All members can be null when no links are available or known to us.
        /// </summary>
        public IDictionary<string, string> Links { get; set; }

        /// <summary>
        /// HTTP link to the VN image.
        /// </summary>
        public Uri Image { get; set; }

        /// <summary>
        /// Whether the VN image is flagged as NSFW or not.
        /// </summary>
        public bool ImageNSFW { get; set; }

        /// <summary>
        /// (Possibly empty) list of anime related to the VN, each object has the following members:
        /// "id", integer, AniDB ID
        /// "ann_id", integer, AnimeNewsNetwork ID
        /// "nfo_id", string, AnimeNfo ID
        /// "title_romaji", string
        /// "title_kanji", string
        /// "year", integer, year in which the anime was aired
        /// "type", string
        /// All members except the "id" can be null. Note that this data is courtesy of AniDB, and may not reflect the latest state of their information due to caching.
        /// </summary>
        public IEnumerable<AniDBEntry> Anime { get; set; }

        /// <summary>
        /// (Possibly empty) list of related visual novels, each object has the following members:
        /// "id", integer
        /// "relation", string, relation to the VN
        /// "title", string, (romaji) title
        /// "original", string, original/official title, can be null.
        /// </summary>
        public IEnumerable<VNRelation> Relations { get; set; }

        /// <summary>
        /// (Possibly empty) list of tags linked to this VN. Each tag is represented as an array with three elements:
        /// tag id (integer),
        /// score (number between 0 and 3),
        /// spoiler level (integer, 0=none, 1=minor, 2=major)
        /// Only tags with a positive score are included. Note that this list may be relatively large - more than 50 tags for a VN is quite possible.
        /// General information for each tag is available in the tags dump. Keep in mind that it is possible that a tag has only recently been added and is not available in the dump yet, though this doesn't happen often.
        /// </summary>
        public IEnumerable<VNTag> Tags { get; set; }

        /// <summary>
        /// Between 0 (unpopular) and 100 (most popular).
        /// </summary>
        public float Popularity { get; set; }

        /// <summary>
        /// Bayesian rating, between 1 and 10.
        /// </summary>
        public float Rating { get; set; }

        /// <summary>
        /// Number of votes.
        /// </summary>
        public int VoteCount { get; set; }
    }

    public class AniDBEntry
    {
        public int             Id { get; set; } // AniDB ID
        public int          AnnId { get; set; } // AnimeNewsNetwork ID
        public string       NfoId { get; set; } // AnimeNfo ID
        public string TitleRomaji { get; set; }
        public string  TitleKanji { get; set; }
        public short         Year { get; set; } // year in which the anime was aired
        public string        Type { get; set; }
    }

    public class VNRelation
    {
        public int          Id { get; set; }
        public string Relation { get; set; }
        public string    Title { get; set; }
        public string Original { get; set; }
    }

    public enum VNSpoiler
    {
        None, Minor, Major
    }

    public class VNTag
    {
        public int            Id { get; set; }
        public float       Score { get; set; }
        public VNSpoiler Spoiler { get; set; }
    }
}
