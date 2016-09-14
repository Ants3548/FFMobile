using FantasyFootball.DAL;
using FantasyFootball.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace FantasyFootball
{
	public partial class Augment
	{
		public static void UpdateOpponents(ref List<RankingsPost> myWriters)
		{
			FantasyFootballEntities db = new FantasyFootballEntities();
			tbl_ff_weeks myWeek = db.tbl_ff_weeks.Where(w => w.StartDate <= DateTime.Now && w.EndDate >= DateTime.Now).SingleOrDefault();
			if (myWeek != null)
			{
				List<tbl_ff_matchups> matches = db.tbl_ff_matchups.Where(mch => mch.Week == myWeek.Id).ToList();
				//Correct Jacksonville abbrev
				foreach (tbl_ff_matchups matchrow in matches)
				{
					switch (matchrow.HomeTeam)
					{
						case "JAX":
							matchrow.HomeTeam = "JAC";
							break;
						case "LA":
							matchrow.HomeTeam = "LAR";
							break;
					}
					switch (matchrow.AwayTeam)
					{
						case "JAX":
							matchrow.AwayTeam = "JAC";
							break;
						case "LA":
							matchrow.AwayTeam = "LAR";
							break;
					}
				}

				foreach(RankingsPost myWriter in myWriters)
				{
					//Search Rankings
					foreach(KeyValuePair<string, List<Ranking>> myPosRankings in myWriter.MultiPartRankings)
					{
						foreach (Ranking myRanking in myPosRankings.Value)
						{
							tbl_ff_matchups myMatchUp = matches.Where(m => m.HomeTeam == myRanking.Team.ToUpper() || m.AwayTeam == myRanking.Team.ToUpper()).FirstOrDefault();
							bool isHomeTeam = myMatchUp.HomeTeam == myRanking.Team.ToUpper();

							myRanking.Opponent = (isHomeTeam ? myMatchUp.AwayTeam.ToLower() : myMatchUp.HomeTeam.ToLower());
							myRanking.IsHomeTeam = isHomeTeam;
						}
					}					
				}				
			}
		}
		public static void UpdateOpponents(ref List<Player> myPlayers)
		{
			FantasyFootballEntities db = new FantasyFootballEntities();
			tbl_ff_weeks myWeek = db.tbl_ff_weeks.Where(w => w.StartDate <= DateTime.Now && w.EndDate >= DateTime.Now).SingleOrDefault();
			if (myWeek != null)
			{
				List<tbl_ff_matchups> matches = db.tbl_ff_matchups.Where(mch => mch.Week == myWeek.Id).ToList();
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

				foreach(Player myPlayer in myPlayers)
				{
					tbl_ff_matchups myMatchUp = matches.Where(m => m.HomeTeam == myPlayer.Team.ToUpper() || m.AwayTeam == myPlayer.Team.ToUpper()).FirstOrDefault();
					bool isHomeTeam = myMatchUp.HomeTeam == myPlayer.Team.ToUpper();
					myPlayer.Opponent = (isHomeTeam ? myMatchUp.AwayTeam.ToLower() : myMatchUp.HomeTeam.ToLower());
                }
			}
		}
	}
}