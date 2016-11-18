using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using System.Data;
using Discord.WebSocket;

namespace SGMessageBot.Bot
{
	/// <summary>
	/// This class is used for the process to run through the bots server list and add missing data to the database.
	/// </summary>
	public static class BotExamineServers
	{
		public static async Task startupCheck(IEnumerable<SocketGuild> servers)
		{
			foreach(var server in servers)
			{
				await updateDatabaseServer(server);
			}
		}
		/// <summary>
		/// Updates a specific server in the database
		/// </summary>
		public static async Task updateDatabaseServer(SocketGuild server)
		{
			await Task.Delay(0).ConfigureAwait(false);
			try
			{
				var queryString = @"INSERT INTO servers (serverID, ownerID, serverName, userCount, channelCount, roleCount, regionID, createdDate)
				VALUES (@serverID, @ownerID, @serverName, @userCount, @channelCount, @roleCount, @regionID, @createdDate)
				ON DUPLICATE KEY UPDATE ownerID=@ownerID, serverName=@serverName, userCount=@userCount, channelCount=@channelCount, 
				roleCount=@roleCount, regionID=@regionID, createdDate=@createdDate";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", server.Id), new MySqlParameter("@ownerID", server.OwnerId),
					new MySqlParameter("@serverName", server.Name), new MySqlParameter("@userCount", server.MemberCount), new MySqlParameter("@channelCount", server.Channels.Count),
					new MySqlParameter("@roleCount", server.Roles.Count), new MySqlParameter("@regionID", server.VoiceRegionId), new MySqlParameter("@createdDate", server.CreatedAt.UtcDateTime));
				foreach (var role in server.Roles)
				{
					queryString = @"INSERT INTO roles (serverID, roleID, roleName, roleColor, roleMention, isEveryone)
					VALUES (@serverID, @roleID, @roleName, @roleColor, @roleMention, @isEveryone)
					ON DUPLICATE KEY UPDATE serverID=@serverID, roleName=@roleName, roleColor=@roleColor, roleMention=@roleMention, isEveryone=@isEveryone";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", role.Guild.Id), new MySqlParameter("@roleID", role.Id),
						new MySqlParameter("@roleName", role.Name), new MySqlParameter("@roleColor", role.Color.ToString()), new MySqlParameter("@roleMention", role.Mention),
						new MySqlParameter("@isEveryone", role.IsEveryone));
				}
				foreach (var channel in server.Channels)
				{
					var tChannel = channel as SocketTextChannel;
					var vChannel = channel as SocketVoiceChannel;
					if (tChannel != null)
					{
						queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType)
						VALUES (@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType)
						ON DUPLICATE KEY UPDATE serverID=@serverID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition, channelType=@channelType";
						DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", channel.Guild.Id), new MySqlParameter("@channelID", channel.Id),
							new MySqlParameter("@channelMention", tChannel.Mention), new MySqlParameter("@channelName", channel.Name), new MySqlParameter("@channelPosition", channel.Position),
							new MySqlParameter("@channelType", "text"));
					}
					else if(vChannel != null)
					{
						queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType)
						VALUES (@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType)
						ON DUPLICATE KEY UPDATE serverID=@serverID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition, channelType=@channelType";
						DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", channel.Guild.Id), new MySqlParameter("@channelID", channel.Id),
							new MySqlParameter("@channelMention", null), new MySqlParameter("@channelName", channel.Name), new MySqlParameter("@channelPosition", channel.Position),
							new MySqlParameter("@channelType", "voice"));
					}
				}
				foreach (var emoji in server.Emojis)
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
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@userID", user.Id), new MySqlParameter("@userName", user.Username),
						new MySqlParameter("@mention", user.Mention.Replace("!", String.Empty)), new MySqlParameter("@isBot", user.IsBot));

					DateTime? joinedAtDateTime = user.JoinedAt.HasValue ? ((user.JoinedAt.Value.UtcDateTime) as DateTime?) : null;
					queryString = @"INSERT INTO usersInServers (serverID, userID, discriminator, nickName, nickNameMention, joinedDate, avatarID, avatarUrl, lastOnline)
					VALUES(@serverID, @userID, @discriminator, @nickName, @nickNameMention, @joinedDate, @avatarID, @avatarUrl, @lastOnline)
					ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, discriminator=@discriminator, nickName=@nickName, nickNameMention=@nickNameMention, 
					joinedDate=@joinedDate, avatarID=@avatarID, avatarUrl=@avatarUrl, lastOnline=@lastOnline";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", user.Guild.Id), new MySqlParameter("@userID", user.Id),
						new MySqlParameter("@discriminator", user.Discriminator), new MySqlParameter("@nickName", user.Nickname), new MySqlParameter("@nickNameMention", user.Mention.Replace("!", String.Empty)),
						new MySqlParameter("@joinedDate", joinedAtDateTime), new MySqlParameter("@avatarID", user.AvatarId), new MySqlParameter("@avatarUrl", user.AvatarUrl),
						new MySqlParameter("@lastOnline", joinedAtDateTime));
				}
			}
			catch (Exception e)
			{
				return;
			}
		}
	}
}
