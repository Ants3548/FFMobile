using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

using FantasyFootball.Common;

namespace FantasyFootball.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            Functions.CheckForSession();
            
            //return RedirectToAction("Weekly", "Rankings");

            return View();

        }

        [HttpPost]
        public JsonResult JavaScript(int dipWidth, int dipHeight, int physWidth, int physHeight, decimal pxRatio)
        {
            Session["dipWidth"] = ((dipWidth < dipHeight) ? dipWidth : dipHeight);
            Session["dipHeight"] = ((dipWidth < dipHeight) ? dipHeight : dipWidth);
            Session["physWidth"] = ((physWidth < physHeight) ? physWidth : physHeight);
            Session["physHeight"] = ((physWidth < physHeight) ? physHeight : physWidth);
            Session["pxRatio"] = pxRatio;
            return Json(new { dipWidth = Session["dipWidth"] });
        }
    }
}
