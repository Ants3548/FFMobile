using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyFootball.Models
{
	public class Transaction
	{
		public string OwnerName { get; set; }
		public string Date { get; set; }
		public List<Player> Players { get; set; }
	}
}