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
    public class YahooController : Controller
    {
		public ActionResult Index()
		{
			List<League> myLeagues = new List<League>();

			if (Session["yahoo"] != null)
				myLeagues = GetLeagues();
			else
				return RedirectToAction("Yahoo", "Login");

			return View(myLeagues);
		}
		
		public ActionResult Team()
		{
			List<Player> myPlayers = new List<Player>();

			if (Session["yahoo"] != null)
				myPlayers = GetPlayers(Request.Params["leagueId"], Request.Params["teamId"]);
			else
				return RedirectToAction("Yahoo", "Login");

			ViewBag.Title = "Some Team";
			ViewBag.LeagueId = Request.Params["leagueId"];
			ViewBag.TeamId = Request.Params["teamId"];
            return View(myPlayers);
		}

		public ActionResult Teams()
		{
			List<Owner> myTeams = new List<Owner>();

			if (Session["yahoo"] != null)
				myTeams = GetTeams(Request.Params["leagueId"]);
			else
				return RedirectToAction("Yahoo", "Login");

			ViewBag.Title = "League Owners";
			ViewBag.LeagueId = Request.Params["leagueId"];
			return View(myTeams);
		}

		public ActionResult Transactions()
		{
			List<Transaction> myTransactions = new List<Transaction>();
			//transaction-table
			if (Session["yahoo"] != null)
				myTransactions = GetTransactions(Request.Params["leagueId"]);
			else
				return RedirectToAction("Yahoo", "Login");

			ViewBag.Title = "Recent Transactions";
			ViewBag.LeagueId = Request.Params["leagueId"];
			ViewBag.TeamId = Request.Params["teamId"];
			return View(myTransactions);
		}

		public ActionResult WaiverWire()
		{
			List<Player> myPlayers = new List<Player>();

			if (Session["yahoo"] != null)
				myPlayers = GetWaivers(Request.Params["leagueId"]);
			else
				return RedirectToAction("Yahoo", "Login");

			ViewBag.Title = "Waiver Wire";
			ViewBag.LeagueId = Request.Params["leagueId"];
			ViewBag.TeamId = Request.Params["teamId"];
			return View(myPlayers);
		}

		public ActionResult WeeklyRankingsPPR()
		{
			List<string> playerYahooIds = new List<string>(), playerCbsIds = new List<string>();
			if (Session["yahoo"] != null)
			{
				string html = Functions.GetHttpHtml(string.Format("http://football.fantasysports.yahoo.com/f1/{0}/starters", Request.Params["leagueId"]), (string)Session["yahoo"]);
				MatchCollection players = Regex.Matches(html, @"(?i)/nfl/players/(?<PlayerId>\d+)""", RegexOptions.Singleline);
				if (players.Count > 0)
				{
					foreach (Match player in players)
					{
						playerYahooIds.Add(player.Groups["PlayerId"].Value);
					}

					using (FantasyFootballEntities db = new FantasyFootballEntities())
					{
						playerCbsIds = db.tbl_ff_players.Where(x => playerYahooIds.Contains(x.Yahoo) && x.Cbs != null).Select(s => s.Cbs).ToList();
					}
				}
			}

			Dictionary<string, List<FantasyFootball.Models.Ranking>> rankings = CbsController.GetRankingsWeeklyPPR(playerCbsIds);					

			ViewBag.Title = "Weekly PPR Rankings";
			ViewBag.LeagueId = Request.Params["leagueId"];
			return View(rankings);
		}

		protected List<League> GetLeagues()
		{
			string html = Functions.GetHttpHtml("https://football.fantasysports.yahoo.com/", (string)Session["yahoo"]);

			List<League> myLeagues = new List<League>();
			MatchCollection myTeams = Regex.Matches(html, @"(?i)Pstart-30\sPy-med.*?</div>", RegexOptions.Singleline);

			foreach (Match myTeam in myTeams)
			{
				MatchCollection links = Regex.Matches(myTeam.Value, @"(?i)<a\s+[^>]+>.*?</a>", RegexOptions.Singleline);
				if (links.Count == 2)
				{
					Match teamLink = Regex.Match(links[0].Value, @"(?i)<a.*?href=""http://football.fantasysports.yahoo.com/f1/(?<LeagueId>\d+)/(?<TeamId>\d+)""[^>]*>(?<TeamName>[^<]+)</a>", RegexOptions.Singleline);
					Match leagueLink = Regex.Match(links[1].Value, @"(?i)<a\s+[^>]+>(?<LeagueName>[^<]+)</a>", RegexOptions.Singleline);

					if (teamLink.Success && leagueLink.Success)
					{
						myLeagues.Add(new League()
						{
							LeagueId = Convert.ToInt32(teamLink.Groups["LeagueId"].Value),
							TeamName = teamLink.Groups["TeamName"].Value,
							LeagueName = leagueLink.Groups["LeagueName"].Value,
							Season = DateTime.Now.Year,
							TeamId = Convert.ToInt32(teamLink.Groups["TeamId"].Value)
						});
					}
				}
			}

			return myLeagues;
		}

		protected List<Player> GetPlayers(string leagueId, string teamId)
		{
			string html = Functions.GetHttpHtml(string.Format("http://football.fantasysports.yahoo.com/f1/{0}/{1}", leagueId, teamId), (string)Session["yahoo"]);
			Match htmlMatch = Regex.Match(html, @"(?i)<section[^>]+?id=""team-roster""[^>]*>.*?</section>", RegexOptions.Singleline);
			List<Player> myPlayers = new List<Player>();
			if (htmlMatch.Success)
			{
				MatchCollection myTRs = Regex.Matches(htmlMatch.Value, @"(?i)<tr[^>]*>.*?</tr>", RegexOptions.Singleline);
				if(myTRs.Count > 0)
				{
					bool isEditPage = htmlMatch.Value.Contains(@"roster-edit-form");
					foreach (Match myTR in myTRs)
					{
						MatchCollection myTDs = Regex.Matches(myTR.Value, @"(?i)<td[^>]*>(?<Content>.*?)</td>", RegexOptions.Singleline);
						if(myTDs.Count > 0)
						{
							Match playerMatch = Regex.Match(myTDs[(isEditPage ? 2 : 1)].Groups["Content"].Value, @"(?i)ysf-player-name.*?http://sports.yahoo.com/nfl/players/(?<PlayerId>\d+)[^>]+>(?<PlayerName>[^<]+)</a>.*?(?<Team>\w{2,3})\s+\-\s+(?<Position>\w{2,3})</span>", RegexOptions.Singleline);
							Match opponentMatch = Regex.Match(myTDs[(isEditPage ? 2 : 1)].Groups["Content"].Value, @"(?i)ysf-game-status.*?<a\s+[^>]+>(?<Opponent>[^<]+)</a>", RegexOptions.Singleline);
							if(playerMatch.Success && opponentMatch.Success)
							{
								myPlayers.Add(new Player()
								{
									PlayerId = playerMatch.Groups["PlayerId"].Value,
									Name = playerMatch.Groups["PlayerName"].Value,
									Position = playerMatch.Groups["Position"].Value,
									Team = playerMatch.Groups["Team"].Value.ToLower(),
									Opponent = Regex.Match(opponentMatch.Groups["Opponent"].Value, @"\w+$", RegexOptions.Singleline).Value.ToLower(),
									Note01 = opponentMatch.Groups["Opponent"].Value
								});
                            }
						}
					}
				}
			}

			//Get the CBS images for the players
			using(FantasyFootballEntities db = new FantasyFootballEntities())
			{
				List<string> yahooIdList = (from p in myPlayers select p.PlayerId).ToList();
				List<tbl_ff_players> playerEntityList = db.tbl_ff_players.Where(x => yahooIdList.Contains(x.Yahoo) && x.Cbs != null).ToList();
				foreach (tbl_ff_players entityPlayer in playerEntityList)
				{
					Player yahooPlayer = (from p in myPlayers where p.PlayerId == entityPlayer.Yahoo select p).FirstOrDefault();
					yahooPlayer.PlayerAltId = entityPlayer.Cbs;
				}
			}			

			return myPlayers;
		}

		protected List<Player> GetWaivers(string leagueId)
		{
			string html = Functions.GetHttpHtml(string.Format("http://football.fantasysports.yahoo.com/f1/{0}/players?&sort=R_PO&sdir=1&status=A", leagueId), (string)Session["yahoo"]);
			Match htmlMatch = Regex.Match(html, @"(?i)<section[^>]+?id=""players-table-wrapper""[^>]*>.*?</section>", RegexOptions.Singleline);
			List<Player> myPlayers = new List<Player>();
			if (htmlMatch.Success)
			{
				MatchCollection myTRs = Regex.Matches(htmlMatch.Value, @"(?i)<tr[^>]*>.*?</tr>", RegexOptions.Singleline);
				if (myTRs.Count > 0)
				{
					foreach (Match myTR in myTRs)
					{
						MatchCollection myTDs = Regex.Matches(myTR.Value, @"(?i)<td[^>]*>(?<Content>.*?)</td>", RegexOptions.Singleline);
						if (myTDs.Count > 0)
						{
							Match playerMatch = Regex.Match(myTDs[1].Groups["Content"].Value, @"(?i)ysf-player-name.*?http://sports.yahoo.com/nfl/players/(?<PlayerId>\d+)[^>]+>(?<PlayerName>[^<]+)</a>.*?(?<Team>\w{2,3})\s+\-\s+(?<Position>\w{2,3})</span>", RegexOptions.Singleline);
							Match opponentMatch = Regex.Match(myTDs[1].Groups["Content"].Value, @"(?i)ysf-game-status.*?<a\s+[^>]+>(?<Opponent>[^<]+)</a>", RegexOptions.Singleline);
							Match injuryMatch = Regex.Match(myTDs[1].Groups["Content"].Value, @"(?i)<abbr[^>]*>(?<Content>[^<]+)</abbr>", RegexOptions.Singleline);
                            if (playerMatch.Success && opponentMatch.Success)
							{
								myPlayers.Add(new Player()
								{
									PlayerId = playerMatch.Groups["PlayerId"].Value,
									Name = playerMatch.Groups["PlayerName"].Value 
									+ string.Format(" {0}", Regex.Replace(myTDs[6].Groups["Content"].Value, @"(?i)<[^>]+>", string.Empty))
									+ (injuryMatch.Success ? string.Format(" - <span class=\"txRed\">{0}</span>", injuryMatch.Groups["Content"].Value) : string.Empty),
									Position = playerMatch.Groups["Position"].Value,
									Team = playerMatch.Groups["Team"].Value.ToLower(),
									Opponent = Regex.Match(opponentMatch.Groups["Opponent"].Value, @"\w+$", RegexOptions.Singleline).Value.ToLower(),
									Note01 = Regex.Replace(opponentMatch.Groups["Opponent"].Value, @"\s+(@|vs)\s+\w+$", string.Empty) + " - " + Regex.Replace(myTDs[3].Groups["Content"].Value, @"(?i)<[^>]+>", string.Empty)
								});
							}
						}
					}
				}
			}

			//Get the CBS images for the players
			using (FantasyFootballEntities db = new FantasyFootballEntities())
			{
				List<string> yahooIdList = (from p in myPlayers select p.PlayerId).ToList();
				List<tbl_ff_players> playerEntityList = db.tbl_ff_players.Where(x => yahooIdList.Contains(x.Yahoo) && x.Cbs != null).ToList();
				foreach (tbl_ff_players entityPlayer in playerEntityList)
				{
					Player yahooPlayer = (from p in myPlayers where p.PlayerId == entityPlayer.Yahoo select p).FirstOrDefault();
					yahooPlayer.PlayerAltId = entityPlayer.Cbs;
				}
			}

			return myPlayers;
		}

		protected List<Owner> GetTeams(string leagueId)
		{
			string html = Functions.GetHttpHtml(string.Format("http://football.fantasysports.yahoo.com/f1/{0}/", leagueId), (string)Session["yahoo"]);
			Match htmlMatch = Regex.Match(html, @"(?i)standingstable.*?</table>", RegexOptions.Singleline);
			List<Owner> myOwners = new List<Owner>();
            if (htmlMatch.Success)
			{
				MatchCollection myHrefs = Regex.Matches(htmlMatch.Value, @"(?i)href=""/f1/\d+/(?<OwnerId>\d+)"">(?<TeamName>.*?)</a>", RegexOptions.Singleline);
				if (myHrefs.Count > 0)
				{
					foreach (Match myHref in myHrefs)
					{
						myOwners.Add(new Owner() {
							Id = myHref.Groups["OwnerId"].Value,
							TeamName = myHref.Groups["TeamName"].Value
						});
                    }
				}
			}

			return myOwners;
		}

		protected List<Transaction> GetTransactions(string leagueId)
		{
			string html = Functions.GetHttpHtml(string.Format("http://football.fantasysports.yahoo.com/f1/{0}/transactions", leagueId), (string)Session["yahoo"]);
			Match htmlMatch = Regex.Match(html, @"(?i)transaction-table.*?</table>", RegexOptions.Singleline);
			List<Transaction> myTransactions = new List<Transaction>();
			if (htmlMatch.Success)
			{
				MatchCollection myTRs = Regex.Matches(htmlMatch.Value, @"(?i)<tr[^>]*>(?<Content>.*?)</tr>", RegexOptions.Singleline);
				if(myTRs.Count > 0)
				{
					foreach(Match myTR in myTRs)
					{
						MatchCollection myTDs = Regex.Matches(myTR.Groups["Content"].Value, @"(?i)<td[^>]*>(?<Content>.*?)</td>", RegexOptions.Singleline);
						if (myTDs.Count == 3)
						{
							//Get Owner info
							Match ownerMatch = Regex.Match(myTDs[2].Groups["Content"].Value, @"/f1/(?<LeagueId>\d+)/(?<OwnerId>\d+)[^>]+>(?<OwnerName>[^<]+)</a>.*?<span[^>]*>(?<TimeStamp>[^<]+)</span>", RegexOptions.Singleline);
							MatchCollection playerMatch = Regex.Matches(myTDs[1].Groups["Content"].Value, @"(?i)http://sports.yahoo.com/nfl/players/(?<PlayerId>\d+)[^>]+>(?<PlayerName>[^<]+)</a>.*?<span[^>]*>(?<Team>\w{2,3})\s+\-\s+(?<Position>\w{2,3})</span>", RegexOptions.Singleline);
							MatchCollection changesMatch = Regex.Matches(myTDs[0].Groups["Content"].Value, @"(?i)title=""(?<ChangeType>[^""]+)""", RegexOptions.Singleline);
							if (ownerMatch.Success && playerMatch.Count > 0 && changesMatch.Count > 0)
							{
								List<Player> playerList = new List<Player>();
								for (int i = 0; i < playerMatch.Count; i++)
								{
									Match pMatch = playerMatch[i];
									playerList.Add(new Player()
									{
										PlayerId = pMatch.Groups["PlayerId"].Value,
										Name = pMatch.Groups["PlayerName"].Value,
										Position = pMatch.Groups["Position"].Value,
										Team = pMatch.Groups["Team"].Value.ToLower(),
										Added = changesMatch[i].Groups["ChangeType"].Value.Contains("Added")
									});
                                }

								myTransactions.Add(new Transaction()
								{
									OwnerName = ownerMatch.Groups["OwnerName"].Value,
									Date = Regex.Replace(ownerMatch.Groups["TimeStamp"].Value, @"(?i)\s+(\w+)$", "$1", RegexOptions.Singleline),
									Players = playerList
								});
							}
						}
					}
				}				
			}

			return myTransactions;
		}
	}
}