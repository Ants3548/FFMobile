using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyFootball.Models
{
    public class League
    {
        public int LeagueId { get; set; }
        public string TeamName { get; set; }
        public string LeagueName { get; set; }
        public int Season { get; set; }
        public int TeamId { get; set; }
    }
}