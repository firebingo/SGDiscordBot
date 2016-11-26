using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using System.Data;
using Discord.WebSocket;
using Discord.Commands;

namespace SGMessageBot.Bot
{
	/// <summary>
	/// This class is used for the process to run through the bots server list and add missing data to the database.
	/// </summary>
	public static class BotExamineServers
	{
		public static async Task startupCheck(IEnumerable<SocketGuild> servers)
		{
			foreach (var server in servers)
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
					else if (vChannel != null)
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

		/// <summary>
		/// Goes through the message history of a channel and gets all messages sent in the channel.
		/// Note this can be an expensive operation and should not be done commonly.
		/// </summary>
		/// <param name="context">The context from the command, used to get the channel to reload</param>
		/// <returns></returns>
		public static async Task<string> updateMessageHistoryChannel(CommandContext context)
		{
			try
			{
				Console.WriteLine($"Reloading messages for channel{context.Channel.Name}/{context.Channel.Id}");
				var messages = context.Channel.GetMessagesAsync(Int32.MaxValue).ToList().Result;
				foreach (var messageList in messages)
				{
					List<string> mesRows = new List<string>();
					List<string> attachRows = new List<string>();
					var cCount = 0;
					++cCount;
					Console.WriteLine($"{Math.Round(((float)cCount / (float)messages.Count) * 100, 2)}% complete on current Channel");
					foreach (var message in messageList)
					{
						mesRows.Add($"({context.Guild.Id}, {message.Author.Id}, {context.Channel.Id}, {message.Id}, '{MySqlHelper.EscapeString(message.Content)}', '{MySqlHelper.EscapeString(message.Content)}', 0, {message.Timestamp.UtcDateTime.ToString("yyyyMMddHHmmss")})");
						foreach (var attach in message.Attachments)
						{
							attachRows.Add($"({message.Id}, {attach.Id}, '{MySqlHelper.EscapeString(attach.Filename)}', {(attach.Height.HasValue ? attach.Height.Value : -1)}, {(attach.Width.HasValue ? attach.Width.Value : -1)}, '{MySqlHelper.EscapeString(attach.ProxyUrl)}', '{MySqlHelper.EscapeString(attach.Url)}', {attach.Size})");
						}
					}
					if (mesRows.Count > 0)
					{
						var mesQueryString = $"INSERT IGNORE messages (serverID, userID, channelID, messageID, rawText, mesText, mesStatus, mesTime) VALUES {string.Join(",", mesRows)}";
						var mesRes = DataLayerShortcut.ExecuteNonQuery(mesQueryString);
						if (mesRes != String.Empty)
							return mesRes;
					}
					if (attachRows.Count > 0)
					{
						var attachQueryString = $"INSERT IGNORE attachments (messageID, attachID, fileName, height, width, proxyURL, attachURL, attachSize) VALUES {string.Join(",", attachRows)}";
						var mesRes = DataLayerShortcut.ExecuteNonQuery(attachQueryString);
						if (mesRes != String.Empty)
							return mesRes;
					}
				}
			}
			catch (Exception e)
			{
				var aE = e as AggregateException;
				if (aE != null)
				{
					List<string> exceptions = new List<string>();
					foreach (var ex in aE.InnerExceptions)
					{
						exceptions.Add(ex.Message);
					}
					return $"Exceptions: {string.Join(", ", exceptions)}";
				}
				else
				{
					return $"Exception: {e.Message}";
				}
			}
			return "Operation Complete";
		}

		/// <summary>
		/// Goes through the message history of every channel on a server and gets all messages sent in the channel.
		/// Note this can be an expensive operation and should not be done commonly.
		/// </summary>
		/// <param name="context">The context from the command, used to get the server to reload</param>
		/// <returns></returns>
		public static async Task<string> updateMessageHistoryServer(CommandContext context)
		{
			var exceptionsResult = new List<string>();
			Console.WriteLine("Reloading all server messages");
			try
			{
				var channels = context.Guild.GetChannelsAsync().Result;
				foreach (var channel in channels)
				{
					try
					{
						Console.WriteLine($"Working on channel{channel.Name}/{channel.Id}");
						var messageChannel = channel as Discord.IMessageChannel;
						//Voice channels will be null obviously.
						if (messageChannel != null)
						{
							var messages = messageChannel.GetMessagesAsync(Int32.MaxValue).ToList().Result;
							var cCount = 0;
							foreach (var messageList in messages)
							{
								++cCount;
								Console.WriteLine($"{Math.Round(((float)cCount / (float)messages.Count) * 100, 2)}% complete on current Channel");
								List<string> mesRows = new List<string>();
								List<string> attachRows = new List<string>();
								foreach (var message in messageList)
								{
									mesRows.Add($"({context.Guild.Id}, {message.Author.Id}, {context.Channel.Id}, {message.Id}, '{MySqlHelper.EscapeString(message.Content)}', '{MySqlHelper.EscapeString(message.Content)}', 0, {message.Timestamp.UtcDateTime.ToString("yyyyMMddHHmmss")})");
									foreach (var attach in message.Attachments)
									{
										attachRows.Add($"({message.Id}, {attach.Id}, '{MySqlHelper.EscapeString(attach.Filename)}', {(attach.Height.HasValue ? attach.Height.Value : -1)}, {(attach.Width.HasValue ? attach.Width.Value : -1)}, '{MySqlHelper.EscapeString(attach.ProxyUrl)}', '{MySqlHelper.EscapeString(attach.Url)}', {attach.Size})");
									}
								}
								if (mesRows.Count > 0)
								{
									var mesQueryString = $"INSERT IGNORE messages (serverID, userID, channelID, messageID, rawText, mesText, mesStatus, mesTime) VALUES {string.Join(",", mesRows)}";
									var mesRes = DataLayerShortcut.ExecuteNonQuery(mesQueryString);
									if (mesRes != String.Empty)
										return mesRes;
								}
								if (attachRows.Count > 0)
								{
									var attachQueryString = $"INSERT IGNORE attachments (messageID, attachID, fileName, height, width, proxyURL, attachURL, attachSize) VALUES {string.Join(",", attachRows)}";
									var mesRes = DataLayerShortcut.ExecuteNonQuery(attachQueryString);
									if (mesRes != String.Empty)
										return mesRes;
								}
							}
						}
					}
					catch (Exception e) //Something something don't use exceptions for flow control something something.
					{
						var aE = e as AggregateException;
						if (aE != null)
						{
							List<string> exceptions = new List<string>();
							foreach (var ex in aE.InnerExceptions)
							{
								exceptions.Add(ex.Message);
							}
							exceptionsResult.Add($"Exceptions: {string.Join(", ", exceptions)}");
						}
						else
						{
							exceptionsResult.Add($"Exception: {e.Message}");
						}
						continue;
					}
				}
			}
			catch (Exception e)
			{
				var aE = e as AggregateException;
				if (aE != null)
				{
					List<string> exceptions = new List<string>();
					foreach(var ex in aE.InnerExceptions)
					{
						exceptions.Add(ex.Message);
					}
					return $"Exceptions: {string.Join(", ", exceptions)}";
				}
				else
				{
					return $"Exception: {e.Message}";
				}
			}
			if (exceptionsResult.Count > 0)
			{
				return $"Operation complete with exceptions, {string.Join(",", exceptionsResult)}";
			}
			else
			{
				return "Operation Complete";
			}
		}
	}
}
