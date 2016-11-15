using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	[Serializable]
	public class UserCountModel
	{
		public ulong userID;
		public string userMention;
		public int messageCount;
	}
}
