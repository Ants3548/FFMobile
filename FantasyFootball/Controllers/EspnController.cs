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
			Functions.CheckForSession();

			List<Models.League> myLeagues = new List<League>();

			if (Session["espn"] != null)
				myLeagues = GetLeagues();
			else
				return RedirectToAction("Espn", "Login");

			return View(myLeagues);
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
	}
}
