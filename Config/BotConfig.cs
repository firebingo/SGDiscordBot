using Newtonsoft.Json;
using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.IO;

namespace SGMessageBot.Config
{
	public class BotConfig
	{
		private readonly string ConfigPath = "Data/BotConfig.json";
		private readonly string OldCredConfigPath = "Data/CredConfig.json";
		private readonly object locker = new object();
		public MainBotConfig BotInfo { get; private set; }

		public BaseResult LoadConfig()
		{
			var result = new BaseResult();
			lock(locker)
			{
				if (File.Exists(OldCredConfigPath))
				{
					try
					{
						var oldConfig = LoadOldDiscordConfig();
						File.Move(OldCredConfigPath, "Data/CredConfig.bak.json");
						if(oldConfig != null)
						{
							BotInfo = new MainBotConfig();
							SaveCredConfig(oldConfig);
						}
					}
					catch(Exception ex)
					{
						ErrorLog.WriteError(ex);
						return new BaseResult() { Success = false, Message = ex.Message };
					}
				}
				else
				{
					if (File.Exists(ConfigPath))
					{
						var config = DeserializeConfig(ref result);
						if (config == null || !result.Success)
							return result;
						BotInfo = config;
					}
					else
						SaveCredConfig();
				}
			}
			result.Success = true;
			result.Message = string.Empty;
			return result;
		}

		private MainBotConfig DeserializeConfig(ref BaseResult result)
		{
			MainBotConfig retval = null;
			try
			{
				retval = JsonConvert.DeserializeObject<MainBotConfig>(File.ReadAllText(ConfigPath));
				result.Success = true;
				result.Message = string.Empty;
			}
			catch (Exception ex)
			{
				result.Success = false;
				result.Message = ex.Message;
				ErrorLog.WriteError(ex);
				return null;
			}

			return retval;
		}

		private DiscordConfig LoadOldDiscordConfig()
		{
			lock (locker)
			{
				DiscordConfig OldInfo = null;

				try
				{
					OldInfo = JsonConvert.DeserializeObject<DiscordConfig>(File.ReadAllText(OldCredConfigPath));
				}
				catch (Exception ex)
				{
					ErrorLog.WriteError(ex);
					return null;
				}

				return OldInfo;
			}
		}

		public BaseResult SaveCredConfig(DiscordConfig discordConfig = null)
		{
			lock (locker)
			{
				var result = new BaseResult();
				try
				{
					if (BotInfo == null)
						BotInfo = new MainBotConfig();
					if (discordConfig != null)
						BotInfo.DiscordConfig = discordConfig;
					File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(BotInfo, Formatting.Indented));
				}
				catch (Exception ex)
				{
					result.Success = false;
					result.Message = "FAIL_SAVE_CRED";
					ErrorLog.WriteError(ex);
				}
				result.Success = true;
				return result;
			}
		}
	}

	[Serializable]
	public class MainBotConfig
	{
		public bool DiscordEnabled = true;
		public bool SteamEnabled = false;
		public DiscordConfig DiscordConfig = new DiscordConfig();
		public SteamConfig SteamConfig = new SteamConfig();
		public List<DebugLogTypes> debugLogIds = new List<DebugLogTypes>();
	}

	//Don't rename/recase these variables since they have already created configs with these names
	[Serializable]
	public class DiscordConfig
	{
		public string token = string.Empty;
		public string botId = string.Empty;
		public string aiCorpusExtraPath = string.Empty;
		public List<ulong> ownerIds = new List<ulong>();
		public List<ulong> commandRoleIds = new List<ulong>();
		public List<ulong> ignoreCommandsFrom = new List<ulong>();
		public bool ignoreOtherBots = true;
		public bool escapeMentionsChat = true;
		public Dictionary<ulong, MessageCountTracker> messageCount = new Dictionary<ulong, MessageCountTracker>();
		public Dictionary<string, RandomMessageInfo> randomMessageSend = new Dictionary<string, RandomMessageInfo>();
		public List<ulong> statServerIds = new List<ulong>();
	}

	[Serializable]
	public class MessageCountTracker
	{
		public bool enabled = false;
		public ulong channelId = 0;
		public int messageCount = 0;
		public string message = string.Empty;
	}

	[Serializable]
	public class RandomMessageInfo
	{
		public string key = "test";
		public ulong serverId = 0;
		public ulong channelId = 0;
		public double maxSeconds = 7200.0; //2hours
		public List<string> messagesToPick = new List<string>();
	}

	[Serializable]
	public class SteamConfig
	{
		public string Username = string.Empty;
		public string Password = string.Empty;
		public string AuthCode = string.Empty;
		public string TwoFactorCode = string.Empty;
		public string SentryFileLocation = string.Empty;
		public List<SteamGroupToUse> Groups = new List<SteamGroupToUse>();
	}

	[Serializable]
	public class SteamGroupToUse
	{
		public ulong GroupId = 0;
		public ulong ChatId = 0;
	}
}
