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
			buildQueries.Add("CREATE TABLE servers (serverID VARCHAR(64), ownerID VARCHAR(64), serverName VARCHAR(255), userCount INT UNSIGNED, channelCount SMALLINT UNSIGNED, roleCount SMALLINT UNSIGNED, regionID varChar(64), createdDate DATETIME, PRIMARY KEY (serverID))");
			buildQueries.Add("CREATE TABLE roles (serverID VARCHAR(64), roleID VARCHAR(64), roleName VARCHAR(64), roleColor VARCHAR(10), CONSTRAINT kf_roleServer FOREIGN KEY (serverID) REFERENCES servers(serverID), PRIMARY KEY (roleID))");
			buildQueries.Add(@"CREATE TABLE channels (serverID VARCHAR(64), channelID VARCHAR(64), channelMention VARCHAR(64), channelName VARCHAR(64), channelPosition SMALLINT UNSIGNED, channelType SMALLINT UNSIGNED, 
			CONSTRAINT kf_channelServer FOREIGN KEY (serverID) REFERENCES servers(serverID), PRIMARY KEY (channelID))");
			buildQueries.Add("CREATE TABLE emojis (serverID VARCHAR(64), emojiID VARCHAR(64), emojiName CHAR(32), isManaged BOOL, Colons BOOL, CONSTRAINT kf_emojiServer FOREIGN KEY (serverID) REFERENCES servers(serverID), PRIMARY KEY (emojiID))");
			buildQueries.Add("CREATE TABLE users (userID VARCHAR(64), userName VARCHAR(128), mention VARCHAR(64), isBot BOOL, PRIMARY KEY (userID))");
			buildQueries.Add(@"CREATE TABLE usersInServers (serverID VARCHAR(64), userID VARCHAR(64), discriminator VARCHAR(32), nickName VARCHAR(128), nickNameMention VARCHAR(64), joinedDate DATETIME, avatarID VARCHAR(128), avatarUrl TEXT, 
			CONSTRAINT kf_userTableID FOREIGN KEY(userID) REFERENCES users(userID), CONSTRAINT kf_userServerID FOREIGN KEY(serverID) REFERENCES servers(serverID))");
			buildQueries.Add(@"CREATE TABLE messages (serverID VARCHAR(64), userID VARCHAR(64), channelID VARCHAR(64), messageID VARCHAR(128), rawText TEXT, mesText TEXT, mesStatus TINYINT UNSIGNED, mesTime DATETIME, mesEditedTime DATETIME,
			CONSTRAINT kf_mesUserID FOREIGN KEY (userID) REFERENCES users(userID), CONSTRAINT kf_mesServerID FOREIGN KEY (serverID) REFERENCES servers(serverID), PRIMARY KEY (messageID), CONSTRAINT kf_mesChanID FOREIGN KEY (channelID) REFERENCES channels(channelID))");
			buildQueries.Add("CREATE TABLE attachments (messageID VARCHAR(128), attachID VARCHAR(64), fileName VARCHAR(256), height SMALLINT UNSIGNED, width SMALLINT UNSIGNED, proxyURL TEXT, attachURL TEXT, attachSize BIGINT UNSIGNED, PRIMARY KEY (attachID))");
		}

		public BaseResult createDatabase()
		{
			var result = new BaseResult();
			try
			{
				DataLayerShortcut.closeConnection();
				foreach (var query in createQueries)
				{
					DataLayerShortcut.ExecuteNonQuery(query, $"server={DataLayerShortcut.DBConfig.config.address};uid={DataLayerShortcut.DBConfig.config.userName};pwd={DataLayerShortcut.DBConfig.config.password};");
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
					DataLayerShortcut.ExecuteNonQuery(query, null);
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
