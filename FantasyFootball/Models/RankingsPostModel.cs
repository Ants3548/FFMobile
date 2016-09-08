using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyFootball.Models
{
    public class RankingsPost
    {
        public string Author { get; set; }
        public string TimeStamp { get; set; }
        public string Thumbnail { get; set; }
		public string Twitter { get; set; }
		public List<Ranking> Rankings { get; set; }
		public Dictionary<string, List<Ranking>> MultiPartRankings { get; set; }
	}
}