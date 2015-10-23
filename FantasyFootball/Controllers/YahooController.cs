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

		public ActionResult SoS()
		{
			ApplicationWeeklyStats myStats = new ApplicationWeeklyStats();
			List<tbl_ff_matchups> matchups = new List<tbl_ff_matchups>();
            int week = 1;
			
			if (Session["yahoo"] != null)
			{
				//Do a stat scrape
				myStats = Common.Functions.GetWeeklyStats("Yahoo", Request.Params["leagueId"]);
				if(myStats == null)
				{
					SetTeamWeeklyStats(Request.Params["leagueId"], 3);
					myStats = Common.Functions.GetWeeklyStats("Yahoo", Request.Params["leagueId"]);
				}
			}
			else
				return RedirectToAction("Yahoo", "Login");

			GetSchedules(ref week, ref matchups);

			ViewBag.Title = "Strength of Schedule";
			ViewBag.LeagueId = Request.Params["leagueId"];
			ViewBag.Matchups = matchups;
			ViewBag.Week = week;
			return View(myStats);
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
			Dictionary<string, List<Owner>> myTeams = new Dictionary<string, List<Owner>>();

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
							MatchCollection hrefMatches = Regex.Matches(myTDs[(isEditPage ? 2 : 1)].Groups["Content"].Value, @"(?i)<a.*?href=""(?<Href>[^""]+)[^>]+>(?<Content>.*?)</a>", RegexOptions.Singleline);
							if(hrefMatches.Count > 0)
							{
								Match playerMatch = Regex.Match(myTDs[(isEditPage ? 2 : 1)].Groups["Content"].Value, @"(?i)ysf-player-name.*?http://sports.yahoo.com/nfl/(players|teams)/(?<PlayerId>[^""]+)[^>]+>(?<PlayerName>[^<]+)</a>.*?(?<Team>\w{2,3})\s+\-\s+(?<Position>\w{2,3})</span>", RegexOptions.Singleline);
								Match opponentMatch = Regex.Match(hrefMatches[hrefMatches.Count-1].Groups["Content"].Value, @"(?i)^(?<Note>[^<]+)<a[^>]+>(?<Opponent>\w+)$", RegexOptions.Singleline);
								Match injuryMatch = Regex.Match(myTDs[(isEditPage ? 2 : 1)].Groups["Content"].Value, @"(?i)F-injury[^>]+>(?<InjuryStatus>[^<]+)</span>", RegexOptions.Singleline);
								if (playerMatch.Success)
								{
									myPlayers.Add(new Player()
									{
										PlayerId = playerMatch.Groups["PlayerId"].Value,
										Name = playerMatch.Groups["PlayerName"].Value + (injuryMatch.Success ? @" - <span class=""txRed"">" + injuryMatch.Groups["InjuryStatus"].Value.Substring(0, 1) + "</span>" : string.Empty),
										Position = playerMatch.Groups["Position"].Value,
										Team = playerMatch.Groups["Team"].Value.ToLower(),
										Opponent = opponentMatch.Groups["Opponent"].Value.ToLower(),
										Note01 = opponentMatch.Groups["Note"].Value.Trim()
									});
								}
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
					if(yahooPlayer != null)
					{
						yahooPlayer.PlayerAltId = entityPlayer.Cbs;
					}					
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

		protected Dictionary<string, List<Owner>> GetTeams(string leagueId)
		{
			string html = Functions.GetHttpHtml(string.Format("http://football.fantasysports.yahoo.com/f1/{0}/", leagueId), (string)Session["yahoo"]);
			Match htmlMatch = Regex.Match(html, @"(?i)standingstable.*?<tbody>(?<Content>.*?)</tbody>.*?</table>", RegexOptions.Singleline);
			Dictionary<string, List<Owner>> myOwners = new Dictionary<string, List<Owner>>();
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
							if(myTDs.Count == 1)
							{
								keyName = Functions.StripHtmlTags(myTDs[0].Groups["Content"].Value);
								myOwners.Add(keyName, new List<Owner>());
							}
							else if(myTDs.Count > 1)
							{
								Match ownerMatch = Regex.Match(myTDs[1].Groups["Content"].Value, @"(?i)src=""(?<Avatar>[^""]+)"".*?href=""/f1/\d+/(?<OwnerId>\d+)"">(?<TeamName>.*?)</a>", RegexOptions.Singleline);
								if (ownerMatch.Success)
								{
									myOwners[keyName].Add(new Owner()
									{
										Rank = Int32.Parse(Functions.StripHtmlTags(myTDs[0].Groups["Content"].Value)),
										Id = ownerMatch.Groups["OwnerId"].Value,
										TeamName = ownerMatch.Groups["TeamName"].Value,
										Avatar = ownerMatch.Groups["Avatar"].Value,
										Record = myTDs[2].Groups["Content"].Value
									});
								}								
							}
						}						
                    }
				}
			}

			return myOwners;
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
					}
					switch (matchrow.AwayTeam)
					{
						case "JAC":
							matchrow.AwayTeam = "JAX";
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
			if(myWeek != null)
			{			
				List<tbl_ff_matchups> matchups = db.tbl_ff_matchups.Where(mch => mch.Week >= myWeek.Id - weeks && mch.Week < myWeek.Id).OrderBy(o => o.Date).ToList();
				for (int i = myWeek.Id - 2; i < myWeek.Id; i++)
				{
					bool reachedZero = false; int count = 0;
					while(reachedZero == false)
					{
						string html = Functions.GetHttpHtml(string.Format("http://football.fantasysports.yahoo.com/f1/{0}/players?status=ALL&pos=ALL&stat1=S_W_{1}&sort=PTS&sdir=1&count={2}", leagueId, i, count), (string)Session["yahoo"]);
						Match htmlMatch = Regex.Match(html, @"(?i)class=""players"">\s+<table[^>]+>.*?<tbody>(?<Content>.*?)</tbody>.*?</table>", RegexOptions.Singleline);
						if(htmlMatch.Success)
						{
							MatchCollection myTRs = Regex.Matches(htmlMatch.Groups["Content"].Value, @"(?i)<tr[^>]*>(?<Content>.*?)</tr>", RegexOptions.Singleline);
							if(myTRs.Count > 0)
							{
								foreach (Match myTR in myTRs)
								{
									MatchCollection myTDs = Regex.Matches(myTR.Groups["Content"].Value, @"(?i)<td[^>]*>(?<Content>.*?)</td>", RegexOptions.Singleline);
									if (myTDs.Count > 0)
									{
										Match myPlayerMatch = Regex.Match(myTDs[1].Groups["Content"].Value, @"(?i)(?<Team>\w+)\s-\s(?<Position>\w+)</span>", RegexOptions.Singleline);
										Decimal myPoints;
										if (myPlayerMatch.Success && decimal.TryParse(Functions.StripHtmlTags(myTDs[5].Groups["Content"].Value), out myPoints))
										{
											if(myPoints > 0)
											{
												TeamWeeklyStats tmpStats = myStats.Where(s => s.Team == myPlayerMatch.Groups["Team"].Value && s.Position == myPlayerMatch.Groups["Position"].Value && s.Week == i).SingleOrDefault();
												if(tmpStats != null)
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
						}
						else
						{
							break;
						}

						//increment results count
						count += 25;
                    }					
				}

				//Calculate opposite results for opponents				
				Dictionary<string, int> teamWeekCounts = new Dictionary<string, int>();
				for (int i = myWeek.Id - 2; i < myWeek.Id; i++)
				{
					List<tbl_ff_matchups> myMatchups = matchups.Where(mch => mch.Week == i).ToList();
					foreach(tbl_ff_matchups match in myMatchups)
					{
						List<TeamWeeklyStats> homeStats = myStats.Where(hm => hm.Week == i && hm.Team.ToUpper() == match.HomeTeam).Select(hm => new TeamWeeklyStats() { Week = 0, Points = hm.Points, Team = match.AwayTeam, Position = hm.Position }).ToList();
						List<TeamWeeklyStats> awayStats = myStats.Where(aw => aw.Week == i && aw.Team.ToUpper() == match.AwayTeam).Select(aw => new TeamWeeklyStats() { Week = 0, Points = aw.Points, Team = match.HomeTeam, Position = aw.Position }).ToList();
						List<TeamWeeklyStats> oppStats = homeStats.Union(awayStats).ToList();

						foreach(TeamWeeklyStats myWeeklyStat in oppStats)
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
				List<string> myPositions = new List<string>() { "QB", "RB", "WR", "TE", "DEF", "K" };
                Dictionary<string, SortedList<decimal, string>> myTeamsPointsDictionary = new Dictionary<string, SortedList<decimal, string>>();
                myPositions = myPositions.Intersect(opponentStats.Select(myp => myp.Position).Distinct().ToList()).ToList();
				List<string> myTeams = opponentStats.Select(s => s.Team.ToUpper()).Distinct().ToList();
                foreach (string position in myPositions)
				{
					SortedList<decimal, string> positionStats = new SortedList<decimal, string>();
                    foreach (string myTeam in myTeams)
					{
						var pointsAllowed = opponentStats.Where(w => w.Team.ToUpper() == myTeam.ToUpper() && w.Position == position).Select(s => s.Points).ToList();
						var weeksPlayed = matchups.Where(w => w.HomeTeam == myTeam.ToUpper() || w.AwayTeam == myTeam.ToUpper());
						positionStats.Add((pointsAllowed != null ? (pointsAllowed.Sum() / weeksPlayed.Count()) : 0), myTeam.ToUpper());                        
					}

					myTeamsPointsDictionary.Add(position, positionStats);
				}

				//Save stats to Application scope
				Common.Functions.SetWeeklyStats(new ApplicationWeeklyStats() { Website = "Yahoo", LeagueId = leagueId, PositionStats = myTeamsPointsDictionary });
			}
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
							MatchCollection playerMatch = Regex.Matches(myTDs[1].Groups["Content"].Value, @"(?i)http://sports.yahoo.com/nfl/(players|teams)/(?<PlayerId>[^""]+)[^>]+>(?<PlayerName>[^<]+)</a>.*?<span[^>]*>(?<Team>\w{2,3})\s+\-\s+(?<Position>\w{2,3})</span>", RegexOptions.Singleline);
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