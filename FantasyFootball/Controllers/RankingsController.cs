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
    public class RankingsController : Controller
    {
        //
        // GET: /Rankings/

        public ActionResult Index()
        {
            Functions.CheckForSession();
            
            return View();
        }

		public ActionResult DraftStandard()
		{
			Functions.CheckForSession();

			WebClient client = new WebClient();
			client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

			Stream data = client.OpenRead("http://fantasynews.cbssports.com/fantasyfootball/rankings/top200/yearly");
			StreamReader reader = new StreamReader(data);
			string s = reader.ReadToEnd();
			data.Close();
			reader.Close();

			MatchCollection myWriters = Regex.Matches(s, @"<table[^>]+?class=""data"".*?</table>", RegexOptions.Singleline);
			List<Ranking> myRankings = new List<Ranking>();

			if (myWriters.Count > 0)
			{
				MatchCollection myRanks = Regex.Matches(myWriters[1].Value, @"(?i)<td[^>]*><a[^>]+>(?<Name>.*?)</a>\s+(?<Team>\w+),\s+(?<Position>\w+)", RegexOptions.Singleline);
				for (int i = 0; i < myRanks.Count - 1; i++)
				{
					Match myMatch = myRanks[i];
					myRankings.Add(new Ranking()
					{
						Name = myMatch.Groups["Name"].Value,
						Position = myMatch.Groups["Position"].Value,
						Team = myMatch.Groups["Team"].Value,
						Rank = i + 1
					});
				}
			}

			return View(myRankings);
		}
		
		public ActionResult DraftPPR()
        {
            Functions.CheckForSession();
            
            WebClient client = new WebClient();
            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            Stream data = client.OpenRead("http://fantasynews.cbssports.com/fantasyfootball/rankings/points-per-reception/yearly");
            StreamReader reader = new StreamReader(data);
            string s = reader.ReadToEnd();
            data.Close();
            reader.Close();

            MatchCollection myWriters = Regex.Matches(s, @"<table[^>]+?class=""data"".*?</table>", RegexOptions.Singleline);
            List<Ranking> myRankings = new List<Ranking>();

            if (myWriters.Count > 0)
            {
                MatchCollection myRanks = Regex.Matches(myWriters[1].Value, @"(?i)<td[^>]*><a[^>]+>(?<Name>.*?)</a>\s+(?<Team>\w+),\s+(?<Position>\w+).*?\(\D+(?<Bye>\d+)\)", RegexOptions.Singleline);
                for (int i = 0; i < myRanks.Count - 1; i++)
                {
                    Match myMatch = myRanks[i];
                    myRankings.Add(new Ranking()
                    {
                        Name = myMatch.Groups["Name"].Value,
                        Position = myMatch.Groups["Position"].Value,
                        Bye = Convert.ToInt32(myMatch.Groups["Bye"].Value),
                        Team = myMatch.Groups["Team"].Value,
                        Rank = i + 1
                    });
                }
            }

            return View(myRankings);
        }

        //public ActionResult Weekly()
        //{
        //    Functions.CheckForSession();
            
        //    WebClient client = new WebClient();
        //    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

        //    Stream data = client.OpenRead("http://fantasynews.cbssports.com/fantasyfootball/rankings/weekly");
        //    StreamReader reader = new StreamReader(data);
        //    string s = reader.ReadToEnd();
        //    data.Close();
        //    reader.Close();

        //    MatchCollection myAuthors = Regex.Matches(s, @"<td[^>]*?url\((?<Thumbnail>.*?/authors/.*?)\)[^>]+>\s*<a[^>]+>(?<Author>.*?)</a>.*?</td>", RegexOptions.Singleline);
        //    MatchCollection myTimeStamps = Regex.Matches(s, @"(?i)Updated:\s+(?<TimeStamp>.*?)</td>", RegexOptions.Singleline);
        //    MatchCollection myTableLists = Regex.Matches(s, @"<table[^>]+?class=""data"".*?</table>", RegexOptions.Singleline);

        //    List<RankingsPost> myAuthorsList = new List<RankingsPost>();

        //    //Build Authors
        //    if (myAuthors.Count > 0)
        //    {
        //        foreach (Match myMatch in myAuthors)
        //        {
        //            myAuthorsList.Add(new RankingsPost()
        //            {
        //                Author = myMatch.Groups["Author"].Value,
        //                Thumbnail = myMatch.Groups["Thumbnail"].Value
        //            });
        //        }
        //    }

        //    //Add timestamps
        //    if (myTimeStamps.Count > 0)
        //        for (int i = 0; i < myTimeStamps.Count; i++)
        //            myAuthorsList[i].TimeStamp = myTimeStamps[i].Groups["TimeStamp"].Value;

        //    //Build RankingsPosts
        //    if (myTableLists.Count > 0)
        //    {
        //        List<string> CbsIds = new List<string>();
        //        for (int i = 0; i < myTableLists.Count; i++)
        //        {
        //            //Pull the Player CBSIDs from the findings
        //            MatchCollection CbsIdResults = Regex.Matches(myTableLists[i].Value, @"(?i)playerpage/(?<CbsId>\d+)", RegexOptions.Singleline);
        //            if (CbsIdResults.Count > 0)
        //                foreach (Match myMatch in CbsIdResults)
        //                {
        //                    string myId = myMatch.Groups["CbsId"].Value;
        //                    if (!CbsIds.Contains(myId))
        //                        CbsIds.Add(myId);
        //                }
        //        }

        //        //Pull the player teams (since CBS doesn't :/)
        //        FantasyFootballEntities db = new FantasyFootballEntities();
        //        List<tbl_ff_players> PlayerTeams = db.tbl_ff_players.Where(x => CbsIds.Contains(x.Cbs)).ToList();

        //        for (int i = 0; i < myTableLists.Count; i++)
        //        {
        //            myAuthorsList[i].Rankings = new List<Ranking>();
        //            MatchCollection myPositions = Regex.Matches(myTableLists[i].Value, @"(?i)<tr[^>]+?class=""title""[^>]*?>\s*<td[^>]*>(?<Position>[^>]+)</td>\s*</tr>", RegexOptions.Singleline);
        //            if (myPositions.Count > 0)
        //                for (int j = 0; j < myPositions.Count; j++)
        //                {
        //                    string playerPosition = string.Empty;
        //                    switch (myPositions[j].Groups["Position"].Value)
        //                    {
        //                        case "Quarterbacks":
        //                            playerPosition = "QB";
        //                            break;
        //                        case "Running Backs":
        //                            playerPosition = "RB";
        //                            break;
        //                        case "Wide Receivers":
        //                            playerPosition = "WR";
        //                            break;
        //                        case "Tight Ends":
        //                            playerPosition = "TE";
        //                            break;
        //                        case "Kickers":
        //                            playerPosition = "K";
        //                            break;
        //                        case "Defensive Special Teams":
        //                            playerPosition = "DST";
        //                            break;
        //                    }

        //                    string playersBlock = string.Empty;
        //                    if (j == (myPositions.Count - 1))
        //                    {
        //                        playersBlock = myTableLists[i].Value.Substring(myPositions[j].Index, (myTableLists[i].Value.Length - myPositions[j].Index));
        //                    }
        //                    else
        //                    {
        //                        playersBlock = myTableLists[i].Value.Substring(myPositions[j].Index, (myPositions[j + 1].Index - myPositions[j].Index));
        //                    }
        //                    MatchCollection players = Regex.Matches(playersBlock, @"(?i)<td[^>]*>\s*<a[^>]+?playerpage/(?<CbsId>\d+)[^>]+>(?<Name>.*?)</a>\s*(?<Team>\b\w+\b).*?(?<Opponent>\b\w+\b)\)", RegexOptions.Singleline);

        //                    if (players.Count > 0)
        //                    {
        //                        for (int k = 0; k < players.Count; k++)
        //                        {
        //                            var myPlayerTeam = PlayerTeams.Where(x => x.Cbs == players[k].Groups["CbsId"].Value).SingleOrDefault();
        //                            myAuthorsList[i].Rankings.Add(new Ranking()
        //                            {
        //                                Name = players[k].Groups["Name"].Value,
        //                                Position = playerPosition,
        //                                Opponent = players[k].Groups["Opponent"].Value.Replace("@", string.Empty).ToLower(),
        //                                Rank = k + 1,
        //                                Team = players[k].Groups["Team"].Value.ToLower() //((myPlayerTeam != null) ? myPlayerTeam.Team.ToLower() : string.Empty)
        //                            });
        //                        }
        //                    }
        //                }
        //        }
        //    }
        //    List<RankingsPost> myFinalAuthors = new List<RankingsPost>();
        //    myFinalAuthors.Add(myAuthorsList[1]);
        //    myFinalAuthors.Add(myAuthorsList[0]);
        //    //myFinalAuthors.Add(myAuthorsList[2]);

            


        //    return View(myFinalAuthors);
        //}

    }
}
