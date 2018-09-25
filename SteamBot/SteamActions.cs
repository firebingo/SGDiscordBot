using SGMessageBot.Config;
using SGMessageBot.Helpers;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot.SteamBot
{
	class SteamActions
	{
		private static readonly string steamCommunityHost = "steamcommunity.com";
		private static readonly string steamCommunityURL = $"https://{steamCommunityHost}";

		private IServiceProvider dependencyMap = null;

		public void InstallServiceMap(IServiceProvider _map)
		{
			dependencyMap = _map;
		}

		public Task HandleMessageReceived(SteamFriends.ChatMsgCallback callback)
		{
			switch(callback.ChatMsgType)
			{
				case EChatEntryType.ChatMsg:

					break;
			}
			return Task.CompletedTask;
		}

		public Task GetGroups()
		{
			var config = dependencyMap.GetService(typeof(BotConfig)) as BotConfig;
			var steamFriends = dependencyMap.GetService(typeof(SteamFriends)) as SteamFriends;
			var groups = new List<SteamID>();
			for(var i =0; i < steamFriends.GetClanCount(); ++i)
			{
				groups.Add(steamFriends.GetClanByIndex(i));
			}
			return Task.CompletedTask;
		}

		public Task ConnectToGroupChats()
		{
			try
			{
				var config = dependencyMap.GetService(typeof(BotConfig)) as BotConfig;
				var steamFriends = dependencyMap.GetService(typeof(SteamFriends)) as SteamFriends;
				foreach(var group in config.BotInfo.SteamConfig.Groups)
				{
					steamFriends.JoinChat(group.ChatId);
				}
			}
			catch(Exception ex)
			{
				ErrorLog.WriteError(ex);
			}
			return Task.CompletedTask;
		}
	}
}
