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

	[Serializable]
	public class DateModel
	{
		public DateTime date;
	}

	[Serializable]
	public class EmojiCountModel
	{
		public ulong emojiID;
		public string emojiName;
		public int useCount;
	}

	[Serializable]
	public class EmojiUseModel
	{
		public ulong emojiID;
		public string emojiName;
		public ulong userID;
		public string userMention;
	}

	[Serializable]
	public class RoleCounts
	{
		public ulong roleID;
		public string roleMention;
		public int roleCount;
	}
}
