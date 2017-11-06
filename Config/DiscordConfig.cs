using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace SGMessageBot.Config
{
	public class BotConfig
	{
		public BotCredentialInfo botInfo { get; private set; }

		public BaseResult loadCredConfig()
		{
			botInfo = null;
			var result = new BaseResult();

			if (File.Exists("Data/CredConfig.json"))
			{
				try
				{
					botInfo = JsonConvert.DeserializeObject<BotCredentialInfo>(File.ReadAllText("Data/CredConfig.json"));
					if (botInfo == null)
					{
						result.success = false;
						result.message = "FAIL_LOAD_CRED";
					}
				}
				catch (Exception e)
				{
					result.success = false;
					result.message = e.Message;
					return result;
				}
			}
			else
			{
				result.success = false;
				result.message = "FAIL_FIND_CRED";
			}
			result.success = true;
			return result;
		}

		public BaseResult saveCredConfig()
		{
			var result = new BaseResult();
			try
			{
				File.WriteAllText("Data/CredConfig.json", JsonConvert.SerializeObject(botInfo, Formatting.Indented));
			}
			catch (Exception e)
			{
				result.success = false;
				result.message = "FAIL_SAVE_CRED";
			}
			result.success = true;
			return result;
		}
	}

	[Serializable]
	public class BotCredentialInfo
	{
		public string token;
		public string clientId;
		public string botId;
		public List<ulong> ownerIds;
		public List<ulong> commandRoleIds;
		public Dictionary<ulong, MessageCountTracker> messageCount;
	}

	[Serializable]
	public class MessageCountTracker
	{
		public bool enabled;
		public ulong channelId;
		public int messageCount;
		public string message;
	}
}
