﻿using System;
using System.IO;
using Newtonsoft.Json;
using SGMessageBot.Helpers;

namespace SGMessageBot.Config
{
	public class DBConfig
	{
		public DBConfigInfo config { get; private set; }
		public string connectionString { get; private set; }

		public BaseResult loadDBConfig()
		{
			config = null;
			var result = new BaseResult();
			
			if (File.Exists("Data/DBConfig.json"))
			{
				try
				{
					config = JsonConvert.DeserializeObject<DBConfigInfo>(File.ReadAllText("Data/DBConfig.json"));
					if (config == null)
					{
						result.success = false;
						result.message = "FAIL_LOAD_DB_CONFIG";
					}
				}
				catch(Exception e)
				{
					ErrorLog.writeError(e);
					result.success = false;
					result.message = e.Message;
					return result;
				}
			}
			else
			{
				result.success = false;
				result.message = "FAIL_FIND_DB_CONFIG";
			}
			result.success = true;
			connectionString = $"Server={config.address};Port={config.port};Database={config.schemaName};Uid={config.userName};Pwd={config.password};Charset=utf8mb4";
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
