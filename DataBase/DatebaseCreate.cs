using MySql.Data.MySqlClient;
using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.Data;

namespace SGMessageBot.DataBase
{
	public class DatebaseCreate
	{
		private List<string> createQueries;
		private Dictionary<int, List<string>> buildQueries;
		private const int mkey = 345; //this is just so metadata can be updated.

		public DatebaseCreate()
		{
			createQueries = new List<string>();
			createQueries.Add($"CREATE DATABASE {DataLayerShortcut.DBConfig.config.schemaName}");
			createQueries.Add($"ALTER DATABASE {DataLayerShortcut.DBConfig.config.schemaName} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
			createQueries.Add("CREATE TABLE metaData (mkey INT, version INT UNSIGNED, createdDate DATETIME, updatedDate DATETIME, PRIMARY KEY(mkey))");
			buildQueries = new Dictionary<int, List<string>>();
			buildQueries.Add(1, new List<string>());
			buildQueries[1].Add("CREATE TABLE servers (serverID BIGINT UNSIGNED, ownerID BIGINT UNSIGNED, serverName VARCHAR(256), userCount INT UNSIGNED, channelCount SMALLINT UNSIGNED, roleCount SMALLINT UNSIGNED, regionID varChar(64), createdDate DATETIME, isDeleted BOOL DEFAULT FALSE, PRIMARY KEY (serverID))");
			buildQueries[1].Add("CREATE TABLE roles (serverID BIGINT UNSIGNED, roleID BIGINT UNSIGNED, roleName VARCHAR(64), roleColor VARCHAR(10), roleMention VARCHAR(64), isEveryone BOOL, isDeleted BOOL DEFAULT FALSE, CONSTRAINT kf_roleServer FOREIGN KEY (serverID) REFERENCES servers(serverID), PRIMARY KEY (roleID))");
			buildQueries[1].Add(@"CREATE TABLE channels (serverID BIGINT UNSIGNED, channelID BIGINT UNSIGNED, channelMention VARCHAR(64), channelName VARCHAR(128), channelPosition SMALLINT UNSIGNED, channelType varChar(64), isDeleted BOOL DEFAULT FALSE, 
			CONSTRAINT kf_channelServer FOREIGN KEY (serverID) REFERENCES servers(serverID), PRIMARY KEY (channelID))");
			buildQueries[1].Add("CREATE TABLE emojis (serverID BIGINT UNSIGNED, emojiID BIGINT UNSIGNED, emojiName CHAR(32), isManaged BOOL, colonsRequired BOOL, isDeleted BOOL DEFAULT FALSE, CONSTRAINT kf_emojiServer FOREIGN KEY (serverID) REFERENCES servers(serverID), PRIMARY KEY (emojiID))");
			buildQueries[1].Add("CREATE TABLE users (userID BIGINT UNSIGNED, userName VARCHAR(128), mention VARCHAR(64), isBot BOOL, isDeleted BOOL DEFAULT FALSE, PRIMARY KEY (userID))");
			buildQueries[1].Add(@"CREATE TABLE usersInServers (serverID BIGINT UNSIGNED, userID BIGINT UNSIGNED, discriminator SMALLINT UNSIGNED, nickName VARCHAR(128), nickNameMention VARCHAR(64), joinedDate DATETIME, avatarID VARCHAR(128), avatarUrl TEXT, 
			lastOnline DATETIME, isBanned BOOL DEFAULT FALSE, isDeleted BOOL DEFAULT FALSE, CONSTRAINT kf_userTableID FOREIGN KEY (userID) REFERENCES users(userID), CONSTRAINT kf_userServerID FOREIGN KEY (serverID) REFERENCES servers(serverID), UNIQUE KEY (serverID, userID))");
			buildQueries[1].Add(@"CREATE TABLE messages (serverID BIGINT UNSIGNED, userID BIGINT UNSIGNED, channelID BIGINT UNSIGNED, messageID BIGINT UNSIGNED, rawText MEDIUMTEXT, mesText MEDIUMTEXT, mesStatus TINYINT UNSIGNED, mesTime DATETIME, mesEditedTime DATETIME, editedRawText MEDIUMTEXT, editedMesText MEDIUMTEXT, isDeleted BOOL DEFAULT FALSE,
			CONSTRAINT kf_mesUserID FOREIGN KEY (userID) REFERENCES users(userID), CONSTRAINT kf_mesServerID FOREIGN KEY (serverID) REFERENCES servers(serverID), CONSTRAINT kf_mesChanID FOREIGN KEY (channelID) REFERENCES channels(channelID), PRIMARY KEY (messageID))");
			buildQueries[1].Add("CREATE TABLE attachments (messageID BIGINT UNSIGNED, attachID VARCHAR(64), fileName VARCHAR(256), height SMALLINT UNSIGNED, width SMALLINT UNSIGNED, proxyURL TEXT, attachURL TEXT, attachSize BIGINT UNSIGNED, isDeleted BOOL DEFAULT FALSE, PRIMARY KEY (attachID))");
			buildQueries.Add(2, new List<string>());
			buildQueries[2].Add(@"CREATE TABLE reactions (serverID BIGINT UNSIGNED, userID BIGINT UNSIGNED, channelID BIGINT UNSIGNED, messageID BIGINT UNSIGNED, emojiID BIGINT UNSIGNED, emojiName CHAR(32), isDeleted BOOL DEFAULT FALSE, CONSTRAINT kf_reaServerID FOREIGN KEY (serverID) REFERENCES servers(serverID), 
			CONSTRAINT kf_reaUserID FOREIGN KEY (userID) REFERENCES users(userID), CONSTRAINT kf_reaChanID FOREIGN KEY (channelID) REFERENCES channels(channelID), CONSTRAINT kf_reaMesID FOREIGN KEY (messageID) REFERENCES messages(messageID))");
			buildQueries.Add(3, new List<string>());
			buildQueries[3].Add(@"CREATE TABLE emojiUses (serverID BIGINT UNSIGNED, userID BIGINT UNSIGNED, channelID BIGINT UNSIGNED, messageID BIGINT UNSIGNED, emojiID BIGINT UNSIGNED, emojiName CHAR(32), isDeleted BOOL DEFAULT FALSE,
			CONSTRAINT kf_emoServerID FOREIGN KEY (serverID) REFERENCES servers(serverID), CONSTRAINT kf_emoUserID FOREIGN KEY (userID) REFERENCES users(userID), CONSTRAINT kf_emoChanID FOREIGN KEY (channelID) REFERENCES channels(channelID), 
			CONSTRAINT kf_emoMesID FOREIGN KEY (messageID) REFERENCES messages(messageID))");
			buildQueries.Add(4, new List<string>());
			buildQueries[4].Add("ALTER TABLE usersInServers ADD roleIDs json");
			buildQueries.Add(5, new List<string>());
			buildQueries[5].Add("ALTER TABLE messages MODIFY userID BIGINT UNSIGNED NOT NULL");
			buildQueries[5].Add("ALTER TABLE messages MODIFY serverID BIGINT UNSIGNED NOT NULL");
			buildQueries[5].Add("ALTER TABLE usersInServers MODIFY userID BIGINT UNSIGNED NOT NULL");
			buildQueries[5].Add("ALTER TABLE usersInServers MODIFY serverID BIGINT UNSIGNED NOT NULL");
			buildQueries[5].Add("ALTER TABLE usersInServers ADD PRIMARY KEY(userID, serverID)");
			buildQueries.Add(6, new List<string>());
			buildQueries[6].Add("ALTER TABLE usersInServers ADD COLUMN mesCount INT UNSIGNED NOT NULL DEFAULT 0");
			buildQueries.Add(7, new List<string>());
			buildQueries[7].Add("ALTER TABLE emojis ADD COLUMN isAnimated BOOL DEFAULT FALSE");
			buildQueries.Add(8, new List<string>());
			buildQueries[8].Add("CREATE TABLE stats (serverID BIGINT UNSIGNED, statType INT UNSIGNED, statTime DATETIME, statValue BIGINT, statText TEXT, CONSTRAINT kf_statServerID FOREIGN KEY (serverID) REFERENCES servers(serverID))");
			buildQueries[8].Add("CREATE INDEX idx_statType ON stats (statType)");
		}

		public BaseResult createDatabase()
		{
			var result = new BaseResult();
			try
			{
				foreach (var query in createQueries)
				{
					DataLayerShortcut.ExecuteSpecialNonQuery(query, $"server={DataLayerShortcut.DBConfig.config.address};uid={DataLayerShortcut.DBConfig.config.userName};pwd={DataLayerShortcut.DBConfig.config.password};charset=utf8mb4");
				}
				var metaData = "INSERT INTO metaData (mkey, version, createdDate, updatedDate) VALUES (@mkey, @version, @createdDate, @updatedDate)";
				DataLayerShortcut.ExecuteSpecialNonQuery(metaData, $"server={DataLayerShortcut.DBConfig.config.address};uid={DataLayerShortcut.DBConfig.config.userName};pwd={DataLayerShortcut.DBConfig.config.password};charset=utf8mb4",
					new MySqlParameter("@mkey", mkey), new MySqlParameter("@version", 0), new MySqlParameter("@createdDate", DateTime.UtcNow), new MySqlParameter("@updatedDate", DateTime.UtcNow));
			}
			catch (MySqlException e)
			{
				ErrorLog.writeError(e);
				result.message = e.Message;
				result.success = false;
				return result;
			}
			result.success = true;
			return result;
		}

		public BaseResult buildDatabase()
		{
			var result = new BaseResult();
			var metaDataGet = getMetaData();
			//this hopefully should only happen if a db was made on version 1 before the metaData table.
			if(!metaDataGet.success)
			{
				Console.WriteLine("Database could not get metadata, this probally means the database was created before metadata was added. \nVersion will be set to 1 then new >version 1 tables will be created.\n" + 
					"If you were somehow above version 1 type n to exit and backup your database before this is run.");
				var read = Console.ReadLine();
				read = read.ToLower();
				if(read == "n")
					Environment.Exit(0);

				var metaQuery = "CREATE TABLE metaData (mkey INT, version INT UNSIGNED, createdDate DATETIME, updatedDate DATETIME, PRIMARY KEY(mkey))";
				DataLayerShortcut.ExecuteNonQuery(metaQuery);
				metaQuery = "INSERT INTO metaData (mkey, version, createdDate, updatedDate) VALUES (@mkey, @version, @createdDate, @updatedDate)";
				DataLayerShortcut.ExecuteNonQuery(metaQuery, new MySqlParameter("@mkey", mkey), new MySqlParameter("@version", 1), new MySqlParameter("@createdDate", DateTime.UtcNow), new MySqlParameter("@updatedDate", DateTime.UtcNow));
				metaDataGet = getMetaData();
				if(!metaDataGet.success)
				{
					Console.WriteLine("Getting metaData still failed, press any key to exit.");
					Console.Read();
					Environment.Exit(0);
				}
			}
			try
			{
				if (metaDataGet.metaData.version < 0)
				{
					result.message = "Failure to build database. Metadata reported a version below 0.";
					result.success = false;
					return result;
				}
				else
				{
					var cVersion = metaDataGet.metaData.version;
					var versionsToDo = new List<int>();
					while (cVersion < buildQueries.Count)
					{
						cVersion++;
						versionsToDo.Add(cVersion);
					};

					foreach (var v in versionsToDo)
					{
						if(buildQueries.ContainsKey(v))
						{
							foreach(var query in buildQueries[v])
							{
								DataLayerShortcut.ExecuteNonQuery(query);
							}
						}
						var metaDataUpdate = "UPDATE metaData SET version=@version, updatedDate=@updatedDate WHERE mkey=@mkey";
						DataLayerShortcut.ExecuteNonQuery(metaDataUpdate, new MySqlParameter("@version", v), new MySqlParameter("@updatedDate", DateTime.UtcNow), new MySqlParameter("@mkey", mkey));
					}
				}
			}
			catch (MySqlException e)
			{
				ErrorLog.writeError(e);
				result.message = e.Message;
				result.success = false;
				return result;
			}
			result.success = true;
			return result;
		}

		private MetaDataModelResult getMetaData()
		{
			var result = new MetaDataModelResult();
			try
			{
				var metaData = new MetaDataModel();
				var getVersion = "SELECT * FROM metaData";
				DataLayerShortcut.ExecuteReader<MetaDataModel>(readMetaData, metaData, getVersion);
				result.metaData = metaData;
				result.success = true;
				return result;
			}
			catch(MySqlException e)
			{
				ErrorLog.writeError(e);
				result.success = false;
				result.message = e.Message;
				return result;
			}
		}

		private void readMetaData(IDataReader reader, MetaDataModel data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null)
			{
				if (reader.FieldCount >= 4)
				{
					var temp = reader.GetValue(1) as uint?;
					data.version = temp.HasValue ? (int)temp.Value : -1;
					data.createdDate = reader.GetDateTime(2);
					data.updatedDate = reader.GetDateTime(3);
				}
			}
		}

		[Serializable]
		private class MetaDataModel
		{
			public int version;
			public DateTime createdDate;
			public DateTime updatedDate;
		}

		private class MetaDataModelResult : BaseResult
		{
			public MetaDataModel metaData;
		}
	}
}
