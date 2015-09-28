using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace FantasyFootball.Common
{
	public class Functions
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
	}
}
