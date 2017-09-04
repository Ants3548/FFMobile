using System.Collections.Generic;

namespace FantasyFootball.Classes
{
    public class Transaction
	{
		public string OwnerName { get; set; }
		public string Date { get; set; }
		public List<Player> Players { get; set; }
	}
}