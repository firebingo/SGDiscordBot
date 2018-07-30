using System;
using System.IO;
using Newtonsoft.Json;
using SGMessageBot.Helpers;

namespace SGMessageBot.Config
{
	public class DBConfig
	{
		public DBConfigInfo Config { get; private set; }
		public string ConnectionString { get; private set; }

		public BaseResult LoadDBConfig()
		{
			Config = null;
			var result = new BaseResult();
			
			if (File.Exists("Data/DBConfig.json"))
			{
				try
				{
					Config = JsonConvert.DeserializeObject<DBConfigInfo>(File.ReadAllText("Data/DBConfig.json"));
					if (Config == null)
					{
						result.Success = false;
						result.Message = "FAIL_LOAD_DB_CONFIG";
					}
				}
				catch(Exception ex)
				{
					ErrorLog.WriteError(ex);
					result.Success = false;
					result.Message = ex.Message;
					return result;
				}
			}
			else
			{
				result.Success = false;
				result.Message = "FAIL_FIND_DB_CONFIG";
			}
			result.Success = true;
			ConnectionString = $"Server={Config.address};Port={Config.port};Database={Config.schemaName};Uid={Config.userName};Pwd={Config.password};Charset=utf8mb4";
			return result;
		}
	}

	[Serializable]
	public class DBConfigInfo
	{
		public string userName;
		public string password;
		public string address;
		public string port;
		public string schemaName;
	}
}
