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

		public ActionResult Team()
		{
			List<Player> myPlayers = new List<Player>();

			if (Session["espn"] != null)
				myPlayers = GetPlayers(Request.Params["leagueId"], Request.Params["teamId"]);
			else
				return RedirectToAction("Yahoo", "Login");

			ViewBag.Title = "Some Team";
			ViewBag.LeagueId = Request.Params["leagueId"];
			ViewBag.TeamId = Request.Params["teamId"];
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

		protected List<Player> GetPlayers(string leagueId, string teamId)
		{
			string html = Functions.GetHttpHtml(string.Format("http://games.espn.go.com/ffl/clubhouse?leagueId={0}&teamId={1}", leagueId, teamId), (string)Session["espn"]);
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
						MatchCollection myTDs = Regex.Matches(myTR.Value, @"(?i)<td[^>]*>(?<Content>.*?)</td>", RegexOptions.Singleline);
						if (myTDs.Count > 0)
						{
							Match playerMatch = Regex.Match(myTDs[1].Groups["Content"].Value, @"(?i)playerid=""(?<PlayerId>\d+)[^>]+>(?<PlayerName>[^<]+)</a>\W+(?<Team>\w{2,3})&nbsp;(?<Position>\w{2,4})</span>", RegexOptions.Singleline);
							Match opponentMatch = Regex.Match(myTDs[(isEditPage ? 5 : 4)].Groups["Content"].Value, @"(?i)<a[^>]+>(?<Opponent>[^<]+)</a>", RegexOptions.Singleline);
							Match opponentRankMatch = Regex.Match(myTDs[(isEditPage ? 14 : 13)].Groups["Content"].Value, @"(?i)<a[^>]+>(?<Content>[^<]+)</a>", RegexOptions.Singleline);
							Match noteMatch = Regex.Match(myTDs[(isEditPage ? 6 : 5)].Groups["Content"].Value, @"(?i)<a[^>]+>(?<Content>[^<]+)</a>", RegexOptions.Singleline);
							if (playerMatch.Success && opponentMatch.Success)
							{
								myPlayers.Add(new Player()
								{
									PlayerId = playerMatch.Groups["PlayerId"].Value,
									Name = playerMatch.Groups["PlayerName"].Value,
									Position = playerMatch.Groups["Position"].Value,
									Team = playerMatch.Groups["Team"].Value.ToLower(),
									Opponent = Regex.Replace(opponentMatch.Groups["Opponent"].Value, @"@", string.Empty, RegexOptions.Singleline).ToLower(),
									OpponentRank = (opponentRankMatch.Success ? opponentRankMatch.Groups["Content"].Value : string.Empty),
									Note01 = (noteMatch.Success ? noteMatch.Groups["Content"].Value : string.Empty)
								});
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
	}
}
