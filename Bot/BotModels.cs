﻿using System;
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
	public class EmojiMessageModel
	{
		public ulong userID;
		public ulong messageID;
		public string mention;
		public string mesText;
		public int useCount;
		public int userUseCount;
		public ulong emojiID;
	}

	[Serializable]
	public class RoleCounts
	{
		public ulong roleID;
		public string roleMention;
		public int roleCount;
	}
}
