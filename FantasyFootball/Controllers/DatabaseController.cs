using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

using FantasyFootball.DAL;
using FantasyFootball.Common;

namespace FantasyFootball.Controllers
{
	public class DatabaseController : Controller
	{
		//
		// GET: /Database/

		public ActionResult Cbs()
		{

			//Get our HTML list of HTML pages
			WebClient client = new WebClient();
			client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
			Stream data = client.OpenRead("http://www.cbssports.com/nfl/teams");
			StreamReader reader = new StreamReader(data);
			string s = reader.ReadToEnd();
			data.Close();
			reader.Close();

			MatchCollection myTeamPages = Regex.Matches(s, @"/nfl/teams/roster/[^""]+", RegexOptions.Singleline);
			if (myTeamPages.Count > 0)
			{
				FantasyFootballEntities db = new FantasyFootballEntities();
				foreach (Match teamUrl in myTeamPages)
				{
					WebClient myclient = new WebClient();
					myclient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
					Stream mydata = myclient.OpenRead("http://www.cbssports.com" + teamUrl.Value);
					StreamReader myreader = new StreamReader(mydata);
					string mys = myreader.ReadToEnd();
					mydata.Close();
					myreader.Close();

					string myTeam = Regex.Match(teamUrl.Value, @"/roster/(?<Team>[^/]+)/", RegexOptions.Singleline).Groups["Team"].Value;
					Match myHtml = Regex.Match(mys, @"id=""roster"".*?class=""data""", RegexOptions.Singleline);
					if (myHtml.Success)
					{
						MatchCollection myPlayers = Regex.Matches(myHtml.Value, @"(?i)<td[^>]*>(?<JerseyNumber>\d+)</td>\s*<td[^>]*>\s*<a[^>]*?id=""(?<CbsId>[^""]+)""[^>]*>(?<PlayerName>.*?)</a>.*?</td>\s*<td[^>]*>(?<Position>.*?)</td>", RegexOptions.Singleline);
						if (myPlayers.Count > 0)
						{
							foreach (Match player in myPlayers)
							{
								string CbsId = Regex.Replace(player.Groups["CbsId"].Value, @"\D", string.Empty);
								string[] Name = player.Groups["PlayerName"].Value.Replace(" (IR)", string.Empty).Split(new char[] { ',' });

								tbl_ff_players playerObj = db.tbl_ff_players.FirstOrDefault(x => x.Cbs == CbsId);
								if (playerObj != null)
								{
									playerObj.FirstName = Name[1].Trim();
									playerObj.LastName = Name[0].Trim();
									playerObj.JerseyNumber = player.Groups["JerseyNumber"].Value.Trim();
									playerObj.Position = player.Groups["Position"].Value.Trim();
									playerObj.Team = myTeam;
								}
								else
								{
									db.tbl_ff_players.Add(new tbl_ff_players()
									{
										FirstName = Name[1].Trim(),
										LastName = Name[0].Trim(),
										JerseyNumber = player.Groups["JerseyNumber"].Value.Trim(),
										Position = player.Groups["Position"].Value.Trim(),
										Team = myTeam,
										Cbs = CbsId
									});
								}
							}
						}
					}
				}
				db.SaveChanges();
			}

			return View();
		}

		//public ActionResult CbsStatsWeeklyPlayers()
		//{
		//	FantasyFootballEntities db = new FantasyFootballEntities();
		//	for (int i = 1; i < 17; i++)
		//	{
		//		//Get our HTML list of HTML pages
		//		WebClient client = new WebClient();
		//		client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
		//		Stream data = client.OpenRead("http://www.cbssports.com/nfl/scoreboard/2013/week" + i);
		//		StreamReader reader = new StreamReader(data);
		//		string s = reader.ReadToEnd();
		//		data.Close();
		//		reader.Close();

		//		MatchCollection myScoresPages = Regex.Matches(s, @"(?i)<a[^>]+?href=""(?<Uri>[^""]+)""[^>]*>GameTracker</a>", RegexOptions.Singleline);
		//		if (myScoresPages.Count > 0)
		//		{
		//			List<tbl_ff_stats_weekly_players> PlayerIds = new List<tbl_ff_stats_weekly_players>();
		//			foreach (Match myUri in myScoresPages)
		//			{
		//				//Get our HTML list of HTML pages
		//				WebClient myScoresClient = new WebClient();
		//				myScoresClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
		//				Stream myScoresData = myScoresClient.OpenRead("http://www.cbssports.com" + myUri.Groups["Uri"].Value);
		//				StreamReader myScoresReader = new StreamReader(myScoresData);
		//				string myScoresHtml = myScoresReader.ReadToEnd();
		//				myScoresData.Close();
		//				myScoresReader.Close();

		//				MatchCollection myStatTables = Regex.Matches(myScoresHtml, @"<span\sclass=""pStats(?<StatId>\d+)"">(?<Html>.*?)</span>", RegexOptions.Singleline);
		//				string myTempHtml = string.Empty;
		//				foreach (Match myHtml in myStatTables)
		//					myTempHtml += myHtml.Value;

		//				MatchCollection myPlayerIds = Regex.Matches(myTempHtml, @"/nfl/players/playerpage/(?<PlayerId>\d+)/", RegexOptions.Singleline);
		//				List<String> PlayerTally = new List<string>();

		//				if (myPlayerIds.Count > 0)
		//					foreach (Match myPlayer in myPlayerIds)
		//						if (!PlayerTally.Contains(myPlayer.Groups["PlayerId"].Value))
		//						{
		//							PlayerIds.Add(new tbl_ff_stats_weekly_players()
		//							{
		//								Cbs = myPlayer.Groups["PlayerId"].Value,
		//								Week = i
		//							});
		//							PlayerTally.Add(myPlayer.Groups["PlayerId"].Value);
		//						}

		//				if (myStatTables.Count > 0)
		//				{
		//					foreach (Match myStatsHtml in myStatTables)
		//					{
		//						MatchCollection myHtmlRows = Regex.Matches(myStatsHtml.Groups["Html"].Value, @"(?i)<tr[^>]*>.*?</tr>", RegexOptions.Singleline);
		//						if (myHtmlRows.Count > 0)
		//						{
		//							MatchCollection myColumns = Regex.Matches(myHtmlRows[0].Value, @"(?i)<td[^>]*>(?<Header>.*?)</td>", RegexOptions.Singleline);
		//							if ((myColumns.Count > 0) && (myHtmlRows.Count > 1))
		//							{
		//								for (int j = 1; j < myHtmlRows.Count; j++)
		//								{
		//									Match myPlayerId = Regex.Match(myHtmlRows[j].Value, @"(?i)/playerpage/(?<PlayerId>\d+)/", RegexOptions.Singleline);
		//									if (myPlayerId.Success)
		//									{
		//										tbl_ff_stats_weekly_players myPlayer = PlayerIds.SingleOrDefault(x => x.Cbs == myPlayerId.Groups["PlayerId"].Value);
		//										if (myPlayer != null)
		//										{
		//											MatchCollection myCells = Regex.Matches(myHtmlRows[j].Value, @"(?i)<td[^>]*>(?<Content>.*?)</td>", RegexOptions.Singleline);
		//											for (int k = 1; k < myColumns.Count; k++)
		//											{
		//												switch (myStatsHtml.Groups["StatId"].Value)
		//												{
		//													//QB
		//													case "00":
		//													case "10":
		//														switch (myColumns[k].Groups["Header"].Value)
		//														{
		//															case "CP/AT":
		//																string[] myVar01 = myCells[k].Groups["Content"].Value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		//																if (myVar01.Length == 2)
		//																{
		//																	myPlayer.PassComp = Convert.ToInt32(myVar01[0]);
		//																	myPlayer.PassAtt = Convert.ToInt32(myVar01[1]);
		//																}
		//																break;
		//															case "YDS":
		//																myPlayer.PassYds = Convert.ToInt32(myCells[k].Groups["Content"].Value);
		//																break;
		//															case "TD":
		//																myPlayer.PassTDs = Convert.ToInt32(myCells[k].Groups["Content"].Value);
		//																break;
		//															case "INT":
		//																myPlayer.PassINTs = Convert.ToInt32(myCells[k].Groups["Content"].Value);
		//																break;
		//														}
		//														break;

		//													//RB
		//													case "01":
		//													case "11":
		//														switch (myColumns[k].Groups["Header"].Value)
		//														{
		//															case "ATT":
		//																myPlayer.Rushes = Convert.ToInt32(myCells[k].Groups["Content"].Value);
		//																break;
		//															case "YDS":
		//																myPlayer.RushYds = Convert.ToInt32(myCells[k].Groups["Content"].Value);
		//																break;
		//															case "TD":
		//																myPlayer.RushTDs = Convert.ToInt32(myCells[k].Groups["Content"].Value);
		//																break;
		//														}
		//														break;

		//													//WR
		//													case "02":
		//													case "12":
		//														switch (myColumns[k].Groups["Header"].Value)
		//														{
		//															case "REC":
		//																myPlayer.Receptions = Convert.ToInt32(myCells[k].Groups["Content"].Value);
		//																break;
		//															case "YDS":
		//																myPlayer.RecYards = Convert.ToInt32(myCells[k].Groups["Content"].Value);
		//																break;
		//															case "TD":
		//																myPlayer.RecTDs = Convert.ToInt32(myCells[k].Groups["Content"].Value);
		//																break;
		//														}
		//														break;
		//												}
		//											}
		//										}
		//									}
		//								}
		//							}
		//						}
		//					}
		//				}
		//				//var PlayerEntities = db.tbl_ff_players.Where(x => PlayerIds.Contains(x.Cbs)).ToList();



		//				//return View();
		//			}
		//			foreach (tbl_ff_stats_weekly_players weekStat in PlayerIds)
		//				db.tbl_ff_stats_weekly_players.Add(weekStat);

		//			db.SaveChanges();
		//		}
		//		else
		//		{
		//			break;
		//		}
		//	}

		//	return View();
		//}

		public ActionResult Espn()
		{

			//Get our HTML list of HTML pages
			WebClient client = new WebClient();
			client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
			Stream data = client.OpenRead("http://espn.go.com/nfl/teams");
			StreamReader reader = new StreamReader(data);
			string s = reader.ReadToEnd();
			data.Close();
			reader.Close();

			MatchCollection myTeamPages = Regex.Matches(s, @"/nfl/team/roster/_/name/[^""]+", RegexOptions.Singleline);
			if (myTeamPages.Count > 0)
			{
				FantasyFootballEntities db = new FantasyFootballEntities();
				foreach (Match teamUrl in myTeamPages)
				{
					WebClient myclient = new WebClient();
					myclient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
					Stream mydata = myclient.OpenRead("http://espn.go.com" + teamUrl.Value);
					StreamReader myreader = new StreamReader(mydata);
					string mys = myreader.ReadToEnd();
					mydata.Close();
					myreader.Close();

					string myTeam = Regex.Match(teamUrl.Value, @"/name/(?<Team>[^/]+)/", RegexOptions.Singleline).Groups["Team"].Value.ToUpper();
					if (myTeam == "WSH")
						myTeam = "WAS";
					MatchCollection myPlayers = Regex.Matches(mys, @"(?i)<td[^>]*>(?<JerseyNumber>\d+)</td>\s*<td[^>]*>\s*<a\s+href=""http://espn.go.com/nfl/player/_/id/(?<EspnId>\d+)/", RegexOptions.Singleline);
					if (myPlayers.Count > 0)
					{
						foreach (Match player in myPlayers)
						{
							string EspnId = Regex.Replace(player.Groups["EspnId"].Value, @"\D", string.Empty);
							string JerseyNo = player.Groups["JerseyNumber"].Value;
							tbl_ff_players playerObj = db.tbl_ff_players.FirstOrDefault(x => x.Team == myTeam && x.JerseyNumber == JerseyNo);
							if (playerObj != null)
							{
								playerObj.Espn = EspnId;
							}
						}
					}

				}
				db.SaveChanges();
			}

			return View();
		}

		public ActionResult Yahoo()
		{

			//Get our HTML list of HTML pages
			WebClient client = new WebClient();
			client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
			Stream data = client.OpenRead("http://sports.yahoo.com/nfl/teams/");
			StreamReader reader = new StreamReader(data);
			string s = reader.ReadToEnd();
			data.Close();
			reader.Close();
			//<a href="/nfl/teams/buf/roster/" title="Roster" data-rapid_p="4">Roster</a>
			MatchCollection myTeamPages = Regex.Matches(s, @"/nfl/teams/(?<Team>[^""]+)/roster/", RegexOptions.Singleline);
			if (myTeamPages.Count > 0)
			{
				FantasyFootballEntities db = new FantasyFootballEntities();
				foreach (Match teamUrl in myTeamPages)
				{
					WebClient myclient = new WebClient();
					myclient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
					Stream mydata = myclient.OpenRead("http://sports.yahoo.com/nfl/teams/" + teamUrl.Groups["Team"].Value + "/roster/");
					StreamReader myreader = new StreamReader(mydata);
					string mys = myreader.ReadToEnd();
					mydata.Close();
					myreader.Close();

					string myTeam = teamUrl.Groups["Team"].Value.ToUpper();
					switch(myTeam)
					{
						case "GNB":
							myTeam = "GB";
							break;
						case "KAN":
							myTeam = "KC";
							break;
						case "NOR":
							myTeam = "NO";
							break;
						case "NWE":
							myTeam = "NE";
							break;
						case "SDG":
							myTeam = "SD";
							break;
						case "SFO":
							myTeam = "SF";
							break;
						case "TAM":
							myTeam = "TB";
							break;
						case "WSH":
							myTeam = "WAS";
							break;
					}
						
					MatchCollection myPlayers = Regex.Matches(mys, @"(?i)<td\s*class=""number"">(?<JerseyNumber>\d+)</td>\s*<td[^>]*>\s*<a\s+href=""/nfl/players/(?<YahooId>\d+)/", RegexOptions.Singleline);
					if (myPlayers.Count > 0)
					{
						foreach (Match player in myPlayers)
						{
							string YahooId = Regex.Replace(player.Groups["YahooId"].Value, @"\D", string.Empty);
							string JerseyNo = player.Groups["JerseyNumber"].Value;
							tbl_ff_players playerObj = db.tbl_ff_players.FirstOrDefault(x => x.Team == myTeam && x.JerseyNumber == JerseyNo);
							if (playerObj != null)
							{
								playerObj.Yahoo = YahooId;
							}
						}
					}

				}
				db.SaveChanges();
			}

			return View();
		}

		public ActionResult UsaToday()
		{
			FantasyFootballEntities db = new FantasyFootballEntities();
			int myYear = DateTime.Now.Year;

			for (int i = 1; i < 17; i++)
			{
				WebClient myclient = new WebClient();
				myclient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
				Stream mydata = myclient.OpenRead(string.Format("http://www.usatoday.com/sports/nfl/schedule/{1}/season-regular/{0}/", i, myYear));
				StreamReader myreader = new StreamReader(mydata);
				string html = myreader.ReadToEnd();
				mydata.Close();
				myreader.Close();

				MatchCollection myTables = Regex.Matches(html, @"(?i)<table.*?class=""schedule""[^>]*>.*?<b>(?<Date>.*?)</b>.*?<tbody>(?<Content>.*?)</tbody>.*?</table>", RegexOptions.Singleline);
				if(myTables.Count > 0)
				{
					foreach (Match myTable in myTables)
					{
						MatchCollection myTRs = Regex.Matches(myTable.Groups["Content"].Value, @"(?i)<tr>(?<Content>.*?)</tr>", RegexOptions.Singleline);
						if (myTRs.Count > 0)
						{
							foreach (Match myTR in myTRs)
							{
								MatchCollection myTDs = Regex.Matches(myTR.Groups["Content"].Value, @"(?i)<t(d|h)[^>]*>(?<Content>.*?)</t(d|h)>", RegexOptions.Singleline);
								MatchCollection myTeams = Regex.Matches(myTDs[0].Groups["Content"].Value, @"(?<Team>\w+)\.png", RegexOptions.Singleline);
								string time = Regex.Replace(myTDs[1].Groups["Content"].Value, @"(AM|PM)\sET", @" $1", RegexOptions.Singleline);

                                if (myTeams.Count > 0)
								{
									db.tbl_ff_matchups.Add(new tbl_ff_matchups()
									{
										HomeTeam = myTeams[1].Groups["Team"].Value.Trim(),
										AwayTeam = myTeams[0].Groups["Team"].Value.Trim(),
										Season = 2015,
										Week = i,
										Date = DateTime.Parse(string.Format("{0}, 2015 {1}", Regex.Replace(myTable.Groups["Date"].Value, @"\w{2}$", string.Empty, RegexOptions.Singleline), time))
									});
								}								
							}
						}
					}			
				}				
			}

			db.SaveChanges();

			return View();
		}

		//public ActionResult FFToolboxSchedule()
		//{
		//	string html = System.IO.File.ReadAllText(Server.MapPath(@"~/2013-schedule.html"));
		//	MatchCollection myTeams = Regex.Matches(html, @"<tr>(?<myTds>.*?)</tr>", RegexOptions.Singleline);

		//	if (myTeams.Count > 0)
		//	{
		//		FantasyFootballEntities db = new FantasyFootballEntities();
		//		foreach (Match myTeam in myTeams)
		//		{
		//			MatchCollection myTds = Regex.Matches(myTeam.Value, @"<td[^>]*>(?<Content>.*?)</td>", RegexOptions.Singleline);
		//			if (myTds.Count > 0)
		//			{
		//				Match myTeamName = Regex.Match(myTds[0].Value, @"<a[^>]+>(?<Team>.*?)</a>", RegexOptions.Singleline);
		//				for (int j = 1; j < myTds.Count; j++)
		//				{
		//					db.tbl_ff_matchups_2013.Add(new tbl_ff_matchups_2013()
		//					{
		//						Team = myTeamName.Groups["Team"].Value,
		//						Week = j,
		//						Opponent = myTds[j].Groups["Content"].Value.Replace("@", string.Empty),
		//						IsHome = (!myTds[j].Value.Contains("@")),
		//						IsBye = (myTds[j].Groups["Content"].Value == "BYE")
		//					});
		//				}
		//			}
		//		}
		//		db.SaveChanges();
		//	}

		//	return View();
		//}

	}
}
