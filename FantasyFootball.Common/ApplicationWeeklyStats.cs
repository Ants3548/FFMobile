using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyFootball.Common
{
	public class ApplicationWeeklyStats
	{
		public string Website { get; set; }
		public string LeagueId { get; set; }
		public Dictionary<string, SortedList<decimal, string>> PositionStats { get; set; }

		public int GetTeamRank(string position, string team)
		{
			SortedList<decimal, string> positionStats;
			if (PositionStats.TryGetValue(position, out positionStats))
			{
				return positionStats.IndexOfValue(team);
			}
			else
			{
				return -1;
			}
		}

		//public string GetTeamRankText(string position, string team)
		//{
		//	SortedList<decimal, string> positionStats;
		//	if (PositionStats.TryGetValue(position, out positionStats))
		//	{
		//		int rank = positionStats.IndexOfValue(team);
		//		if (int[](){ 0 })
  //          }
		//	else
		//	{
		//		return "N/A";
		//	}
		//}
	}
}