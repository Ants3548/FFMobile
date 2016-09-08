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

		public static List<RankingsPost> GetRankingsWeeklyPPR(List<string> playerIds)
		{
			List<string> myPositions = new List<string>() { "QB", "RB", "WR", "TE", "DST" };
			List<RankingsPost> myRankings = new List<RankingsPost>();

			foreach(string myPosition in myPositions)
			{
				string html = Functions.GetHttpHtml(string.Format("http://www.cbssports.com/fantasy/football/rankings/ppr/{0}/weekly/", myPosition), null);

				//Make sure the author data is populated
				if (myRankings.Count <= 0)
				{
					myRankings = GetCbsAuthors(html);
				}

				MatchCollection myRankTableMatches = Regex.Matches(html, @"(?i)rankings-tbl-data-inner[^>]+>(?<Html>.*?)</table>", RegexOptions.Singleline);

				for(int i = 0; i < myRankTableMatches.Count; i++)
				{
					Match myRankTable = myRankTableMatches[i];
					List<Ranking> myTableRankings = new List<Ranking>();
					MatchCollection myPlayers = Regex.Matches(myRankTable.Groups["Html"].Value, @"(?i)/fantasy/football/players/(?<PlayerId>\d+)/[^>]*>(?<PlayerName>\w.*?)</a>.*?<span[^>]+>(?<PlayerTeam>[^<]+)", RegexOptions.Singleline);

					for(int j = 0; j < myPlayers.Count; j++)
					{
						Match myPlayerMatch = myPlayers[j];
						myTableRankings.Add(new Ranking()
						{
							Id = myPlayerMatch.Groups["PlayerId"].Value,
							Name = myPlayerMatch.Groups["PlayerName"].Value,
							//Opponent = myPlayerMatch.Groups["Opponent"].Value.Replace("@", string.Empty),
							Team = myPlayerMatch.Groups["PlayerTeam"].Value.Trim(),
							Rank = j + 1,
							Position = myPosition,
							Active = ((playerIds.Contains(myPlayerMatch.Groups["PlayerId"].Value)) ? false : true)
						});
					}

					//Make sure Author dictionary exists
					if(myRankings[i].MultiPartRankings == null)
					{
						myRankings[i].MultiPartRankings = new Dictionary<string, List<Ranking>>();
                    }
					if(!myRankings[i].MultiPartRankings.ContainsKey(myPosition))
					{
						myRankings[i].MultiPartRankings.Add(myPosition, myTableRankings);
                    }
                }
			}			

			return myRankings;
		}

		public static List<RankingsPost> GetCbsAuthors(string html)
		{
			List<RankingsPost> myRankings = new List<RankingsPost>();
			Match writersMatch = Regex.Match(html, @"(?i)<div\s+id=""experts"">.*?</article>", RegexOptions.Singleline);
			MatchCollection myWriters = Regex.Matches(writersMatch.Value, @"class=""rankings-author"".*?</time>", RegexOptions.Singleline);

			for (int i = 0; i < myWriters.Count; i++)
			{
				Match myWriter = myWriters[i];
				RankingsPost myRankingPost = new RankingsPost();

				Match imageMatch = Regex.Match(myWriter.Value, @"(?i)<img\s+src=""(?<Image>[^""]+)""", RegexOptions.Singleline);
				myRankingPost.Thumbnail = imageMatch.Groups["Image"].Value.Trim();

				Match authorMatch = Regex.Match(myWriter.Value, @"(?i)rel=""author"".*?>(?<Author>[^<]+)</a>", RegexOptions.Singleline);
				myRankingPost.Author = authorMatch.Groups["Author"].Value.Trim();

				Match twitterMatch = Regex.Match(myWriter.Value, @"(?i)class=""twitter"".*?>(?<Twitter>[^<]+)</span>", RegexOptions.Singleline);
				myRankingPost.Twitter = twitterMatch.Groups["Twitter"].Value.Trim();

				Match timeMatch = Regex.Match(myWriter.Value, @"(?i)<time[^>]+>(?<Time>[^<]+)</time>", RegexOptions.Singleline);
				myRankingPost.TimeStamp = timeMatch.Groups["Time"].Value.Trim();

				myRankings.Add(myRankingPost);
			}

			return myRankings;
        }
	}
}