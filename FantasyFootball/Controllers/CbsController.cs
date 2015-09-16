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
			string html = Functions.GetHttpHtml("http://fantasynews.cbssports.com/fantasyfootball/rankings/weekly/ppr", null);
			Match htmlMatch = Regex.Match(html, @"(?i)<table\s+class=""data""\s+width=""100%"">.*?</table>", RegexOptions.Singleline);
			Dictionary<string, List<Ranking>> rankings = new Dictionary<string, List<Ranking>>();			

			if (htmlMatch.Success)
			{
				MatchCollection myTRs = Regex.Matches(htmlMatch.Value, @"(?i)<tr[^>]*>(?<Content>.*?)</tr>", RegexOptions.Singleline);
				if (myTRs.Count > 0)
				{
					string keyName = string.Empty;
					foreach (Match myTR in myTRs)
					{
						MatchCollection myTDs = Regex.Matches(myTR.Groups["Content"].Value, @"(?i)<td[^>]*>(?<Content>.*?)</td>", RegexOptions.Singleline);
						if (myTDs.Count > 0)
						{
							switch (myTDs.Count)
							{
								case 1:
									keyName = myTDs[0].Groups["Content"].Value;
									rankings.Add(keyName, new List<Ranking>());
									break;
								case 2:
									Match playerMatch = Regex.Match(myTDs[1].Groups["Content"].Value, @"(?i)^<a[^>]+?playerpage/(?<PlayerId>\d+)[^>]+>(?<PlayerName>[^<]+)</a>\s*(?<PlayerTeam>[^&\s]+).*?\((@|vs\.\s+)(?<Opponent>\w+)\)", RegexOptions.Singleline);
									if (playerMatch.Success)
									{
										rankings[keyName].Add(new Ranking()
										{
											Name = playerMatch.Groups["PlayerName"].Value,
											Opponent = playerMatch.Groups["Opponent"].Value,
											Team = playerMatch.Groups["PlayerTeam"].Value,
											Rank = rankings[keyName].Count + 1,
											Active = ((playerIds.Contains(playerMatch.Groups["PlayerId"].Value)) ? false : true)
										});
									}
									break;
							}
						}
					}
				}
			}

			return rankings;
		}
	}
}