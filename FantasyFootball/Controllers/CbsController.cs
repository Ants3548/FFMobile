using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

using FantasyFootball.Models;
using FantasyFootball.Common;
using FantasyFootball.DAL;
using System.Collections.Specialized;

namespace FantasyFootball.Controllers
{
    public class CbsController : Controller
    {
        // GET: Cbs
        public ActionResult Index()
        {
            return View();
        }

		public static Dictionary<string, List<Ranking>> GetRankingsWeeklyPPR(List<string> playerIds)
		{
			List<string> myPositions = new List<string>() { "QB", "RB", "WR", "TE", "DST" };			
			Dictionary<string, List<Ranking>> rankings = new Dictionary<string, List<Ranking>>();

			foreach(string myPosition in myPositions)
			{
				rankings.Add(myPosition, new List<Ranking>());
				string html = Functions.GetHttpHtml(string.Format("http://www.cbssports.com/fantasy/football/rankings/ppr/{0}/weekly/", myPosition), null);
				Match htmlMatch = Regex.Match(html, @"(?i)rankings-tbl-data-inner.*?</table>", RegexOptions.Singleline);

				if (htmlMatch.Success)
				{
					MatchCollection myTRs = Regex.Matches(htmlMatch.Value, @"(?i)ranking-tbl-data-inner-tr[^>]*>(?<Content>.*?)</tr>", RegexOptions.Singleline);
					if (myTRs.Count > 0)
					{
						foreach (Match myTR in myTRs)
						{
							Match playerMatch = Regex.Match(myTR.Groups["Content"].Value, @"/fantasy/football/players/(?<PlayerId>\d+)/[^>]+>(?<PlayerName>[^<]+)</a>.*?rank-team-span[^>]+>(?<PlayerTeam>[^<]+)</span>.*?<span>(?<Opponent>[^<]+)</span>", RegexOptions.Singleline);
							if (playerMatch.Success)
							{
								rankings[myPosition].Add(new Ranking()
								{
									Id = playerMatch.Groups["PlayerId"].Value,
									Name = playerMatch.Groups["PlayerName"].Value,
									Opponent = playerMatch.Groups["Opponent"].Value.Replace("@", string.Empty),
									Team = playerMatch.Groups["PlayerTeam"].Value,
									Rank = rankings[myPosition].Count + 1,
									Position = myPosition,
									Active = ((playerIds.Contains(playerMatch.Groups["PlayerId"].Value)) ? false : true)
								});
							}
						}
					}
				}
			}			

			return rankings;
		}
	}
}