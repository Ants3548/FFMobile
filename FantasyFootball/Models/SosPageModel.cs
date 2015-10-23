using FantasyFootball.Common;
using FantasyFootball.DAL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyFootball.Models
{
	public class SosPage
	{
		public List<tbl_ff_matchups> Matchups { get; set; }
		public ApplicationWeeklyStats Stats { get; set; }
		public List<string> Teams { 
			get 
			{ 
				return Stats.PositionStats.First().Value.ToList();
			} 
		}

		public int Week { get; set; }
		public int LastWeek
		{
			get
			{
				return Matchups.Select(s => s.Week).Distinct().Last();
			}
		}
	}
}