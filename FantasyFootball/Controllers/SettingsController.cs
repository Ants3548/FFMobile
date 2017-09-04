using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using FantasyFootball.Classes;
using FantasyFootball.DAL;
using FantasyFootball.Common;

namespace FantasyFootball.Controllers
{
    public class SettingsController : Controller
    {
        //
        // GET: /Settings/

        public ActionResult Index()
        {
          Functions.CheckForSession();

            return View();
        }

        public ActionResult ChooseLeague()
        {
          Functions.CheckForSession();

          return View();
        }

    }
}
