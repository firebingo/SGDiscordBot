using MySql.Data.MySqlClient;
using SGMessageBot.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot.DataBase
{
	public class DatebaseCreate
	{
		private List<string> createQueries;
		private List<string> buildQueries;

		public DatebaseCreate()
		{
			createQueries = new List<string>();
			createQueries.Add($"CREATE DATABASE {DataLayerShortcut.DBConfig.config.schemaName}");
			createQueries.Add($"ALTER DATABASE {DataLayerShortcut.DBConfig.config.schemaName} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
			buildQueries = new List<string>();
			buildQueries.Add("CREATE TABLE servers (serverID BIGINT UNSIGNED, ownerID BIGINT UNSIGNED, serverName VARCHAR(256), userCount INT UNSIGNED, channelCount SMALLINT UNSIGNED, roleCount SMALLINT UNSIGNED, regionID varChar(64), createdDate DATETIME, isDeleted BOOL DEFAULT FALSE, PRIMARY KEY (serverID))");
			buildQueries.Add("CREATE TABLE roles (serverID BIGINT UNSIGNED, roleID BIGINT UNSIGNED, roleName VARCHAR(64), roleColor VARCHAR(10), roleMention VARCHAR(64), isEveryone BOOL, isDeleted BOOL DEFAULT FALSE, CONSTRAINT kf_roleServer FOREIGN KEY (serverID) REFERENCES servers(serverID), PRIMARY KEY (roleID))");
			buildQueries.Add(@"CREATE TABLE channels (serverID BIGINT UNSIGNED, channelID BIGINT UNSIGNED, channelMention VARCHAR(64), channelName VARCHAR(128), channelPosition SMALLINT UNSIGNED, channelType varChar(64), isDeleted BOOL DEFAULT FALSE, 
			CONSTRAINT kf_channelServer FOREIGN KEY (serverID) REFERENCES servers(serverID), PRIMARY KEY (channelID))");
			buildQueries.Add("CREATE TABLE emojis (serverID BIGINT UNSIGNED, emojiID BIGINT UNSIGNED, emojiName CHAR(32), isManaged BOOL, colonsRequired BOOL, isDeleted BOOL DEFAULT FALSE, CONSTRAINT kf_emojiServer FOREIGN KEY (serverID) REFERENCES servers(serverID), PRIMARY KEY (emojiID))");
			buildQueries.Add("CREATE TABLE users (userID BIGINT UNSIGNED, userName VARCHAR(128), mention VARCHAR(64), isBot BOOL, isDeleted BOOL DEFAULT FALSE, PRIMARY KEY (userID))");
			buildQueries.Add(@"CREATE TABLE usersInServers (serverID BIGINT UNSIGNED, userID BIGINT UNSIGNED, discriminator SMALLINT UNSIGNED, nickName VARCHAR(128), nickNameMention VARCHAR(64), joinedDate DATETIME, avatarID VARCHAR(128), avatarUrl TEXT, 
			lastOnline DATETIME, isBanned BOOL DEFAULT FALSE, isDeleted BOOL DEFAULT FALSE, CONSTRAINT kf_userTableID FOREIGN KEY (userID) REFERENCES users(userID), CONSTRAINT kf_userServerID FOREIGN KEY (serverID) REFERENCES servers(serverID), UNIQUE KEY (serverID, userID))");
			buildQueries.Add(@"CREATE TABLE messages (serverID BIGINT UNSIGNED, userID BIGINT UNSIGNED, channelID BIGINT UNSIGNED, messageID BIGINT UNSIGNED, rawText MEDIUMTEXT, mesText MEDIUMTEXT, mesStatus TINYINT UNSIGNED, mesTime DATETIME, mesEditedTime DATETIME, editedRawText MEDIUMTEXT, editedMesText MEDIUMTEXT, isDeleted BOOL DEFAULT FALSE,
			CONSTRAINT kf_mesUserID FOREIGN KEY (userID) REFERENCES users(userID), CONSTRAINT kf_mesServerID FOREIGN KEY (serverID) REFERENCES servers(serverID), CONSTRAINT kf_mesChanID FOREIGN KEY (channelID) REFERENCES channels(channelID), PRIMARY KEY (messageID))");
			buildQueries.Add("CREATE TABLE attachments (messageID BIGINT UNSIGNED, attachID VARCHAR(64), fileName VARCHAR(256), height SMALLINT UNSIGNED, width SMALLINT UNSIGNED, proxyURL TEXT, attachURL TEXT, attachSize BIGINT UNSIGNED, isDeleted BOOL DEFAULT FALSE, PRIMARY KEY (attachID))");
		}

		public BaseResult createDatabase()
		{
			var result = new BaseResult();
			try
			{
				DataLayerShortcut.closeConnection();
				foreach (var query in createQueries)
				{
					DataLayerShortcut.ExecuteSpecialNonQuery(query, $"server={DataLayerShortcut.DBConfig.config.address};uid={DataLayerShortcut.DBConfig.config.userName};pwd={DataLayerShortcut.DBConfig.config.password};charset=utf8mb4");
				}
				DataLayerShortcut.closeConnection();
			}
			catch (MySqlException e)
			{
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
			try
			{
				foreach (var query in buildQueries)
				{
					DataLayerShortcut.ExecuteNonQuery(query);
				}
			}
			catch (MySqlException e)
			{
				result.message = e.Message;
				result.success = false;
				return result;
			}
			result.success = true;
			return result;
		}
	}
}
