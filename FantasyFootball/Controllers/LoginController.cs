﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

using FantasyFootball.Common;

namespace FantasyFootball.Controllers
{
	public class LoginController : Controller
	{
		//
		// GET: /Login/

		public ActionResult Index()
		{
			Functions.CheckForSession();

			return View();
		}

		[HttpGet]
		public ActionResult Espn()
		{
			return View();
		}

		[HttpPost]
		public ActionResult EspnPost()
		{
			string jsonPost = @"{""loginValue"":""" + Request.Form["username"] + @""",""password"":""" + Request.Form["password"] + @"""}";
			byte[] buffer = Encoding.ASCII.GetBytes(jsonPost.ToString());

			HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create("https://registerdisney.go.com/jgc/v2/client/ESPN-FANTASYLM-PROD/guest/login?langPref=en-US");
			WebReq.Accept = "application/json, text/plain, */*";
			WebReq.ContentLength = buffer.Length;
			WebReq.ContentType = "application/json;charset=UTF-8";
			WebReq.Host = "registerdisney.go.com";			
			WebReq.KeepAlive = true;
			WebReq.Method = "POST";
			WebReq.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.93 Safari/537.36";

			Stream PostData = WebReq.GetRequestStream();
			PostData.Write(buffer, 0, buffer.Length);
			PostData.Close();

			HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();
			if (WebResp.StatusCode == HttpStatusCode.OK)
			{
				Stream jsonResp = WebResp.GetResponseStream();
				StreamReader _Answer = new StreamReader(jsonResp);
				string htmlResponse = _Answer.ReadToEnd();

				Match mySwid = Regex.Match(htmlResponse, @"(?i)""swid""\:""\{(?<Swid>[^}]+)\}""", RegexOptions.Singleline);
				if (mySwid.Success)
				{
					Session["espn"] = @"SWID={" + mySwid.Groups["Swid"].Value + @"}; espnAuth={""swid"":""{" + mySwid.Groups["Swid"].Value + @"}""}";
					return RedirectToAction("Index", "Espn"); //("~/Views/Home/Index.cshtml");
				}
				else
				{
					return RedirectToAction("Espn", "Login");
				}
			}
			else
			{
				return RedirectToAction("Espn", "Login");
			}

		}

		[HttpGet]
		public ActionResult Yahoo()
		{
			return View();
		}

		[HttpPost]
		public ActionResult YahooPost()
		{
			//Grab parameters that the Yahoo! login page will be looking for in addition to username & password
			string html, myCookieString = string.Empty; //= Functions.GetHttpHtml("https://login.yahoo.com/config/login", string.Empty);
			using (WebClient client = new WebClient())
			{
				client.Headers["User-Agent"] = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
				html = client.DownloadString("https://login.yahoo.com/config/login");
				//Hold onto the authentication cookie
				if (!string.IsNullOrEmpty(client.ResponseHeaders["Set-Cookie"]))
				{
					MatchCollection myCookies = Regex.Matches(client.ResponseHeaders["Set-Cookie"], @"(?i)\b[^=]+\b=[^;]*?;", RegexOptions.Singleline);					
					foreach (Match myMatch in myCookies)
						if (!Regex.IsMatch(myMatch.Value, "(?i)^(domain|expires|path)"))
							myCookieString += myMatch.Value;
				}
			}

			//return View();

			MatchCollection inputs = Regex.Matches(html, @"(?i)<input.*?name=""(?<ParamName>[^""]+)"".*?value=""(?<ParamValue>[^""]+)""[^>]*>", RegexOptions.Singleline);
			if (inputs.Count > 0)
			{
				string _crumb = string.Empty, _ts = string.Empty, _format = string.Empty, _uuid = string.Empty, _seqid = string.Empty, otp_channel = string.Empty;
				foreach (Match input in inputs)
				{
					switch (input.Groups["ParamName"].Value.ToLower())
					{
						case "_crumb":
							_crumb = input.Groups["ParamValue"].Value;
							break;
						case "_ts":
							_ts = input.Groups["ParamValue"].Value;
							break;
						case "_format":
							_format = input.Groups["ParamValue"].Value;
							break;
						case "_uuid":
							_uuid = input.Groups["ParamValue"].Value;
							break;
						case "_seqid":
							_seqid = input.Groups["ParamValue"].Value;
							break;
						case "otp_channel":
							otp_channel = input.Groups["ParamValue"].Value;
							break;
					}
				}

				StringBuilder PostVars = new StringBuilder();
				PostVars.Append("countrycode=1");
				PostVars.Append("&username=" + Request.Form["username"]);
				PostVars.Append("&passwd=" + Request.Form["password"]);
				PostVars.Append("&.persistent=y");
				PostVars.Append("&signin=");
				PostVars.Append("&_crumb=" + _crumb);
				PostVars.Append("&_ts=" + _ts);
				PostVars.Append("&_format=json");
				PostVars.Append("&_uuid=" + _uuid);
				PostVars.Append("&_seqid=" + _seqid);
				PostVars.Append("&otp_channel=" + otp_channel);
				PostVars.Append("&loadtpl=1");

				byte[] buffer = Encoding.ASCII.GetBytes(PostVars.ToString());
				//Initialization, we use localhost, change if applicable
				HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create("https://login.yahoo.com/config/login");
				WebReq.Method = "POST";
				WebReq.Headers.Add("Cookie", myCookieString);
				WebReq.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				WebReq.ContentLength = buffer.Length;
				WebReq.Host = "login.yahoo.com";
				WebReq.KeepAlive = true;
                WebReq.Headers.Add("Origin", "https://login.yahoo.com");
				WebReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
				WebReq.Referer = "https://login.yahoo.com/config/login";
				WebReq.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.85 Safari/537.36";
				//We open a stream for writing the postvars
				Stream PostData = WebReq.GetRequestStream();
				PostData.Write(buffer, 0, buffer.Length);
				PostData.Close();
				//Get the response handle, we have no true response yet!
				HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();
				//Let's show some information about the response
				//Console.WriteLine(WebResp.StatusCode);
				//Console.WriteLine(WebResp.Server);

				//Now, we read the response (the string), and output it.
				if (WebResp.StatusCode == HttpStatusCode.OK)
				{
					if (!string.IsNullOrEmpty(WebResp.Headers["Set-Cookie"]))
					{
						string cleanCookies = Regex.Replace(WebResp.Headers["Set-Cookie"], @",", @"; ", RegexOptions.Singleline);
                        MatchCollection myCookies = Regex.Matches(cleanCookies, @"(?i)(?<ParamName>[^\s=]+)=[^;]+;", RegexOptions.Singleline);						
						foreach (Match myMatch in myCookies)
							if (!Regex.IsMatch(myMatch.Groups["ParamName"].Value.ToLower(), "(?i)^(domain|expires|path)"))
								myCookieString += myMatch.Value;

						Session["yahoo"] = myCookieString;

						Stream Answer = WebResp.GetResponseStream();
						StreamReader _Answer = new StreamReader(Answer);
						string htmlResponse = _Answer.ReadToEnd();
						//Console.WriteLine(_Answer.ReadToEnd());

						return RedirectToAction("Index", "Yahoo");
					}

					
				}

				return View();
			}
			else
			{
				return View();
			}

		}

	}
}
