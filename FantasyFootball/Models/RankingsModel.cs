using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyFootball.Models
{
    public class Ranking
    {
		public string Id { get; set; }
		public int Rank { get; set; }
        public string Name { get; set; }
        public string Team { get; set; }
        public string Position { get; set; }
        public int Bye { get; set; }
        public string Opponent { get; set; }
		public bool Active { get; set; }
		public bool IsHomeTeam { get; set; }
	}
}