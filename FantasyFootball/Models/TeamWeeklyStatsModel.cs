using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyFootball.Models
{
	public class TeamWeeklyStats
	{
		public string Team { get; set; }
		public string Position { get; set; }
		public decimal Points { get; set; }
		public int Week { get; set; }
	}
}