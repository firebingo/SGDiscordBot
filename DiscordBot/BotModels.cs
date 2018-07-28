using System;

namespace SGMessageBot.DiscordBot
{
	[Serializable]
	public class UserCountModel
	{
		public ulong userID;
		public string userMention;
		public int messageCount;
	}

	[Serializable]
	public class UsersInServersModel
	{
		public ulong userID;
		public uint mesCount;
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

	[Serializable]
	public struct MessageTextModel
	{
		private readonly string _mesText;
		public string mesText => _mesText;

		public MessageTextModel(string t)
		{
			_mesText = t;
		}
	}

	public enum DateGroup
	{
		hour = 0,
		day = 1,
		week = 2,
		month = 3,
		year = 4
	}

	public enum StatType
	{
		userCount = 0,
		uniqueUsers = 1
	}

	[Serializable]
	public class StatModel
	{
		public ulong serverID;
		public StatType statType;
		public DateTime statTime;
		public long statValue;
		public string statText;
		public DateGroup dateGroup;
	}
}
