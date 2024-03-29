﻿using MySql.Data.MySqlClient;
using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace SGMessageBot.DataBase
{
	public class DatebaseCreate
	{
		private readonly List<string> createQueries;
		private readonly Dictionary<int, List<string>> buildQueries;
		public const int mkey = 345; //this is just so metadata can be updated.

		public DatebaseCreate()
		{
			createQueries = new List<string>
			{
				$"CREATE DATABASE {DataLayerShortcut.DBConfig.Config.schemaName}",
				$"ALTER DATABASE {DataLayerShortcut.DBConfig.Config.schemaName} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci",
				"CREATE TABLE metaData (mkey INT, version INT UNSIGNED, createdDate DATETIME, updatedDate DATETIME, PRIMARY KEY(mkey))"
			};
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
			buildQueries.Add(9, new List<string>());
			buildQueries[9].Add("ALTER TABLE stats ADD COLUMN dateGroup INT UNSIGNED NOT NULL DEFAULT 1");
			buildQueries[9].Add("ALTER TABLE stats ADD id INT NOT NULL AUTO_INCREMENT PRIMARY KEY");
			buildQueries.Add(10, new List<string>());
			buildQueries[10].Add("ALTER TABLE users ADD COLUMN isWebHook BOOL DEFAULT FALSE;");
			buildQueries.Add(11, new List<string>());
			buildQueries[11].Add("CREATE TABLE messageCorpus (keyword VARCHAR(128), wordValues MEDIUMTEXT, PRIMARY KEY(keyword));");
			buildQueries[11].Add("ALTER TABLE metaData ADD COLUMN lastCorpusDate DATETIME;");
			buildQueries.Add(12, new List<string>());
			buildQueries[12].Add("ALTER TABLE messageCorpus MODIFY keyword VARCHAR(128) BINARY;");
			buildQueries.Add(13, new List<string>());
			buildQueries[13].Add("ALTER TABLE messages ADD COLUMN flags INT SIGNED default 0;");
			buildQueries[13].Add(@"CREATE TABLE stickerUses (serverID BIGINT UNSIGNED, userID BIGINT UNSIGNED, channelID BIGINT UNSIGNED, messageID BIGINT UNSIGNED, stickerID BIGINT UNSIGNED, stickerName VARCHAR(64), formatType INT SIGNED, isDeleted BOOL DEFAULT FALSE,
			CONSTRAINT kf_stickerMesID FOREIGN KEY(messageID) REFERENCES messages(messageID), CONSTRAINT kf_stickerServerID FOREIGN KEY(serverID) REFERENCES servers(serverID), CONSTRAINT kf_stickerChanID FOREIGN KEY(channelID) REFERENCES channels(channelID));");
			buildQueries[13].Add("CREATE INDEX idx_messagesServerId ON messages(serverId);");
			buildQueries[13].Add("CREATE INDEX idx_channelsServerId ON channels(serverId);");
			buildQueries[13].Add("CREATE INDEX idx_rolesServerId ON roles(serverId);");
			buildQueries[13].Add("CREATE INDEX idx_usersInServersServerId ON usersInServers(serverId);");
			buildQueries[13].Add("CREATE INDEX idx_reactionsServerId ON reactions(serverId);");
			buildQueries[13].Add("CREATE INDEX idx_emojisServerId ON emojis(serverId);");
			buildQueries[13].Add("CREATE INDEX idx_emojiUsesServerId ON emojiUses(serverId);");
			buildQueries[13].Add("CREATE INDEX idx_stickerUsesServerId ON stickerUses (serverId);");
			buildQueries.Add(14, new List<string>());
			buildQueries[14].Add("ALTER TABLE channels ADD COLUMN threadChannelId BIGINT UNSIGNED DEFAULT NULL, ADD FOREIGN KEY kf_threadChannel(threadChannelId) REFERENCES channels(channelID);");
			buildQueries[14].Add("CREATE INDEX idx_threadChannelId ON channels(threadChannelId);");
			buildQueries.Add(15, new List<string>());
			//These indexs were already covered by foreign keys and can cause issues as duplicates.
			buildQueries[15].Add("DROP INDEX idx_messagesServerId ON messages;");
			buildQueries[15].Add("DROP INDEX idx_channelsServerId ON channels;");
			buildQueries[15].Add("DROP INDEX idx_rolesServerId ON roles;");
			buildQueries[15].Add("DROP INDEX idx_usersInServersServerId ON usersInServers;");
			buildQueries[15].Add("DROP INDEX idx_reactionsServerId ON reactions;");
			buildQueries[15].Add("DROP INDEX idx_emojisServerId ON emojis;");
			buildQueries[15].Add("DROP INDEX idx_emojiUsesServerId ON emojiUses;");
		}

		public async Task<BaseResult> CreateDatabase()
		{
			var result = new BaseResult();
			try
			{
				foreach (var query in createQueries)
				{
					await DataLayerShortcut.ExecuteSpecialNonQuery(query, $"server={DataLayerShortcut.DBConfig.Config.address};uid={DataLayerShortcut.DBConfig.Config.userName};pwd={DataLayerShortcut.DBConfig.Config.password};charset=utf8mb4");
				}
				var metaData = "INSERT INTO metaData (mkey, version, createdDate, updatedDate) VALUES (@mkey, @version, @createdDate, @updatedDate)";
				await DataLayerShortcut.ExecuteSpecialNonQuery(metaData, $"server={DataLayerShortcut.DBConfig.Config.address};uid={DataLayerShortcut.DBConfig.Config.userName};pwd={DataLayerShortcut.DBConfig.Config.password};charset=utf8mb4",
					new MySqlParameter("@mkey", mkey), new MySqlParameter("@version", 0), new MySqlParameter("@createdDate", DateTime.UtcNow), new MySqlParameter("@updatedDate", DateTime.UtcNow));
			}
			catch (MySqlException e)
			{
				ErrorLog.WriteError(e);
				result.Message = e.Message;
				result.Success = false;
				return result;
			}
			result.Success = true;
			return result;
		}

		public async Task<BaseResult> BuildDatabase()
		{
			var result = new BaseResult();
			var metaDataGet = await GetMetaData();
			//this hopefully should only happen if a db was made on version 1 before the metaData table.
			if (!metaDataGet.Success)
			{
				Console.WriteLine("Database could not get metadata, this probally means the database was created before metadata was added. \nVersion will be set to 1 then new >version 1 tables will be created.\n" +
					"If you were somehow above version 1 type n to exit and backup your database before this is run.");
				var read = Console.ReadLine();
				read = read.ToLower();
				if (read == "n")
					Environment.Exit(0);

				var metaQuery = "CREATE TABLE metaData (mkey INT, version INT UNSIGNED, createdDate DATETIME, updatedDate DATETIME, PRIMARY KEY(mkey))";
				await DataLayerShortcut.ExecuteNonQuery(metaQuery);
				metaQuery = "INSERT INTO metaData (mkey, version, createdDate, updatedDate) VALUES (@mkey, @version, @createdDate, @updatedDate)";
				await DataLayerShortcut.ExecuteNonQuery(metaQuery, new MySqlParameter("@mkey", mkey), new MySqlParameter("@version", 1), new MySqlParameter("@createdDate", DateTime.UtcNow), new MySqlParameter("@updatedDate", DateTime.UtcNow));
				metaDataGet = await GetMetaData();
				if (!metaDataGet.Success)
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
					result.Message = "Failure to build database. Metadata reported a version below 0.";
					result.Success = false;
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
						if (buildQueries.TryGetValue(v, out var ver))
						{
							foreach (var query in ver)
							{
								await DataLayerShortcut.ExecuteNonQuery(query);
							}
						}
						var metaDataUpdate = "UPDATE metaData SET version=@version, updatedDate=@updatedDate WHERE mkey=@mkey";
						await DataLayerShortcut.ExecuteNonQuery(metaDataUpdate, new MySqlParameter("@version", v), new MySqlParameter("@updatedDate", DateTime.UtcNow), new MySqlParameter("@mkey", mkey));
					}
				}
			}
			catch (MySqlException e)
			{
				ErrorLog.WriteError(e);
				result.Message = e.Message;
				result.Success = false;
				return result;
			}
			result.Success = true;
			return result;
		}

		public static async Task<MetaDataModelResult> GetMetaData()
		{
			var result = new MetaDataModelResult();
			try
			{
				var metaData = new MetaDataModel();
				var getVersion = "SELECT * FROM metaData";
				await DataLayerShortcut.ExecuteReader(ReadMetaData, metaData, getVersion);
				result.metaData = metaData;
				result.Success = true;
				return result;
			}
			catch (MySqlException e)
			{
				ErrorLog.WriteError(e);
				result.Success = false;
				result.Message = e.Message;
				return result;
			}
		}

		private static void ReadMetaData(IDataReader reader, MetaDataModel data)
		{
			reader = reader as DbDataReader;
			if (reader != null)
			{
				if (reader.FieldCount >= 4)
				{
					var temp = reader.GetValue(1) as uint?;
					data.version = temp.HasValue ? (int)temp.Value : -1;
					data.createdDate = reader.GetDateTime(2);
					data.updatedDate = reader.GetDateTime(3);
					if (data.version > 10)
						data.lastCorpusDate = reader.GetValue(4) as DateTime?;
				}
			}
		}

		[Serializable]
		public class MetaDataModel
		{
			public int version;
			public DateTime createdDate;
			public DateTime updatedDate;
			public DateTime? lastCorpusDate;
		}

		public class MetaDataModelResult : BaseResult
		{
			public MetaDataModel metaData;
		}
	}
}
