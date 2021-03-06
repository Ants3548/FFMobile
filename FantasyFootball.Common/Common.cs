﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace FantasyFootball.Common
{
	public partial class Functions
	{
		public static void CheckForSession()
		{
			if (
				(HttpContext.Current.Session["dipWidth"] == null) ||
				(HttpContext.Current.Session["dipHeight"] == null) ||
				(HttpContext.Current.Session["physWidth"] == null) ||
				(HttpContext.Current.Session["physHeight"] == null) ||
				(HttpContext.Current.Session["pxRatio"] == null)
				)
			{
				//HttpContext.Current.Response.Redirect("/jsDetect.html");
			}
		}

		public static String GetHttpHtml(String Url, String CookieHeader)
		{
			HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(Url);
			WebReq.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
			WebReq.Method = "GET";
			if (!String.IsNullOrEmpty(CookieHeader))
				WebReq.Headers.Add("Cookie", CookieHeader);

			HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();
			Stream data = WebResp.GetResponseStream();
			StreamReader reader = new StreamReader(data);
			string html = reader.ReadToEnd();

			data.Close();
			reader.Close();

			return html;
		}

		public static String StripHtmlTags(String html)
		{
			return Regex.Replace(html, @"<[^>]+>", string.Empty, RegexOptions.Singleline);
		}

		public static String RankSuffix(int rank)
		{
			switch (rank)
			{
				case 1:
				case 21:
				case 31:
					return string.Format("{0}st", rank);
					break;
				case 2:
				case 22:
				case 32:
					return string.Format("{0}nd", rank);
					break;
				case 3:
				case 23:
					return string.Format("{0}rd", rank);
					break;
				default:
					return string.Format("{0}th", rank);
					break;
			}		
		}

		public static ApplicationWeeklyStats GetWeeklyStats(string website, string leagueId)
		{
			List<ApplicationWeeklyStats> applicationWeeklyStats = HttpContext.Current.Application["WeeklyStats"] as List<ApplicationWeeklyStats> ?? new List<ApplicationWeeklyStats>();
			ApplicationWeeklyStats myStats = applicationWeeklyStats.Where(w => w.Website == website && w.LeagueId == leagueId).SingleOrDefault();

			return myStats;
        }

		public static void SetWeeklyStats(ApplicationWeeklyStats stats)
		{
			List<ApplicationWeeklyStats> applicationWeeklyStats = HttpContext.Current.Application["WeeklyStats"] as List<ApplicationWeeklyStats> ?? new List<ApplicationWeeklyStats>();
			ApplicationWeeklyStats myStats = applicationWeeklyStats.Where(w => w.Website == stats.Website && w.LeagueId == stats.LeagueId).SingleOrDefault();
			if (myStats != null)
			{
				applicationWeeklyStats.Remove(myStats);
			}
			applicationWeeklyStats.Add(stats);

			HttpContext.Current.Application["WeeklyStats"] = applicationWeeklyStats;
        }
	}
}
