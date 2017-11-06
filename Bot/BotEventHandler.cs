using Discord;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using SGMessageBot.DataBase;
using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	public static class BotEventHandler
	{
		#region Messages
		public static async Task ClientMessageReceived(SocketMessage e)
		{
			try
			{
				await BotEventProcessor.ProcessMessageReceived(e).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.writeLog(ex.Message);
			}
		}

		public static async Task ClientMessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel arg3)
		{
			try
			{
				await BotEventProcessor.ProcessMessageUpdated(before, after).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}

		public static async Task ClientMessageDeleted(Cacheable<IMessage, ulong> mes, ISocketMessageChannel arg2)
		{
			try
			{
				await BotEventProcessor.ProcessMessageDeleted(mes).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}
		#endregion
		#region Server
		public static async Task ClientJoinedServer(SocketGuild e)
		{
			try
			{
				await BotEventProcessor.ProcessJoinedServer(e).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.writeLog(ex.Message);
			}
		}
		public static async Task ClientServerUpdated(SocketGuild before, SocketGuild after)
		{
			try
			{
				await BotEventProcessor.ProcessServerUpdated(before, after).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}
		#endregion
		#region User
		public static async Task ClientUserJoined(SocketGuildUser e)
		{
			try
			{
				await BotEventProcessor.ProcessUserJoined(e).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.writeLog(ex.Message);
			}
		}

		public static async Task ClientUserUnbanned(SocketUser u, SocketGuild s)
		{
			try
			{
				await BotEventProcessor.ProcessUserBannedStatus(u, s, false).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}

		public static async Task ClientUserBanned(SocketUser u, SocketGuild s)
		{
			try
			{
				await BotEventProcessor.ProcessUserBannedStatus(u, s, true).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}

		public static async Task ClientUserLeft(SocketGuildUser e)
		{
			try
			{
				await BotEventProcessor.ProcessUserLeft(e).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.writeLog(ex.Message);
			}
		}

		public static async Task ClientUserUpdated(SocketUser before, SocketUser after)
		{
			try
			{
				await BotEventProcessor.ProcessUserUpdated(before, after).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}

		public static async Task ClientServerUserUpdated(SocketGuildUser before, SocketGuildUser after)
		{
			try
			{
				await BotEventProcessor.ProcessUserServerUpdated(before, after).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}
		#endregion
		#region Role
		public static async Task ClientRoleCreated(SocketRole e)
		{
			try
			{
				await BotEventProcessor.ProcessRoleCreated(e).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.writeLog(ex.Message);
			}
		}

		public static async Task ClientRoleUpdated(SocketRole before, SocketRole after)
		{
			try
			{
				await BotEventProcessor.ProcessRoleUpdated(before, after).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}
		
		public static async Task ClientRoleDeleted(SocketRole e)
		{
			try
			{
				await BotEventProcessor.ProcessRoleDeleted(e).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.writeLog(ex.Message);
			}
		}
		#endregion
		#region Channel
		public static async Task ClientChannelCreated(SocketChannel e)
		{
			try
			{
				await BotEventProcessor.ProcessChannelCreated(e).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.writeLog(ex.Message);
			}
		}

		public static async Task ClientChannelUpdated(SocketChannel before, SocketChannel after)
		{
			try
			{
				await BotEventProcessor.ProcessChannelUpdated(before, after).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}

		public static async Task ClientChannelDestroyed(SocketChannel e)
		{
			try
			{
				await BotEventProcessor.ProcessChannelDestroyed(e).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.writeLog(ex.Message);
			}
		}
		#endregion
		#region Reactions
		public static async Task ClientReactionAdded(Cacheable<IUserMessage, ulong> mes, ISocketMessageChannel channel, SocketReaction react)
		{
			try
			{
				await BotEventProcessor.ProcessReactionAdded(mes, channel, react).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}

		public static async Task ClientReactionRemoved(Cacheable<IUserMessage, ulong> mes, ISocketMessageChannel channel, SocketReaction react)
		{
			try
			{
				await BotEventProcessor.ClientReactionRemoved(mes, channel, react).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}

		public static async Task ClientReactionsCleared(Cacheable<IUserMessage, ulong> mes, ISocketMessageChannel channel)
		{
			try
			{
				await BotEventProcessor.ClientReactionsCleared(mes, channel).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}
		#endregion

		protected static class BotEventProcessor
		{
			#region Messages
			public static async Task ProcessMessageReceived(SocketMessage e)
			{
				//From April Fools Day 2017
				//Regex reg = new Regex("madoka.*\\?");
				//Regex reg1 = new Regex("why.*madoka");
				//if (reg.IsMatch(e.Content.ToLower()) || reg1.IsMatch(e.Content.ToLower()))
				//{
				//	e.Channel.SendMessageAsync("*Check the date*");
				//}

				//For the pre Symphogear AXZ rewatch
				//if (e.Content.ToLower().Contains("!rewatch"))
				//{
				//	e.Channel.SendMessageAsync(OtherFunctions.SGRewatchNext());
				//}

				try
				{
					var gChannel = e.Channel as SocketGuildChannel;
					if (gChannel != null)
					{
						var queryString = @"INSERT INTO messages (serverID, userID, channelID, messageID, rawText, mesText, mesStatus, mesTime)
						VALUES (@serverID, @userID, @channelID, @messageID, @rawText, @mesText, @mesStatus, @mesTime)
						ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, channelID=@channelID, messageID=@messageID, 
						rawText=@rawText, mesText=@mesText, mesStatus=@mesStatus, mesTime=@mesTime";
						DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", gChannel.Guild.Id), new MySqlParameter("@userID", e.Author.Id),
						new MySqlParameter("@channelID", e.Channel.Id), new MySqlParameter("@messageID", e.Id), new MySqlParameter("@rawText", e.Content),
						new MySqlParameter("@mesText", e.Content), new MySqlParameter("@mesStatus", 0), new MySqlParameter("@mesTime", e.Timestamp.UtcDateTime));
						queryString = @"UPDATE usersinservers SET mesCount = mesCount+1 WHERE userID=@userID AND serverID=@serverID";
						DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", gChannel.Guild.Id), new MySqlParameter("@userID", e.Author.Id));

						foreach (var attach in e.Attachments)
						{
							queryString = @"INSERT INTO attachments (messageID, attachID, fileName, height, width, proxyURL, attachURL, attachSize)
							VALUES (@messageID, @attachID, @fileName, @height, @width, @proxyURL, @attachURL, @attachSize)
							ON DUPLICATE KEY UPDATE messageID=@messageID, attachID=@attachID, fileName=@fileName, height=@height, 
							width=@width, proxyURL=@proxyURL, attachURL=@attachURL, attachSize=@attachSize";
							DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@messageID", e.Id), new MySqlParameter("@attachID", attach.Id),
							new MySqlParameter("@fileName", attach.Filename), new MySqlParameter("@height", attach.Height), new MySqlParameter("@width", attach.Width),
							new MySqlParameter("@proxyURL", attach.ProxyUrl), new MySqlParameter("@attachURL", attach.Url), new MySqlParameter("@attachSize", attach.Size));
						}
						checkMessageForEmoji(e, gChannel);
						OtherFunctions.sendMessageTrack(gChannel.Guild);
					}	
				}
				catch (Exception ex)
				{
					ErrorLog.writeLog(ex.Message);
				}
			}

			public static async Task ProcessMessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after)
			{
				try
				{
					var gChannel = after.Channel as SocketGuildChannel;
					//var channelServer = after.Discord.Guilds.FirstOrDefault(s => s.GetChannel(after.Channel.Id) != null);
					if (gChannel != null)
					{
						DateTime? editedDateTime = after.EditedTimestamp.HasValue ? ((after.EditedTimestamp.Value.UtcDateTime) as DateTime?) : null;
						var queryString = @"INSERT INTO messages (serverID, userID, channelID, messageID, mesText, rawText, editedRawText, editedMesText, mesStatus, mesEditedTime)
						VALUES (@serverID, @userID, @channelID, @messageID, @mesText, @rawText, @editedRawText, @editedMesText, @mesStatus, @mesEditedTime)
						ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, channelID=@channelID, messageID=@messageID, 
						editedRawText=@editedRawText, editedMesText=@editedMesText, mesStatus=@mesStatus, mesEditedTime=@mesEditedTime";
						DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", gChannel.Guild.Id), new MySqlParameter("@userID", after.Author.Id),
						new MySqlParameter("@channelID", after.Channel.Id), new MySqlParameter("@messageID", after.Id), new MySqlParameter("@editedRawText", after.Content),
						new MySqlParameter("@editedMesText", after.Content), new MySqlParameter("@mesStatus", 0), new MySqlParameter("@mesEditedTime", editedDateTime),
						new MySqlParameter("@mesText", before.HasValue ? before.Value.Content : ""), new MySqlParameter("@rawText", before.HasValue ? before.Value.Content : ""));

						foreach (var attach in after.Attachments)
						{
							queryString = @"INSERT INTO attachments (messageID, attachID, fileName, height, width, proxyURL, attachURL, attachSize)
							VALUES (@messageID, @attachID, @fileName, @height, @width, @proxyURL, @attachURL, @attachSize)
							ON DUPLICATE KEY UPDATE messageID=@messageID, attachID=@attachID, fileName=@fileName, height=@height, 
							width=@width, proxyURL=@proxyURL, attachURL=@attachURL, attachSize=@attachSize";
							DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@messageID", after.Id), new MySqlParameter("@attachID", attach.Id),
							new MySqlParameter("@fileName", attach.Filename), new MySqlParameter("@height", attach.Height), new MySqlParameter("@width", attach.Width),
							new MySqlParameter("@proxyURL", attach.ProxyUrl), new MySqlParameter("@attachURL", attach.Url), new MySqlParameter("@attachSize", attach.Size));
						}
					}
				}
				catch (Exception e)
				{
					ErrorLog.writeLog(e.Message);
				}
			}

			public static async Task ProcessMessageDeleted(Cacheable<IMessage, ulong> mes)
			{
				try
				{
					var queryString = "UPDATE messages SET isDeleted=@isDeleted WHERE messageID=@messageID";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", mes.Id));
					queryString = "UPDATE attachments SET isDeleted=@isDeleted WHERE messageID=@messageID";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", mes.Id));
					queryString = "UPDATE reactions SET isDeleted=@isDeleted WHERE messageID=@messageID";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", mes.Id));
					queryString = "UPDATE emojiUses SET isDeleted=@isDeleted WHERE messageID=@messageID";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", mes.Id));
					if (mes.HasValue)
					{
						var gChannel = mes.Value.Channel as SocketGuildChannel;
						if (gChannel != null)
						{
							queryString = @"UPDATE usersinservers SET mesCount = mesCount-1 WHERE userID=@userID AND serverID=@serverID";
							DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", gChannel.Guild.Id), new MySqlParameter("@userID", mes.Value.Author.Id));
						}
					}
				}
				catch (Exception e)
				{
					ErrorLog.writeLog(e.Message);
				}
			}
			#endregion
			#region Server
			public static async Task ProcessJoinedServer(SocketGuild e)
			{
				await BotExamineServers.updateDatabaseServer(e);
			}
			public static async Task ProcessServerUpdated(SocketGuild before, SocketGuild after)
			{
				var queryString = @"INSERT INTO servers (serverID, ownerID, serverName, userCount, channelCount, roleCount, regionID, createdDate)
				VALUES(@serverID, @ownerID, @serverName, @userCount, @channelCount, @roleCount, @regionID, @createdDate)
				ON DUPLICATE KEY UPDATE serverID=@serverID, ownerID=@ownerID, serverName=@serverName, userCount=@userCount, channelCount=@channelCount, roleCount=@roleCount,
				regionID=@regionID, createdDate=@createdDate";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", after.Id), new MySqlParameter("@ownerID", after.OwnerId),
				new MySqlParameter("@serverName", after.Name), new MySqlParameter("@userCount", after.Users.Count), new MySqlParameter("@channelCount", after.Channels.Count),
				new MySqlParameter("@roleCount", after.Roles.Count), new MySqlParameter("@regionID", after.VoiceRegionId), new MySqlParameter("@createdDate", after.CreatedAt.UtcDateTime));
			}
			#endregion
			#region User
			public static async Task ProcessUserJoined(SocketGuildUser e)
			{
				var queryString = @"INSERT INTO users (userID, userName, mention, isBot)
				VALUES(@userID, @userName, @mention, @isBot)
				ON DUPLICATE KEY UPDATE userID=@userID, userName=@userName, mention=@mention, isBot=@isBot";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@userID", e.Id), new MySqlParameter("@userName", e.Username),
				new MySqlParameter("@mention", e.Mention.Replace("!", String.Empty)), new MySqlParameter("@isBot", e.IsBot));

				var roleIds = new List<ulong>();
				foreach (var role in e.Roles)
					roleIds.Add(role.Id);

				DateTime? joinedAtDateTime = e.JoinedAt.HasValue ? ((e.JoinedAt.Value.UtcDateTime) as DateTime?) : null;
				queryString = @"INSERT INTO usersInServers (serverID, userID, discriminator, nickName, nickNameMention, joinedDate, avatarID, avatarUrl, lastOnline, isDeleted)
				VALUES(@serverID, @userID, @discriminator, @nickName, @nickNameMention, @joinedDate, @avatarID, @avatarUrl, @lastOnline, @isDeleted)
				ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, discriminator=@discriminator, nickName=@nickName, nickNameMention=@nickNameMention, 
				joinedDate=@joinedDate, avatarID=@avatarID, avatarUrl=@avatarUrl, lastOnline=@lastOnline, isDeleted=@isDeleted, roleIDs=@roleIds";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Guild.Id), new MySqlParameter("@userID", e.Id),
				new MySqlParameter("@discriminator", e.Discriminator), new MySqlParameter("@nickName", e.Nickname), new MySqlParameter("@nickNameMention", e.Mention.Replace("!", String.Empty)),
				new MySqlParameter("@joinedDate", joinedAtDateTime), new MySqlParameter("@avatarID", e.AvatarId), new MySqlParameter("@avatarUrl", e.GetAvatarUrl()),
				new MySqlParameter("@lastOnline", joinedAtDateTime), new MySqlParameter("@isDeleted", false), new MySqlParameter("@roleIds", JsonConvert.SerializeObject(roleIds)));
			}

			public static async Task ProcessUserBannedStatus(SocketUser u, SocketGuild s, bool banned)
			{
				var queryString = "UPDATE usersInServers SET isBanned=@isBanned WHERE userID=@userID AND serverID=@serverID";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isBanned", banned), new MySqlParameter("@userID", u.Id),
				new MySqlParameter("@serverID", s.Id));
			}

			public static async Task ProcessUserLeft(SocketGuildUser e)
			{
				var queryString = "UPDATE usersInServers SET isDeleted=@isDeleted WHERE userID=@userID AND serverID=@serverID";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@userID", e.Id),
				new MySqlParameter("@serverID", e.Guild.Id));
			}

			public static async Task ProcessUserUpdated(SocketUser before, SocketUser after)
			{
				var queryString = @"INSERT INTO users (userID, userName, mention, isBot)
				VALUES(@userID, @userName, @mention, @isBot)
				ON DUPLICATE KEY UPDATE userID=@userID, userName=@userName, mention=@mention, isBot=@isBot";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@userID", after.Id), new MySqlParameter("@userName", after.Username),
				new MySqlParameter("@mention", after.Mention.Replace("!", String.Empty)), new MySqlParameter("@isBot", after.IsBot));
			}

			public static async Task ProcessUserServerUpdated(SocketGuildUser before, SocketGuildUser after)
			{
				var roleIds = new List<ulong>();
				foreach (var role in after.Roles)
					roleIds.Add(role.Id);

				DateTime? joinedAtDateTime = after.JoinedAt.HasValue ? ((after.JoinedAt.Value.UtcDateTime) as DateTime?) : null;
				var queryString = @"INSERT INTO usersInServers (serverID, userID, discriminator, nickName, nickNameMention, joinedDate, avatarID, avatarUrl, lastOnline)
				VALUES(@serverID, @userID, @discriminator, @nickName, @nickNameMention, @joinedDate, @avatarID, @avatarUrl, @lastOnline)
				ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, discriminator=@discriminator, nickName=@nickName, nickNameMention=@nickNameMention,
				joinedDate=@joinedDate, avatarID=@avatarID, avatarUrl=@avatarUrl, lastOnline=@lastOnline, roleIDs=@roleIds";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", after.Guild.Id), new MySqlParameter("@userID", after.Id),
				new MySqlParameter("@discriminator", after.Discriminator), new MySqlParameter("@nickName", after.Nickname), new MySqlParameter("@nickNameMention", after.Mention.Replace("!", String.Empty)),
				new MySqlParameter("@joinedDate", joinedAtDateTime), new MySqlParameter("@avatarID", after.AvatarId), new MySqlParameter("@avatarUrl", after.GetAvatarUrl()),
				new MySqlParameter("@lastOnline", joinedAtDateTime), new MySqlParameter("@roleIds", JsonConvert.SerializeObject(roleIds)));
			}
			#endregion
			#region Role
			public static async Task ProcessRoleCreated(SocketRole e)
			{
				var queryString = @"INSERT INTO roles (serverID, roleID, roleName, roleColor, roleMention, isEveryone, isDeleted)
				VALUES(@serverID, @roleID, @roleName, @roleColor, @roleMention, @isEveryone, @isDeleted)
				ON DUPLICATE KEY UPDATE serverID=@serverID, roleID=@roleID, roleName=@roleName, roleColor=@roleColor, roleMention=@roleMention, isEveryone=@isEveryone,
				isDeleted=@isDeleted";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Guild.Id), new MySqlParameter("@roleID", e.Id),
				new MySqlParameter("@roleName", e.Name), new MySqlParameter("@roleColor", e.Color.ToString()), new MySqlParameter("@roleMention", e.Mention),
				new MySqlParameter("@isEveryone", e.IsEveryone), new MySqlParameter("@isDeleted", false));
			}

			public static async Task ProcessRoleUpdated(SocketRole before, SocketRole after)
			{
				var queryString = @"INSERT INTO roles (serverID, roleID, roleName, roleColor, roleMention, isEveryone)
				VALUES(@serverID, @roleID, @roleName, @roleColor, @roleMention, @isEveryone)
				ON DUPLICATE KEY UPDATE serverID=@serverID, roleID=@roleID, roleName=@roleName, roleColor=@roleColor, roleMention=@roleMention, isEveryone=@isEveryone";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", after.Guild.Id), new MySqlParameter("@roleID", after.Id),
				new MySqlParameter("@roleName", after.Name), new MySqlParameter("@roleColor", after.Color.ToString()), new MySqlParameter("@roleMention", after.Mention),
				new MySqlParameter("@isEveryone", after.IsEveryone));
			}

			public static async Task ProcessRoleDeleted(SocketRole e)
			{
				var queryString = "UPDATE roles SET isDeleted=@isDeleted WHERE roleID=@roleID";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@roleID", e.Id));
			}
			#endregion
			#region Channel
			public static async Task ProcessChannelCreated(SocketChannel e)
			{
				var gChannel = e as SocketGuildChannel;
				//var channelServer = e.Discord.Guilds.FirstOrDefault(s => s.GetChannel(e.Id) != null);
				if (gChannel != null)
				{
					//var serverChannel = channelServer.GetChannel(e.Id);
					var gTChannel = gChannel as SocketTextChannel;
					var queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType, isDeleted)
					VALUES(@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType, @isDeleted)
					ON DUPLICATE KEY UPDATE serverID=@serverID, channelID=@channelID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition,
					channelType=@channelType, isDeleted=@isDeleted";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", gChannel.Guild.Id), new MySqlParameter("@channelID", e.Id),
					new MySqlParameter("@channelMention", gTChannel != null ? gTChannel.Mention : null), new MySqlParameter("@channelName", gChannel.Name), new MySqlParameter("@channelPosition", gChannel.Position),
					new MySqlParameter("@channelType", gTChannel != null ? 0 : 2), new MySqlParameter("@isDeleted", false));
				}
			}

			public static async Task ProcessChannelUpdated(SocketChannel before, SocketChannel after)
			{
				var gChannel = after as SocketGuildChannel;
				//var channelServer = after.Discord.Guilds.FirstOrDefault(s => s.GetChannel(after.Id) != null);
				if (gChannel != null)
				{
					//var serverChannel = channelServer.GetChannel(after.Id);
					var gTChannel = gChannel as SocketTextChannel;
					var queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType)
					VALUES(@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType)
					ON DUPLICATE KEY UPDATE serverID=@serverID, channelID=@channelID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition,
					channelType=@channelType";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", gChannel.Guild.Id), new MySqlParameter("@channelID", after.Id),
					new MySqlParameter("@channelMention", gTChannel != null ? gTChannel.Mention : null), new MySqlParameter("@channelName", gChannel.Name), new MySqlParameter("@channelPosition", gChannel.Position),
					new MySqlParameter("@channelType", gTChannel != null ? 0 : 2), new MySqlParameter("@isDeleted", false));
				}
			}

			public static async Task ProcessChannelDestroyed(SocketChannel e)
			{
				var queryString = "UPDATE channels SET isDeleted=@isDeleted WHERE channelID=@channelID";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@channelID", e.Id));
			}
			#endregion
			#region Reactions
			public static async Task ProcessReactionAdded(Cacheable<IUserMessage, ulong> mes, ISocketMessageChannel channel, SocketReaction react)
			{
				var emote = react.Emote as Emote;
				var queryString = @"INSERT INTO reactions (serverID, userID, channelID, messageID, emojiID, emojiName, isDeleted)
				VALUES(@serverID, @userID, @channelID, @messageID, @emojiID, @emojiName, @isDeleted)
				ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, channelID=@channelID, messageID=@messageID, emojiID=@emojiID, emojiName=@emojiName, isDeleted=@isDeleted";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", (channel as SocketTextChannel).Guild.Id), new MySqlParameter("@channelID", channel.Id),
				new MySqlParameter("@userID", react.UserId), new MySqlParameter("@messageID", react.MessageId), new MySqlParameter("@emojiID", (emote == null ? 0 : emote.Id)), new MySqlParameter("@emojiName", react.Emote.Name), 
				new MySqlParameter("@isDeleted", false));
			}

			public static async Task ClientReactionRemoved(Cacheable<IUserMessage, ulong> mes, ISocketMessageChannel channel, SocketReaction react)
			{
				var emote = react.Emote as Emote;
				var queryString = "UPDATE reactions SET isDeleted=@isDeleted WHERE messageID=@messageID AND emojiID=@emojiID AND userID = @userID";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", react.MessageId),
				new MySqlParameter("@emojiID", emote == null ? 0 : emote.Id), new MySqlParameter("@userID", react.UserId));
			}

			public static async Task ClientReactionsCleared(Cacheable<IUserMessage, ulong> mes, ISocketMessageChannel channel)
			{
				ulong messageId = 0;
				if (mes.HasValue)
					messageId = mes.Value.Id;
				var queryString = "UPDATE reactions SET isDeleted=@isDeleted WHERE messageID=@messageID";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", messageId));
			}
			#endregion
			#region Other Functions
			public static void checkMessageForEmoji(SocketMessage e, SocketGuildChannel g)
			{
				var Regex = new Regex(@"<:.*?:\d*?>");
				var Matches = Regex.Matches(e.Content);
				List<string> emojiRows = new List<string>();
				var nameRegex = new Regex(@"<:(.*?):");
				var idRegex = new Regex(@":(\d*?)>");
				foreach (Match m in Matches)
				{
					try
					{
						//making a lot of assumptions here.
						var name = nameRegex.Match(m.Value).Groups[1].Value;
						ulong? emoteId = ulong.Parse(idRegex.Match(m.Value).Groups[1].Value);
						emojiRows.Add($"({g.Guild.Id}, {e.Author.Id}, {g.Id}, {e.Id}, {(emoteId.HasValue ? emoteId : 0)}, '{name}')");
					}
					catch (Exception ex)
					{
						ErrorLog.writeLog(ex.Message);
						continue;
					}
				}
				if(emojiRows.Count > 0)
				{
					var emojiQueryString = $"INSERT emojiUses (serverID, userID, channelID, messageID, emojiID, emojiName) VALUES {string.Join(",", emojiRows)}";
					DataLayerShortcut.ExecuteNonQuery(emojiQueryString);
				}
			}
			#endregion
		}
	}
}
