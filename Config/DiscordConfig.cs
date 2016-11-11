using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot.Config
{
	public class BotConfig
	{
		public BotCredentialInfo credInfo { get; private set; }

		public BaseResult loadCredConfig()
		{
			credInfo = null;
			var result = new BaseResult();

			if (File.Exists("Data/DBConfig.json"))
			{
				try
				{
					credInfo = JsonConvert.DeserializeObject<BotCredentialInfo>(File.ReadAllText("Data/CredConfig.json"));
					if (credInfo == null)
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
	}

	[Serializable]
	public class BotCredentialInfo
	{
		public string token;
		public string clientId;
		public string botId;
		public List<string> ownerIds;
	}
}
