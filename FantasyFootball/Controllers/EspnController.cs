using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

using FantasyFootball.Models;
using FantasyFootball.DAL;
using FantasyFootball.Common;

namespace FantasyFootball.Controllers
{
	public class EspnController : Controller
	{
		//
		// GET: /Espn/

		public ActionResult Index()
		{
			List<Models.League> myLeagues = new List<League>();

			if (Session["espn"] != null)
				myLeagues = GetLeagues();
			else
				return RedirectToAction("Espn", "Login");

			return View(myLeagues);
		}

		public ActionResult SoS()
		{
			ApplicationWeeklyStats myStats = new ApplicationWeeklyStats();
			List<tbl_ff_matchups> matchups = new List<tbl_ff_matchups>();
			int week = 1;

			if (Session["espn"] != null)
			{
				//Do a stat scrape
				myStats = Common.Functions.GetWeeklyStats("ESPN", Request.Params["leagueId"]);
				if (myStats == null)
				{
					SetTeamWeeklyStats(Request.Params["leagueId"], 3);
					myStats = Common.Functions.GetWeeklyStats("ESPN", Request.Params["leagueId"]);
				}
			}
			else
				return RedirectToAction("ESPN", "Login");

			GetSchedules(ref week, ref matchups);

			ViewBag.Title = "Strength of Schedule";
			ViewBag.LeagueId = Request.Params["leagueId"];
			ViewBag.SeasonId = Request.Params["seasonId"];
			ViewBag.Matchups = matchups;
			ViewBag.Week = week;
			return View(myStats);
		}

		public ActionResult Team()
		{
			List<Player> myPlayers = new List<Player>();
			ApplicationWeeklyStats myStats = new ApplicationWeeklyStats();

			if (Session["espn"] != null)
			{
				myPlayers = GetPlayers(Request.Params["leagueId"], Request.Params["teamId"], Request.Params["seasonId"]);
				myStats = Common.Functions.GetWeeklyStats("ESPN", Request.Params["leagueId"]);
				if (myStats == null)
				{
					SetTeamWeeklyStats(Request.Params["leagueId"], 3);
					myStats = Common.Functions.GetWeeklyStats("ESPN", Request.Params["leagueId"]);
				}
			}				
			else
				return RedirectToAction("Espn", "Login");

			ViewBag.Title = "Some Team";
			ViewBag.LeagueId = Request.Params["leagueId"];
			ViewBag.TeamId = Request.Params["teamId"];
			ViewBag.SeasonId = Request.Params["seasonId"];
			ViewBag.WeeklyStats = myStats;
            return View(myPlayers);
		}

		public ActionResult Leagues()
		{
			Functions.CheckForSession();

			List<Models.League> myLeagues = new List<League>();

			if (Session["espn"] != null)
				myLeagues = GetLeagues();
			else
				return RedirectToAction("Espn", "Login");

			return View(myLeagues);
		}

		public ActionResult WaiverWire()
		{
			List<Player> myPlayers = new List<Player>();
			ApplicationWeeklyStats myStats = new ApplicationWeeklyStats();

			if (Session["espn"] != null)
			{
				myPlayers = GetWaivers(Request.Params["leagueId"]);
				myStats = Common.Functions.GetWeeklyStats("ESPN", Request.Params["leagueId"]);
				if (myStats == null)
				{
					SetTeamWeeklyStats(Request.Params["leagueId"], 3);
					myStats = Common.Functions.GetWeeklyStats("ESPN", Request.Params["leagueId"]);
				}
			}
			else
				return RedirectToAction("ESPN", "Login");

			ViewBag.Title = "Waiver Wire";
			ViewBag.LeagueId = Request.Params["leagueId"];
			ViewBag.TeamId = Request.Params["teamId"];
			ViewBag.SeasonId = Request.Params["seasonId"];
			ViewBag.WeeklyStats = myStats;
			return View(myPlayers);
		}

		public ActionResult WeeklyRankingsPPR()
		{
			List<string> playerEspnIds = new List<string>(), playerCbsIds = new List<string>();
			if (Session["espn"] != null)
			{
				string html = Functions.GetHttpHtml(string.Format("http://games.espn.go.com/ffl/leaguerosters?leagueId={0}", Request.Params["leagueId"]), (string)Session["espn"]);
				MatchCollection players = Regex.Matches(html, @"(?i)playerid=""(?<PlayerId>\d+)""", RegexOptions.Singleline);
				if (players.Count > 0)
				{
					foreach (Match player in players)
					{
						playerEspnIds.Add(player.Groups["PlayerId"].Value);
					}

					using (FantasyFootballEntities db = new FantasyFootballEntities())
					{
						playerCbsIds = db.tbl_ff_players.Where(x => playerEspnIds.Contains(x.Espn) && x.Cbs != null).Select(s => s.Cbs).ToList();
					}
				}
			}

			List<RankingsPost> myRankings = CbsController.GetRankingsWeeklyPPR(playerCbsIds);

			ViewBag.Title = "Weekly PPR Rankings";
			ViewBag.LeagueId = Request.Params["leagueId"];
			return View(myRankings);
		}

		public List<League> GetLeagues()
		{
			string html = Functions.GetHttpHtml("http://games.espn.go.com/frontpage/football", (string)Session["espn"]);
			Match targetHtml = Regex.Match(html, @"(?i)my-teams\s+lm-container.*?editlist", RegexOptions.Singleline);
			List<Models.League> myLeagues = new List<League>();
			if (targetHtml.Success)
			{
				MatchCollection myTeams = Regex.Matches(targetHtml.Value, @"class=""left"".*?clear:\s*both", RegexOptions.Singleline);

				foreach (Match myTeam in myTeams)
				{
					Match teamLink = Regex.Match(myTeam.Value, @"(?i)<a.*?http://games.espn.go.com/ffl/clubhouse\?leagueId=(?<LeagueId>\d+)&teamId=(?<TeamId>\d+)&seasonId=(?<SeasonId>\d+)[^>]+>(?<TeamName>[^<]+)</a>", RegexOptions.Singleline);
					Match leagueLink = Regex.Match(myTeam.Value, @"(?i)<a.*?http://games.espn.go.com/ffl/leagueoffice[^""]+""[^>]+>(?<LeagueName>[^<]+)</a>", RegexOptions.Singleline);

					if (teamLink.Success && leagueLink.Success)
					{
						myLeagues.Add(new League()
						{
							LeagueId = Convert.ToInt32(teamLink.Groups["LeagueId"].Value),
							TeamName = teamLink.Groups["TeamName"].Value,
							LeagueName = leagueLink.Groups["LeagueName"].Value,
							Season = Convert.ToInt32(teamLink.Groups["SeasonId"].Value),
							TeamId = Convert.ToInt32(teamLink.Groups["TeamId"].Value)
						});
					}
				}
			}

			return myLeagues;
		}

		protected List<Player> GetPlayers(string leagueId, string teamId, string seasonId)
		{
			string html = Functions.GetHttpHtml(string.Format("http://games.espn.go.com/ffl/clubhouse?leagueId={0}&teamId={1}&seasonId={2}", leagueId, teamId, seasonId), (string)Session["espn"]);
			Match htmlMatch = Regex.Match(html, @"(?i)<table[^>]+?class=""playertableFrameBorder""[^>]*>.*?</table>", RegexOptions.Singleline);
			List<Player> myPlayers = new List<Player>();
			if (htmlMatch.Success)
			{
				MatchCollection myTRs = Regex.Matches(htmlMatch.Value, @"(?i)<tr[^>]*>.*?</tr>", RegexOptions.Singleline);
				if (myTRs.Count > 0)
				{
					bool isEditPage = htmlMatch.Value.Contains(@"pncButton");
					foreach (Match myTR in myTRs)
					{
						if (myTR.Value.Contains("pncPlayerRow"))
						{
							MatchCollection myTDs = Regex.Matches(myTR.Value, @"(?i)<td[^>]*>(?<Content>.*?)</td>", RegexOptions.Singleline);
							if (myTDs.Count > 0)
							{
								Match playerMatch = Regex.Match(myTDs[1].Groups["Content"].Value, @"(?i)playerid=""(?<PlayerId>\d+)""[^>]*>(?<PlayerName>[^<]+)</a>\W+(?<Team>\w{2,3})&nbsp;(?<Position>\w{1,4})", RegexOptions.Singleline);
								Match opponentMatch = Regex.Match(myTDs[(isEditPage ? 4 : 3)].Groups["Content"].Value, @"(?i)<a[^>]+>(?<Opponent>[^<]+)</a>", RegexOptions.Singleline);
								Match opponentRankMatch = Regex.Match(myTDs[(isEditPage ? 13 : 12)].Groups["Content"].Value, @"(?i)<a[^>]+?>(?<Content>.*?)</a>", RegexOptions.Singleline);
								Match noteMatch = Regex.Match(myTDs[(isEditPage ? 5 : 4)].Groups["Content"].Value, @"(?i)<a[^>]+>(?<Content>[^<]+)</a>", RegexOptions.Singleline);
								if (playerMatch.Success && opponentMatch.Success)
								{
									myPlayers.Add(new Player()
									{
										PlayerId = playerMatch.Groups["PlayerId"].Value,
										Name = playerMatch.Groups["PlayerName"].Value,
										Position = playerMatch.Groups["Position"].Value,
										Team = playerMatch.Groups["Team"].Value.ToLower(),
										Opponent = Regex.Replace(opponentMatch.Groups["Opponent"].Value, @"@", string.Empty, RegexOptions.Singleline).ToLower(),
										OpponentRank = (opponentRankMatch.Success ? opponentRankMatch.Groups["Content"].Value : "&nbsp;"),
										Note01 = (noteMatch.Success ? noteMatch.Groups["Content"].Value : string.Empty)
									});
								}
							}
						}						
					}
				}
			}

			//Get the CBS images for the players
			using (FantasyFootballEntities db = new FantasyFootballEntities())
			{
				List<string> espnIdList = (from p in myPlayers select p.PlayerId).ToList();
				List<tbl_ff_players> playerEntityList = db.tbl_ff_players.Where(x => espnIdList.Contains(x.Espn) && x.Cbs != null).ToList();
				foreach (tbl_ff_players entityPlayer in playerEntityList)
				{
					Player espnPlayer = (from p in myPlayers where p.PlayerId == entityPlayer.Espn select p).FirstOrDefault();
					espnPlayer.PlayerAltId = entityPlayer.Cbs;
				}
			}

			return myPlayers;
		}

		protected List<Player> GetWaivers(string leagueId)
		{
			List<Player> myPlayers = new List<Player>();
			string html = Functions.GetHttpHtml(string.Format("http://games.espn.go.com/ffl/playertable/prebuilt/freeagency?leagueId={0}&=undefined&context=freeagency&view=overview&sortMap=AAAAARgAAAADAQAIY2F0ZWdvcnkDAAAAAwEABmNvbHVtbgMAAAAIAQAJZGlyZWN0aW9uA%2F%2F%2F%2F%2F8%3D&r=41671407&r=77879267", leagueId), (string)Session["espn"]);
			MatchCollection myTRs = Regex.Matches(html, @"(?i)<tr[^>]+pncPlayerRow[^>]+>(?<Content>.*?)</tr>", RegexOptions.Singleline);
			if (myTRs.Count > 0)
			{
				foreach (Match myTR in myTRs)
				{
					MatchCollection myTDs = Regex.Matches(myTR.Groups["Content"].Value, @"(?i)<td[^>]*>(?<Content>.*?)</td>", RegexOptions.Singleline);
					if (myTDs.Count > 0)
					{
						Match playerMatch = Regex.Match(myTDs[0].Groups["Content"].Value, @"(?i)playerid=""(?<PlayerId>\d+)""[^>]*>(?<PlayerName>[^<]+)</a>\W+(?<Team>\w{2,3})&nbsp;(?<Position>\w{1,4})", RegexOptions.Singleline);
						Match injuryMatch = Regex.Match(myTDs[0].Groups["Content"].Value, @"(?i)<span[^>]*>(?<Content>[^<]+)</span>", RegexOptions.Singleline);
						Match opponentMatch = Regex.Match(myTDs[5].Groups["Content"].Value, @"(?i)<a\s+[^>]+>(?<Opponent>[^<]+)</a>", RegexOptions.Singleline);
						
						if (playerMatch.Success && opponentMatch.Success)
						{
							myPlayers.Add(new Player()
							{
								PlayerId = playerMatch.Groups["PlayerId"].Value,
								Name = playerMatch.Groups["PlayerName"].Value
								+ string.Format(" {0}%", Regex.Replace(Common.Functions.StripHtmlTags(myTDs[myTDs.Count - 2].Groups["Content"].Value), @"\.\d+", string.Empty))
								+ (injuryMatch.Success ? string.Format(" - <span class=\"txRed\">{0}</span>", injuryMatch.Groups["Content"].Value) : string.Empty),
								Position = playerMatch.Groups["Position"].Value,
								Team = playerMatch.Groups["Team"].Value.ToLower(),
								Opponent = Regex.Replace(opponentMatch.Groups["Opponent"].Value, @"(@|vs)", string.Empty).ToLower(),
								Note01 = Common.Functions.StripHtmlTags(myTDs[2].Groups["Content"].Value) + " - " + Common.Functions.StripHtmlTags(myTDs[6].Groups["Content"].Value) + " " + (opponentMatch.Groups["Opponent"].Value.Contains("@") ? "@" : "vs")
							});
						}
					}
				}
			}
			
			//Get the CBS images for the players
			using (FantasyFootballEntities db = new FantasyFootballEntities())
			{
				List<string> espnIdList = (from p in myPlayers select p.PlayerId).ToList();
				List<tbl_ff_players> playerEntityList = db.tbl_ff_players.Where(x => espnIdList.Contains(x.Espn) && x.Cbs != null).ToList();
				foreach (tbl_ff_players entityPlayer in playerEntityList)
				{
					Player espnPlayer = (from p in myPlayers where p.PlayerId == entityPlayer.Espn select p).FirstOrDefault();
					espnPlayer.PlayerAltId = entityPlayer.Cbs;
				}
			}

			return myPlayers;
		}

		protected void GetSchedules(ref int week, ref List<tbl_ff_matchups> matchups)
		{
			FantasyFootballEntities db = new FantasyFootballEntities();
			tbl_ff_weeks myWeek = db.tbl_ff_weeks.Where(w => w.StartDate <= DateTime.Now && w.EndDate >= DateTime.Now).SingleOrDefault();
			if (myWeek != null)
			{
				List<tbl_ff_matchups> matches = db.tbl_ff_matchups.Where(mch => mch.Week <= myWeek.Id + 3).OrderBy(o => o.Date).ToList();
				//Correct Jacksonville abbrev
				foreach (tbl_ff_matchups matchrow in matches)
				{
					switch (matchrow.HomeTeam)
					{
						case "JAC":
							matchrow.HomeTeam = "JAX";
							break;
						case "WAS":
							matchrow.HomeTeam = "WSH";
							break;
					}
					switch (matchrow.AwayTeam)
					{
						case "JAC":
							matchrow.AwayTeam = "JAX";
							break;
						case "WAS":
							matchrow.AwayTeam = "WSH";
							break;
					}
				}

				week = myWeek.Id;
				matchups = matches;
			}
		}

		protected void SetTeamWeeklyStats(string leagueId, int weeks)
		{
			List<TeamWeeklyStats> myStats = new List<TeamWeeklyStats>();
			List<TeamWeeklyStats> opponentStats = new List<TeamWeeklyStats>();
			FantasyFootballEntities db = new FantasyFootballEntities();
			tbl_ff_weeks myWeek = db.tbl_ff_weeks.Where(w => w.StartDate <= DateTime.Now && w.EndDate >= DateTime.Now).SingleOrDefault();
			if (myWeek != null)
			{
				List<tbl_ff_matchups> matchups = db.tbl_ff_matchups.Where(mch => mch.Week >= myWeek.Id - weeks).OrderBy(o => o.Date).ToList();
				//Correct Jacksonville abbrev
				foreach (tbl_ff_matchups matchrow in matchups)
				{
					switch (matchrow.HomeTeam)
					{
						case "JAC":
							matchrow.HomeTeam = "JAX";
							break;
						case "WAS":
							matchrow.HomeTeam = "WSH";
							break;
					}
					switch (matchrow.AwayTeam)
					{
						case "JAC":
							matchrow.AwayTeam = "JAX";
							break;
						case "WAS":
							matchrow.AwayTeam = "WSH";
							break;
					}
				}

				for (int i = myWeek.Id - weeks; i < myWeek.Id; i++)
				{
					bool reachedZero = false; int count = 0;
					while (reachedZero == false)
					{
						string html = Functions.GetHttpHtml(string.Format("http://games.espn.go.com/ffl/leaders?leagueId={0}&scoringPeriodId={1}&startIndex={2}", leagueId, i, count), (string)Session["espn"]);
						MatchCollection myTRs = Regex.Matches(html, @"(?i)<tr[^>]+pncPlayerRow[^>]+>(?<Content>.*?)</tr>", RegexOptions.Singleline);
						if (myTRs.Count > 0)
						{
							foreach (Match myTR in myTRs)
							{
								MatchCollection myTDs = Regex.Matches(myTR.Groups["Content"].Value, @"(?i)<td[^>]*>(?<Content>.*?)</td>", RegexOptions.Singleline);
								if (myTDs.Count > 0)
								{
									Match myPlayerMatch = Regex.Match(myTDs[0].Groups["Content"].Value, @"(?i)playerid=""(?<PlayerId>\d+)""[^>]*>(?<PlayerName>[^<]+)</a>\W+(?<Team>\w{2,3})&nbsp;(?<Position>\w{1,4})", RegexOptions.Singleline);
									//Match myPlayerMatch = Regex.Match(myTDs[1].Groups["Content"].Value, @"(?i)(?<Team>\w+)\s-\s(?<Position>\w+)</span>", RegexOptions.Singleline);
									Decimal myPoints;
									if (myPlayerMatch.Success && decimal.TryParse(Functions.StripHtmlTags(myTDs[myTDs.Count - 1].Groups["Content"].Value), out myPoints))
									{
										if (myPoints > 0)
										{
											TeamWeeklyStats tmpStats = myStats.Where(s => s.Team == myPlayerMatch.Groups["Team"].Value && s.Position == myPlayerMatch.Groups["Position"].Value && s.Week == i).SingleOrDefault();
											if (tmpStats != null)
											{
												tmpStats.Points += myPoints;
											}
											else
											{
												myStats.Add(new TeamWeeklyStats()
												{
													Week = i,
													Team = myPlayerMatch.Groups["Team"].Value,
													Position = myPlayerMatch.Groups["Position"].Value,
													Points = myPoints
												});
											}
										}
										else
										{
											reachedZero = true;
											break;
										}
									}
								}
							}
						}
						else
						{
							break;
						}

						//increment results count
						count += 50;
					}
				}

				//Calculate opposite results for opponents		
				Dictionary<string, int> teamWeekCounts = new Dictionary<string, int>();
				for (int i = myWeek.Id - weeks; i < myWeek.Id; i++)
				{
					List<tbl_ff_matchups> myMatchups = matchups.Where(mch => mch.Week == i).ToList();
					foreach (tbl_ff_matchups match in myMatchups)
					{
						List<TeamWeeklyStats> homeStats = myStats.Where(hm => hm.Week == i && hm.Team.ToUpper() == match.HomeTeam).Select(hm => new TeamWeeklyStats() { Week = 0, Points = hm.Points, Team = match.AwayTeam, Position = hm.Position }).ToList();
						List<TeamWeeklyStats> awayStats = myStats.Where(aw => aw.Week == i && aw.Team.ToUpper() == match.AwayTeam).Select(aw => new TeamWeeklyStats() { Week = 0, Points = aw.Points, Team = match.HomeTeam, Position = aw.Position }).ToList();
						List<TeamWeeklyStats> oppStats = homeStats.Union(awayStats).ToList();

						foreach (TeamWeeklyStats myWeeklyStat in oppStats)
						{
							TeamWeeklyStats opponentStat = opponentStats.Where(ost => ost.Team == myWeeklyStat.Team && ost.Position == myWeeklyStat.Position).SingleOrDefault();
							if (opponentStat != null)
							{
								opponentStat.Points += myWeeklyStat.Points;
							}
							else
							{
								opponentStats.Add(myWeeklyStat);
							}
						}

						//Add to week count tally for HomeTeam
						if (teamWeekCounts.ContainsKey(match.HomeTeam))
							teamWeekCounts[match.HomeTeam] += 1;
						else
							teamWeekCounts.Add(match.HomeTeam, 1);
						//And for AwayTeam too
						if (teamWeekCounts.ContainsKey(match.AwayTeam))
							teamWeekCounts[match.AwayTeam] += 1;
						else
							teamWeekCounts.Add(match.AwayTeam, 1);
					}
				}

				//Create a collection ranking teams by points allowed
				List<string> myPositions = new List<string>() { "QB", "RB", "WR", "TE", "K", "DE", "DT", "LB", "S", "CB" };
				Dictionary<string, string[]> myTeamsPointsDictionary = new Dictionary<string, string[]>();
				myPositions = myPositions.Intersect(opponentStats.Select(myp => myp.Position).Distinct().ToList()).ToList();
				List<string> myTeams = opponentStats.Select(s => s.Team.ToUpper()).Distinct().ToList();
				foreach (string position in myPositions)
				{
					Dictionary<string, decimal> teamPts = new Dictionary<string, decimal>();
					foreach (string myTeam in myTeams)
					{
						int weekCount = teamWeekCounts[myTeam];
						decimal pts = opponentStats.Where(w => w.Position == position && w.Team == myTeam).Sum(d => d.Points);
						teamPts.Add(myTeam, pts / weekCount);
					}

					if (teamPts.Count > 0)
						myTeamsPointsDictionary.Add(position, teamPts.OrderBy(o => o.Value).Select(s => s.Key).ToArray());
				}

				//Save stats to Application scope
				Common.Functions.SetWeeklyStats(new ApplicationWeeklyStats() { Website = "ESPN", LeagueId = leagueId, PositionStats = myTeamsPointsDictionary });
			}
		}
	}
}
