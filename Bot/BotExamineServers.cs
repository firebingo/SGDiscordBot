using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using System.Data;

namespace SGMessageBot.Bot
{
	/// <summary>
	/// This class is used for the process to run through the bots server list and add missing data to the database.
	/// </summary>
	public static class BotExamineServers
	{
		public static async Task startupCheck(IEnumerable<Discord.Server> servers)
		{
			foreach(var server in servers)
			{
				await updateDatabaseServer(server);
			}
		}
		/// <summary>
		/// Updates a specific server in the database
		/// </summary>
		public static async Task updateDatabaseServer(Discord.Server server)
		{
			await Task.Delay(0).ConfigureAwait(false);
			try
			{
				var queryString = @"INSERT INTO servers (serverID, ownerID, serverName, userCount, channelCount, roleCount, regionID, createdDate)
			VALUES (@serverID, @ownerID, @serverName, @userCount, @channelCount, @roleCount, @regionID, @createdDate)
			ON DUPLICATE KEY UPDATE ownerID=@ownerID, serverName=@serverName, userCount=@userCount, channelCount=@channelCount, 
			roleCount=@roleCount, regionID=@regionID, createdDate=@createdDate";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", server.Id), new MySqlParameter("@ownerID", server.Owner.Id),
					new MySqlParameter("@serverName", server.Name), new MySqlParameter("@userCount", server.UserCount), new MySqlParameter("@channelCount", server.ChannelCount),
					new MySqlParameter("@roleCount", server.RoleCount), new MySqlParameter("@regionID", server.Region.Id), new MySqlParameter("@createdDate", server.Owner.JoinedAt));
				foreach (var role in server.Roles)
				{
					queryString = @"INSERT INTO roles (serverID, roleID, roleName, roleColor, roleMention, isEveryone)
				VALUES (@serverID, @roleID, @roleName, @roleColor, @roleMention, @isEveryone)
				ON DUPLICATE KEY UPDATE serverID=@serverID, roleName=@roleName, roleColor=@roleColor, roleMention=@roleMention, isEveryone=@isEveryone";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", role.Server.Id), new MySqlParameter("@roleID", role.Id),
						new MySqlParameter("@roleName", role.Name), new MySqlParameter("@roleColor", role.Color.ToString()), new MySqlParameter("@roleMention", role.Mention),
						new MySqlParameter("@isEveryone", role.IsEveryone));
				}
				foreach (var channel in server.TextChannels)
				{
					queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType)
				VALUES (@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType)
				ON DUPLICATE KEY UPDATE serverID=@serverID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition, channelType=@channelType";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", channel.Server.Id), new MySqlParameter("@channelID", channel.Id),
						new MySqlParameter("@channelMention", channel.Mention), new MySqlParameter("@channelName", channel.Name), new MySqlParameter("@channelPosition", channel.Position),
						new MySqlParameter("@channelType", channel.Type.Value));
				}
				foreach (var channel in server.VoiceChannels)
				{
					queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType)
				VALUES (@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType)
				ON DUPLICATE KEY UPDATE serverID=@serverID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition, channelType=@channelType";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", channel.Server.Id), new MySqlParameter("@channelID", channel.Id),
						new MySqlParameter("@channelMention", channel.Mention), new MySqlParameter("@channelName", channel.Name), new MySqlParameter("@channelPosition", channel.Position),
						new MySqlParameter("@channelType", channel.Type.Value));
				}
				foreach (var emoji in server.CustomEmojis)
				{
					queryString = @"INSERT INTO emojis (serverID, emojiID, emojiName, isManaged, colonsRequired)
				VALUES(@serverID, @emojiID, @emojiName, @isManaged, @colonsRequired)
				ON DUPLICATE KEY UPDATE serverID=@serverID, emojiName=@emojiName, isManaged=@isManaged, colonsRequired=@colonsRequired";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", server.Id), new MySqlParameter("@emojiID", emoji.Id),
						new MySqlParameter("@emojiName", emoji.Name), new MySqlParameter("@isManaged", emoji.IsManaged), new MySqlParameter("@colonsRequired", emoji.RequireColons));
				}
				foreach (var user in server.Users)
				{
					queryString = @"INSERT INTO users (userID, userName, mention, isBot)
				VALUES(@userID, @userName, @mention, @isBot)
				ON DUPLICATE KEY UPDATE userID=@userID, userName=@userName, mention=@mention, isBot=@isBot";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@userID", user.Id), new MySqlParameter("@userName", user.Name),
						new MySqlParameter("@mention", user.Mention), new MySqlParameter("@isBot", user.IsBot));

					queryString = @"INSERT INTO usersInServers (serverID, userID, discriminator, nickName, nickNameMention, joinedDate, avatarID, avatarUrl, lastOnline)
				VALUES(@serverID, @userID, @discriminator, @nickName, @nickNameMention, @joinedDate, @avatarID, @avatarUrl, @lastOnline)
				ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, discriminator=@discriminator, nickName=@nickName, nickNameMention=@nickNameMention, 
				joinedDate=@joinedDate, avatarID=@avatarID, avatarUrl=@avatarUrl, lastOnline=@lastOnline";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", user.Server.Id), new MySqlParameter("@userID", user.Id),
						new MySqlParameter("@discriminator", user.Discriminator), new MySqlParameter("@nickName", user.Nickname), new MySqlParameter("@nickNameMention", user.NicknameMention),
						new MySqlParameter("@joinedDate", user.JoinedAt), new MySqlParameter("@avatarID", user.AvatarId), new MySqlParameter("@avatarUrl", user.AvatarUrl),
						new MySqlParameter("@lastOnline", user.LastOnlineAt));
				}
			}
			catch (Exception e)
			{
				return;
			}
		}
	}
}
