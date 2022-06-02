using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Rest;
using Discord;
using Newtonsoft.Json;
using SGMessageBot.Helpers;

namespace SGMessageBot.DiscordBot
{
	/// <summary>
	/// This class is used for the process to run through the bots server list and add missing data to the database.
	/// </summary>
	public static class BotExamineServers
	{
		public static async Task StartupCheck(IEnumerable<SocketGuild> servers)
		{
			await Task.Delay(250);
			foreach (var server in servers)
			{
				await UpdateDatabaseServer(server);
			}
		}
		/// <summary>
		/// Updates a specific server in the database
		/// </summary>
		public static async Task UpdateDatabaseServer(SocketGuild server)
		{
			await Task.Delay(0).ConfigureAwait(false);
			try
			{
				var queryString = @"INSERT INTO servers (serverID, ownerID, serverName, userCount, channelCount, roleCount, regionID, createdDate)
				VALUES (@serverID, @ownerID, @serverName, @userCount, @channelCount, @roleCount, @regionID, @createdDate)
				ON DUPLICATE KEY UPDATE ownerID=@ownerID, serverName=@serverName, userCount=@userCount, channelCount=@channelCount, 
				roleCount=@roleCount, regionID=@regionID, createdDate=@createdDate";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", server.Id), new MySqlParameter("@ownerID", server.OwnerId),
					new MySqlParameter("@serverName", server.Name), new MySqlParameter("@userCount", server.MemberCount), new MySqlParameter("@channelCount", server.Channels.Count),
					new MySqlParameter("@roleCount", server.Roles.Count), new MySqlParameter("@regionID", server.VoiceRegionId), new MySqlParameter("@createdDate", server.CreatedAt.UtcDateTime));
				foreach (var role in server.Roles)
				{
					queryString = @"INSERT INTO roles (serverID, roleID, roleName, roleColor, roleMention, isEveryone)
					VALUES (@serverID, @roleID, @roleName, @roleColor, @roleMention, @isEveryone)
					ON DUPLICATE KEY UPDATE serverID=@serverID, roleName=@roleName, roleColor=@roleColor, roleMention=@roleMention, isEveryone=@isEveryone";
					await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", role.Guild.Id), new MySqlParameter("@roleID", role.Id),
						new MySqlParameter("@roleName", role.Name), new MySqlParameter("@roleColor", role.Color.ToString()), new MySqlParameter("@roleMention", role.Mention),
						new MySqlParameter("@isEveryone", role.IsEveryone));
				}
				foreach (var channel in server.Channels)
				{
					if (channel is SocketTextChannel tChannel)
					{
						queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType)
						VALUES (@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType)
						ON DUPLICATE KEY UPDATE serverID=@serverID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition, channelType=@channelType";
						await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", channel.Guild.Id), new MySqlParameter("@channelID", channel.Id),
							new MySqlParameter("@channelMention", tChannel.Mention), new MySqlParameter("@channelName", channel.Name), new MySqlParameter("@channelPosition", channel.Position),
							new MySqlParameter("@channelType", (int)(tChannel.GetChannelType() ?? 0)));
						foreach (var thread in tChannel.Threads)
						{
							queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType, isDeleted, threadChannelId)
							VALUES (@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType, @isDeleted, @threadChannelId)
							ON DUPLICATE KEY UPDATE serverID=@serverID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition, channelType=@channelType, threadChannelId=@threadChannelId";
							await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", channel.Guild.Id), new MySqlParameter("@channelID", thread.Id),
							new MySqlParameter("@channelMention", thread.Mention), new MySqlParameter("@channelName", thread.Name), new MySqlParameter("@channelPosition", thread.Position),
							new MySqlParameter("@channelType", (int)(thread.GetChannelType() ?? 0)), new MySqlParameter("@isDeleted", false), new MySqlParameter("@threadChannelId", tChannel.Id));
						}
					}
					else
					{
						queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType)
						VALUES (@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType)
						ON DUPLICATE KEY UPDATE serverID=@serverID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition, channelType=@channelType";
						await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", channel.Guild.Id), new MySqlParameter("@channelID", channel.Id),
							new MySqlParameter("@channelMention", DiscordHelpers.GetChannelMention(channel)), new MySqlParameter("@channelName", channel.Name), new MySqlParameter("@channelPosition", channel.Position),
							new MySqlParameter("@channelType", (int)(channel.GetChannelType() ?? 0)));
					}
				}
				foreach (var emoji in server.Emotes)
				{
					queryString = @"INSERT INTO emojis (serverID, emojiID, emojiName, isManaged, colonsRequired, isAnimated)
					VALUES(@serverID, @emojiID, @emojiName, @isManaged, @colonsRequired, @isAnimated)
					ON DUPLICATE KEY UPDATE serverID=@serverID, emojiName=@emojiName, isManaged=@isManaged, colonsRequired=@colonsRequired";
					await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", server.Id), new MySqlParameter("@emojiID", emoji.Id),
						new MySqlParameter("@emojiName", emoji.Name), new MySqlParameter("@isManaged", emoji.IsManaged), new MySqlParameter("@colonsRequired", emoji.RequireColons),
						new MySqlParameter("@isAnimated", false));
				}
				foreach (var user in server.Users)
				{
					queryString = @"INSERT INTO users (userID, userName, mention, isBot, isWebHook)
					VALUES(@userID, @userName, @mention, @isBot, @isWebHook)
					ON DUPLICATE KEY UPDATE userID=@userID, userName=@userName, mention=@mention, isBot=@isBot, isWebHook=@isWebHook";
					await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@userID", user.Id), new MySqlParameter("@userName", user.Username),
						new MySqlParameter("@mention", user.Mention.Replace("!", String.Empty)), new MySqlParameter("@isBot", user.IsBot), new MySqlParameter("@isWebHook", user.IsWebhook));

					var roleIds = new List<ulong>();
					foreach (var role in user.Roles)
						roleIds.Add(role.Id);

					DateTime? joinedAtDateTime = user.JoinedAt.HasValue ? ((user.JoinedAt.Value.UtcDateTime) as DateTime?) : null;
					queryString = @"INSERT INTO usersInServers (serverID, userID, discriminator, nickName, nickNameMention, joinedDate, avatarID, avatarUrl, lastOnline, roleIDs, mesCount)
					VALUES(@serverID, @userID, @discriminator, @nickName, @nickNameMention, @joinedDate, @avatarID, @avatarUrl, @lastOnline, @roleIds, @mesCount)
					ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, discriminator=@discriminator, nickName=@nickName, nickNameMention=@nickNameMention, 
					joinedDate=@joinedDate, avatarID=@avatarID, avatarUrl=@avatarUrl, lastOnline=@lastOnline, roleIDs=@roleIds";
					await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", user.Guild.Id), new MySqlParameter("@userID", user.Id),
						new MySqlParameter("@discriminator", user.Discriminator), new MySqlParameter("@nickName", user.Nickname), new MySqlParameter("@nickNameMention", user.Mention.Replace("!", String.Empty)),
						new MySqlParameter("@joinedDate", joinedAtDateTime), new MySqlParameter("@avatarID", user.AvatarId), new MySqlParameter("@avatarUrl", user.GetAvatarUrl()),
						new MySqlParameter("@lastOnline", joinedAtDateTime), new MySqlParameter("@roleIds", JsonConvert.SerializeObject(roleIds)), new MySqlParameter("@mesCount", value: 0));
				}
				try
				{
					foreach (var hook in await server.GetWebhooksAsync())
					{
						queryString = @"INSERT INTO users (userID, userName, mention, isBot, isWebHook)
					VALUES(@userID, @userName, @mention, @isBot, @isWebHook)
					ON DUPLICATE KEY UPDATE userID=@userID, userName=@userName, mention=@mention, isBot=@isBot, isWebHook=@isWebHook";
						await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@userID", hook.Id), new MySqlParameter("@userName", hook.Name),
							new MySqlParameter("@mention", string.Empty), new MySqlParameter("@isBot", true), new MySqlParameter("@isWebHook", true));

						var roleIds = new List<ulong>();

						DateTime joinedAtDateTime = hook.CreatedAt.UtcDateTime;
						queryString = @"INSERT INTO usersInServers (serverID, userID, discriminator, nickName, nickNameMention, joinedDate, avatarID, avatarUrl, lastOnline, roleIDs, mesCount)
					VALUES(@serverID, @userID, @discriminator, @nickName, @nickNameMention, @joinedDate, @avatarID, @avatarUrl, @lastOnline, @roleIds, @mesCount)
					ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, discriminator=@discriminator, nickName=@nickName, nickNameMention=@nickNameMention, 
					joinedDate=@joinedDate, avatarID=@avatarID, avatarUrl=@avatarUrl, lastOnline=@lastOnline, roleIDs=@roleIds";
						await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", hook.GuildId), new MySqlParameter("@userID", hook.Id),
							new MySqlParameter("@discriminator", 0000), new MySqlParameter("@nickName", null), new MySqlParameter("@nickNameMention", string.Empty),
							new MySqlParameter("@joinedDate", joinedAtDateTime), new MySqlParameter("@avatarID", hook.AvatarId), new MySqlParameter("@avatarUrl", hook.GetAvatarUrl()),
							new MySqlParameter("@lastOnline", joinedAtDateTime), new MySqlParameter("@roleIds", JsonConvert.SerializeObject(roleIds)), new MySqlParameter("@mesCount", value: 0));
					}
				}
				catch (Exception e)
				{
					ErrorLog.WriteLog($"Error getting webhooks for server: {server.Id}");
					ErrorLog.WriteError(e);
					return;
				}
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
				return;
			}
		}

		//TODO: Threads
		/// <summary>
		/// Goes through the message history of a channel and gets all messages sent in the channel.
		/// Note this can be an expensive operation and should not be done commonly.
		/// </summary>
		/// <param name="context">The context from the command, used to get the channel to reload</param>
		/// <returns></returns>
		public static async Task<string> UpdateMessageHistoryChannel(ICommandContext context, IMessageChannel channel)
		{
			try
			{
				if (channel == null)
					channel = context.Channel;
				Console.WriteLine($"Reloading messages for channel{channel.Name}/{channel.Id}");
				//Old reactions must be removed since they have no identification of their own.
				var reactRemove = $"DELETE FROM reactions WHERE channelID = @channelID";
				var delRes = await DataLayerShortcut.ExecuteNonQuery(reactRemove, new MySqlParameter("@channelID", channel.Id));
				if (delRes != String.Empty)
					return delRes;
				//Old emoji uses must be removed since they have no identification of their own.
				var emojiRemove = $"DELETE FROM emojiUses WHERE channelID = @channelID AND NOT isDeleted";
				delRes = await DataLayerShortcut.ExecuteNonQuery(emojiRemove, new MySqlParameter("@channelID", channel.Id));
				if (delRes != String.Empty)
					return delRes;
				var messages = channel.GetMessagesAsync(Int32.MaxValue);
				var cCount = 0;
				var totalMessages = await messages.SumAsync(m => m.Count);
				await foreach (var messageList in messages)
				{
					List<string> mesRows = new List<string>();
					List<string> attachRows = new List<string>();
					List<string> reactionsRows = new List<string>();
					List<string> emojiRows = new List<string>();
					Console.WriteLine($"{Math.Round(((float)cCount / (float)totalMessages) * 100, 2)}% complete on current Channel");
					foreach (var message in messageList)
					{
						++cCount;
						mesRows.Add($"({context.Guild.Id}, {message.Author.Id}, {channel.Id}, {message.Id}, '{MySqlHelper.EscapeString(message.Content)}', '{MySqlHelper.EscapeString(message.Content)}', 0, {message.Timestamp.UtcDateTime:yyyyMMddHHmmss})");
						foreach (var attach in message.Attachments)
						{
							attachRows.Add($"({message.Id}, {attach.Id}, '{MySqlHelper.EscapeString(attach.Filename)}', {(attach.Height ?? -1)}, {(attach.Width ?? -1)}, '{MySqlHelper.EscapeString(attach.ProxyUrl)}', '{MySqlHelper.EscapeString(attach.Url)}', {attach.Size})");
						}
						if (message is RestUserMessage userMes && userMes.Reactions != null)
						{
							foreach (var reaction in userMes.Reactions)
							{
								var emote = reaction.Key as Emote;
								ulong? emoteId = emote?.Id;
								reactionsRows.Add($"({context.Guild.Id}, {userMes.Author.Id}, {userMes.Channel.Id}, {userMes.Id}, {(emoteId.HasValue ? emoteId : 0)}, '{reaction.Key.Name}')");
							}
						}
						CheckMessageForEmoji(context, message, channel, ref emojiRows);
					}
					if (mesRows.Count > 0)
					{
						var mesQueryString = $"INSERT IGNORE messages (serverID, userID, channelID, messageID, rawText, mesText, mesStatus, mesTime) VALUES {string.Join(",", mesRows)}";
						var mesRes = await DataLayerShortcut.ExecuteNonQuery(mesQueryString);
						if (mesRes != String.Empty)
						{
							ErrorLog.WriteLog(mesRes);
							return mesRes;
						}
					}
					if (attachRows.Count > 0)
					{
						var attachQueryString = $"INSERT IGNORE attachments (messageID, attachID, fileName, height, width, proxyURL, attachURL, attachSize) VALUES {string.Join(",", attachRows)}";
						var mesRes = await DataLayerShortcut.ExecuteNonQuery(attachQueryString);
						if (mesRes != String.Empty)
						{
							ErrorLog.WriteLog(mesRes);
							return mesRes;
						}
					}
					if (reactionsRows.Count > 0)
					{
						var reactQueryString = $"INSERT reactions (serverID, userID, channelID, messageID, emojiID, emojiName) VALUES {string.Join(",", reactionsRows)}";
						var mesRes = await DataLayerShortcut.ExecuteNonQuery(reactQueryString);
						if (mesRes != String.Empty)
						{
							ErrorLog.WriteLog(mesRes);
							return mesRes;
						}
					}
					if (emojiRows.Count > 0)
					{
						var emojiQueryString = $"INSERT emojiUses (serverID, userID, channelID, messageID, emojiID, emojiName) VALUES {string.Join(",", emojiRows)}";
						var mesRes = await DataLayerShortcut.ExecuteNonQuery(emojiQueryString);
						if (mesRes != String.Empty)
						{
							ErrorLog.WriteLog(mesRes);
							return mesRes;
						}
					}
				}
			}
			catch (Exception e)
			{
				if (e is AggregateException aE)
				{
					List<string> exceptions = new List<string>();
					foreach (var ex in aE.InnerExceptions)
					{
						exceptions.Add(ex.Message);
					}
					ErrorLog.WriteLog(string.Join(", ", exceptions));
					return $"Exceptions: {string.Join(", ", exceptions)}";
				}
				else
				{
					ErrorLog.WriteError(e);
					return $"Exception: {e.Message}";
				}
			}
			return "Operation Complete";
		}

		//TODO: Threads
		/// <summary>
		/// Goes through the message history of every channel on a server and gets all messages sent in the channel.
		/// Note this can be an expensive operation and should not be done commonly.
		/// </summary>
		/// <param name="context">The context from the command, used to get the server to reload</param>
		/// <returns></returns>
		public static async Task<string> UpdateMessageHistoryServer(ICommandContext context)
		{
			var exceptionsResult = new List<string>();
			Console.WriteLine("Reloading all server messages");
			//Old reactions must be removed since they have no identification of their own.
			var reactRemove = $"DELETE FROM reactions WHERE serverID = @serverID AND NOT isDeleted";
			var delRes = await DataLayerShortcut.ExecuteNonQuery(reactRemove, new MySqlParameter("@serverID", context.Guild.Id));
			if (delRes != String.Empty)
				return delRes;
			//Old emoji uses must be removed since they have no identification of their own.
			var emojiRemove = $"DELETE FROM emojiUses WHERE serverID = @serverID AND NOT isDeleted";
			delRes = await DataLayerShortcut.ExecuteNonQuery(emojiRemove, new MySqlParameter("@serverID", context.Guild.Id));
			if (delRes != String.Empty)
				return delRes;
			try
			{
				var channels = context.Guild.GetChannelsAsync().Result;
				foreach (var channel in channels)
				{
					try
					{
						Console.WriteLine($"Working on channel{channel.Name}/{channel.Id}");
						//Voice channels will be null obviously.
						if (channel is IMessageChannel messageChannel)
						{
							var messages = messageChannel.GetMessagesAsync(Int32.MaxValue);
							var cCount = 0;
							var totalMessages = await messages.SumAsync(m => m.Count);
							await foreach (var messageList in messages)
							{
								Console.WriteLine($"{Math.Round(((float)cCount / (float)totalMessages) * 100, 2)}% complete on current Channel");
								var mesRows = new List<string>();
								var attachRows = new List<string>();
								var reactionsRows = new List<string>();
								var emojiRows = new List<string>();
								foreach (var message in messageList)
								{
									++cCount;
									mesRows.Add($"({context.Guild.Id}, {message.Author.Id}, {messageChannel.Id}, {message.Id}, '{MySqlHelper.EscapeString(message.Content)}', '{MySqlHelper.EscapeString(message.Content)}', 0, {message.Timestamp.UtcDateTime:yyyyMMddHHmmss})");
									foreach (var attach in message.Attachments)
									{
										attachRows.Add($"({message.Id}, {attach.Id}, '{MySqlHelper.EscapeString(attach.Filename)}', {(attach.Height ?? -1)}, {(attach.Width ?? -1)}, '{MySqlHelper.EscapeString(attach.ProxyUrl)}', '{MySqlHelper.EscapeString(attach.Url)}', {attach.Size})");
									}
									if (message is RestUserMessage userMes && userMes.Reactions != null)
									{
										foreach (var reaction in userMes.Reactions)
										{
											var emote = reaction.Key as Emote;
											ulong? emoteId = emote?.Id;
											reactionsRows.Add($"({context.Guild.Id}, {userMes.Author.Id}, {userMes.Channel.Id}, {userMes.Id}, {(emoteId.HasValue ? emoteId : 0)}, '{reaction.Key.Name}')");
										}
									}
									CheckMessageForEmoji(context, message, messageChannel, ref emojiRows);
								}
								if (mesRows.Count > 0)
								{
									var mesQueryString = $"INSERT IGNORE messages (serverID, userID, channelID, messageID, rawText, mesText, mesStatus, mesTime) VALUES {string.Join(",", mesRows)}";
									var mesRes = await DataLayerShortcut.ExecuteNonQuery(mesQueryString);
									if (mesRes != String.Empty)
									{
										ErrorLog.WriteLog(mesRes);
										return mesRes;
									}
								}
								if (attachRows.Count > 0)
								{
									var attachQueryString = $"INSERT IGNORE attachments (messageID, attachID, fileName, height, width, proxyURL, attachURL, attachSize) VALUES {string.Join(",", attachRows)}";
									var mesRes = await DataLayerShortcut.ExecuteNonQuery(attachQueryString);
									if (mesRes != String.Empty)
									{
										ErrorLog.WriteLog(mesRes);
										return mesRes;
									}
								}
								if (reactionsRows.Count > 0)
								{
									var reactQueryString = $"INSERT reactions (serverID, userID, channelID, messageID, emojiID, emojiName) VALUES {string.Join(",", reactionsRows)}";
									var mesRes = await DataLayerShortcut.ExecuteNonQuery(reactQueryString);
									if (mesRes != String.Empty)
									{
										ErrorLog.WriteLog(mesRes);
										exceptionsResult.Add(mesRes);
									}
								}
								if (emojiRows.Count > 0)
								{
									var emojiQueryString = $"INSERT emojiUses (serverID, userID, channelID, messageID, emojiID, emojiName) VALUES {string.Join(",", emojiRows)}";
									var mesRes = await DataLayerShortcut.ExecuteNonQuery(emojiQueryString);
									if (mesRes != String.Empty)
									{
										ErrorLog.WriteLog(mesRes);
										exceptionsResult.Add(mesRes);
									}
								}
							}
						}
					}
					catch (Exception e) //Something something don't use exceptions for flow control something something.
					{
						if (e is AggregateException aE)
						{
							List<string> exceptions = new List<string>();
							foreach (var ex in aE.InnerExceptions)
							{
								exceptions.Add(ex.Message);
							}
							ErrorLog.WriteLog(string.Join(", ", exceptions));
							exceptionsResult.Add($"Exceptions: {string.Join(", ", exceptions)}");
						}
						else
						{
							ErrorLog.WriteError(e);
							exceptionsResult.Add($"Exception: {e.Message}");
						}
						continue;
					}
				}
			}
			catch (Exception e)
			{
				if (e is AggregateException aE)
				{
					List<string> exceptions = new List<string>();
					foreach (var ex in aE.InnerExceptions)
					{
						exceptions.Add(ex.Message);
					}
					ErrorLog.WriteLog(string.Join(", ", exceptions));
					return $"Exceptions: {string.Join(", ", exceptions)}";
				}
				else
				{
					ErrorLog.WriteError(e);
					return $"Exception: {e.Message}";
				}
			}
			if (exceptionsResult.Count > 0)
				return $"Operation complete with {exceptionsResult.Count} exception(s), Check log for details.";

			return "Operation Complete";
		}

		private static void CheckMessageForEmoji(ICommandContext context, IMessage message, IMessageChannel channel, ref List<string> rows)
		{
			var emojiRegex = new Regex(@"<a?:.*?:\d*?>");
			var Matches = emojiRegex.Matches(message.Content);
			var nameRegex = new Regex(@":(.*?):");
			var idRegex = new Regex(@":(\d*?)>");
			foreach (Match m in Matches)
			{
				try
				{
					//making a lot of assumptions here.
					var name = nameRegex.Match(m.Value).Groups[1].Value;
					ulong? emoteId = ulong.Parse(idRegex.Match(m.Value).Groups[1].Value);
					rows.Add($"({context.Guild.Id}, {message.Author.Id}, {channel.Id}, {message.Id}, {(emoteId.HasValue ? emoteId : 0)}, '{name}')");
				}
				catch (Exception e)
				{
					ErrorLog.WriteError(e);
					continue;
				}
			}
		}
	}
}
