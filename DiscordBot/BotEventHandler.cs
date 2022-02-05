using Discord;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using SGMessageBot.DataBase;
using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SGMessageBot.DiscordBot
{
	public static class BotEventHandler
	{
		#region Messages
		public static async Task ClientMessageReceived(SocketMessage e)
		{
			try
			{
				await BotEventProcessor.ProcessMessageReceived(e);
			}
			catch (Exception ex)
			{
				ErrorLog.WriteError(ex);
			}
		}

		public static async Task ClientMessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel arg3)
		{
			try
			{
				await BotEventProcessor.ProcessMessageUpdated(before, after);
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
			}
		}

		public static async Task ClientMessageDeleted(Cacheable<IMessage, ulong> mes, Cacheable<IMessageChannel, ulong> channel)
		{
			try
			{
				await BotEventProcessor.ProcessMessageDeleted(mes, channel);
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
			}
		}

		public static async Task ClientMessageBulkDeleted(IReadOnlyCollection<Cacheable<IMessage, ulong>> messages, Cacheable<IMessageChannel, ulong> channel)
		{
			try
			{
				await BotEventProcessor.ProcessMessagesBulkDeleted(messages, channel);
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
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
				ErrorLog.WriteError(ex);
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
				ErrorLog.WriteError(e);
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
				ErrorLog.WriteError(ex);
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
				ErrorLog.WriteError(e);
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
				ErrorLog.WriteError(e);
			}
		}

		public static async Task ClientUserLeft(SocketGuild guild, SocketUser user)
		{
			try
			{
				await BotEventProcessor.ProcessUserLeft(guild, user).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.WriteError(ex);
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
				ErrorLog.WriteError(e);
			}
		}

		public static async Task ClientServerUserUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
		{
			try
			{
				await BotEventProcessor.ProcessUserServerUpdated(after).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
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
				ErrorLog.WriteError(ex);
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
				ErrorLog.WriteError(e);
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
				ErrorLog.WriteError(ex);
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
				ErrorLog.WriteError(ex);
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
				ErrorLog.WriteError(e);
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
				ErrorLog.WriteError(ex);
			}
		}

		public static async Task ClientThreadCreated(SocketThreadChannel e)
		{
			try
			{
				await BotEventProcessor.ProcessThreadCreated(e).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.WriteError(ex);
			}
		}

		public static async Task ClientThreadUpdated(Cacheable<SocketThreadChannel, ulong> before, SocketThreadChannel after)
		{
			try
			{
				await BotEventProcessor.ProcessThreadUpdated(after).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.WriteError(ex);
			}
		}

		public static async Task ClientThreadDestroyed(Cacheable<SocketThreadChannel, ulong> e)
		{
			try
			{
				await BotEventProcessor.ProcessThreadDestroyed(e).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ErrorLog.WriteError(ex);
			}
		}
		#endregion
		#region Reactions
		public static async Task ClientReactionAdded(Cacheable<IUserMessage, ulong> mes, Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
		{
			try
			{
				await BotEventProcessor.ProcessReactionAdded(mes, channel, react);
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
			}
		}

		public static async Task ClientReactionRemoved(Cacheable<IUserMessage, ulong> mes, Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
		{
			try
			{
				await BotEventProcessor.ClientReactionRemoved(mes, channel, react);
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
			}
		}

		public static async Task ClientReactionsCleared(Cacheable<IUserMessage, ulong> mes, Cacheable<IMessageChannel, ulong> channel)
		{
			try
			{
				await BotEventProcessor.ClientReactionsCleared(mes, channel);
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
			}
		}

		public static async Task ClientReactionsEmoteRemoved(Cacheable<IUserMessage, ulong> mes, Cacheable<IMessageChannel, ulong> channel, IEmote emote)
		{
			try
			{
				await BotEventProcessor.ClientReactionsEmoteRemoved(mes, channel, emote);
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
			}
		}
		#endregion

		protected static class BotEventProcessor
		{
			#region Messages
			public static async Task ProcessMessageReceived(SocketMessage e)
			{
				AprilFools.Madoka2017(e);

				//For the pre Symphogear AXZ rewatch
				//if (e.Content.ToLower().Contains("!rewatch"))
				//{
				//	e.Channel.SendMessageAsync(OtherFunctions.SGRewatchNext());
				//}

				try
				{
					var Tasks = new List<Task>();
					var gChannel = e.Channel as SocketGuildChannel;
					var tChannel = e.Channel as SocketThreadChannel;
					if (gChannel != null || tChannel != null)
					{
						var guildId = gChannel?.Guild?.Id ?? tChannel.Guild.Id;
						var queryString = @"INSERT INTO messages (serverID, userID, channelID, messageID, rawText, mesText, mesStatus, mesTime, flags)
						VALUES (@serverID, @userID, @channelID, @messageID, @rawText, @mesText, @mesStatus, @mesTime, @flags)
						ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, channelID=@channelID, messageID=@messageID, 
						rawText=@rawText, mesText=@mesText, mesStatus=@mesStatus, mesTime=@mesTime";
						var res = await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", guildId), new MySqlParameter("@userID", e.Author.Id),
						new MySqlParameter("@channelID", e.Channel.Id), new MySqlParameter("@messageID", e.Id), new MySqlParameter("@rawText", e.Content),
						new MySqlParameter("@mesText", e.Content), new MySqlParameter("@mesStatus", 0), new MySqlParameter("@mesTime", e.Timestamp.UtcDateTime),
						new MySqlParameter("@flags", (int)e.Flags));

						if (!string.IsNullOrWhiteSpace(res))
							_ = Task.Run(() => DebugLog.WriteLog(DebugLogTypes.MissingUserId, () => $"Error in adding message: UserId: {e.Author.Id}, ChannelId: {e.Channel.Id}, Id: {e.Id}"));

						queryString = @"UPDATE usersinservers SET mesCount = mesCount+1 WHERE userID=@userID AND serverID=@serverID";
						Tasks.Add(DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", guildId), new MySqlParameter("@userID", e.Author.Id)));

						foreach (var attach in e.Attachments)
						{
							queryString = @"INSERT INTO attachments (messageID, attachID, fileName, height, width, proxyURL, attachURL, attachSize)
							VALUES (@messageID, @attachID, @fileName, @height, @width, @proxyURL, @attachURL, @attachSize)
							ON DUPLICATE KEY UPDATE messageID=@messageID, attachID=@attachID, fileName=@fileName, height=@height, 
							width=@width, proxyURL=@proxyURL, attachURL=@attachURL, attachSize=@attachSize";
							await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@messageID", e.Id), new MySqlParameter("@attachID", attach.Id),
							new MySqlParameter("@fileName", attach.Filename), new MySqlParameter("@height", attach.Height), new MySqlParameter("@width", attach.Width),
							new MySqlParameter("@proxyURL", attach.ProxyUrl), new MySqlParameter("@attachURL", attach.Url), new MySqlParameter("@attachSize", attach.Size));
						}
						Tasks.Add(CheckMessageForEmoji(e, e.Channel.Id, guildId));
						Tasks.Add(OtherFunctions.SendMessageTrack(gChannel?.Guild ?? tChannel?.Guild));

						foreach (var sticker in e.Stickers)
						{
							queryString = @"INSERT INTO stickerUses (serverID, userID, channelID, messageID, stickerID, stickerName, formatType, isDeleted)
							VALUES (@serverID, @userID, @channelID, @messageID, @stickerID, @stickerName, @formatType, @isDeleted)";
							await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", guildId), new MySqlParameter("@userID", e.Author.Id),
							new MySqlParameter("@channelID", e.Channel.Id), new MySqlParameter("@messageID", e.Id), new MySqlParameter("@stickerID", sticker.Id),
							new MySqlParameter("@stickerName", sticker.Name), new MySqlParameter("@formatType", (int)sticker.Format), new MySqlParameter("@isDeleted", false));
						}

						await Task.WhenAll(Tasks);
					}
				}
				catch (Exception ex)
				{
					ErrorLog.WriteLog($"Error in adding message: UserId: {e.Author.Id}, ChannelId: {e.Channel.Id}, Id: {e.Id}");
					ErrorLog.WriteError(ex);
				}
			}

			public static async Task ProcessMessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after)
			{
				try
				{
					var gChannel = after.Channel as SocketGuildChannel;
					var tChannel = after.Channel as SocketThreadChannel;
					if (gChannel != null || tChannel != null)
					{
						var guildId = gChannel?.Guild?.Id ?? tChannel.Guild.Id;
						DateTime? editedDateTime = after.EditedTimestamp.HasValue ? ((after.EditedTimestamp.Value.UtcDateTime) as DateTime?) : null;
						var queryString = @"INSERT INTO messages (serverID, userID, channelID, messageID, mesText, rawText, editedRawText, editedMesText, mesStatus, mesEditedTime, flags)
						VALUES (@serverID, @userID, @channelID, @messageID, @mesText, @rawText, @editedRawText, @editedMesText, @mesStatus, @mesEditedTime, @flags)
						ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, channelID=@channelID, messageID=@messageID, 
						editedRawText=@editedRawText, editedMesText=@editedMesText, mesStatus=@mesStatus, mesEditedTime=@mesEditedTime";
						var res = await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", guildId), new MySqlParameter("@userID", after.Author.Id),
						new MySqlParameter("@channelID", after.Channel.Id), new MySqlParameter("@messageID", after.Id), new MySqlParameter("@editedRawText", after.Content),
						new MySqlParameter("@editedMesText", after.Content), new MySqlParameter("@mesStatus", 0), new MySqlParameter("@mesEditedTime", editedDateTime),
						new MySqlParameter("@mesText", before.HasValue ? before.Value.Content : ""), new MySqlParameter("@rawText", before.HasValue ? before.Value.Content : ""),
						new MySqlParameter("@flags", (int)after.Flags));

						if (!string.IsNullOrWhiteSpace(res))
							_ = Task.Run(() => DebugLog.WriteLog(DebugLogTypes.MissingUserId, () => $"Error in updating message: UserId: {after.Author.Id}, ChannelId: {after.Channel.Id}, Id: {after.Id}"));

						foreach (var attach in after.Attachments)
						{
							queryString = @"INSERT INTO attachments (messageID, attachID, fileName, height, width, proxyURL, attachURL, attachSize)
							VALUES (@messageID, @attachID, @fileName, @height, @width, @proxyURL, @attachURL, @attachSize)
							ON DUPLICATE KEY UPDATE messageID=@messageID, attachID=@attachID, fileName=@fileName, height=@height, 
							width=@width, proxyURL=@proxyURL, attachURL=@attachURL, attachSize=@attachSize";
							await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@messageID", after.Id), new MySqlParameter("@attachID", attach.Id),
							new MySqlParameter("@fileName", attach.Filename), new MySqlParameter("@height", attach.Height), new MySqlParameter("@width", attach.Width),
							new MySqlParameter("@proxyURL", attach.ProxyUrl), new MySqlParameter("@attachURL", attach.Url), new MySqlParameter("@attachSize", attach.Size));
						}

						//Delete sticker uses and readd them in case they changed.
						queryString = "UPDATE stickerUses SET isDeleted=@isDeleted WHERE serverId=@serverID AND messageId=@messageID";
						await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@serverId", guildId), new MySqlParameter("@messageID", after.Id));

						foreach (var sticker in after.Stickers)
						{
							queryString = @"INSERT INTO stickerUses (serverID, userID, channelID, messageID, stickerID, stickerName, formatType, isDeleted)
							VALUES (@serverID, @userID, @channelID, @messageID, @stickerID, @stickerName, @formatType, @isDeleted)";
							await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", gChannel.Guild.Id), new MySqlParameter("@userID", after.Author.Id),
							new MySqlParameter("@channelID", after.Channel.Id), new MySqlParameter("@messageID", after.Id), new MySqlParameter("@stickerID", sticker.Id),
							new MySqlParameter("@stickerName", sticker.Name), new MySqlParameter("@formatType", (int)sticker.Format), new MySqlParameter("@isDeleted", false));
						}
					}
				}
				catch (Exception ex)
				{
					ErrorLog.WriteLog($"Error in updating message: UserId: {after.Author.Id}, ChannelId: {after.Channel.Id}, Id: {after.Id}");
					ErrorLog.WriteError(ex);
				}
			}

			public static async Task ProcessMessageDeleted(Cacheable<IMessage, ulong> mes, Cacheable<IMessageChannel, ulong> channel)
			{
				var Tasks = new List<Task>();
				try
				{
					var queryString = "UPDATE messages SET isDeleted=@isDeleted WHERE messageID=@messageID";
					Tasks.Add(DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", mes.Id)));
					queryString = "UPDATE attachments SET isDeleted=@isDeleted WHERE messageID=@messageID";
					Tasks.Add(DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", mes.Id)));
					queryString = "UPDATE reactions SET isDeleted=@isDeleted WHERE messageID=@messageID";
					Tasks.Add(DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", mes.Id)));
					queryString = "UPDATE emojiUses SET isDeleted=@isDeleted WHERE messageID=@messageID";
					Tasks.Add(DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", mes.Id)));
					queryString = "UPDATE stickerUses SET isDeleted=@isDeleted WHERE messageID=@messageID";
					Tasks.Add(DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", mes.Id)));
					if (mes.HasValue && channel.HasValue && channel.Value is SocketGuildChannel gChannel)
					{
						queryString = @"UPDATE usersinservers SET mesCount = mesCount-1 WHERE userID=@userID AND serverID=@serverID";
						Tasks.Add(DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", gChannel.Guild.Id), new MySqlParameter("@userID", mes.Value.Author.Id)));
					}
					else if (mes.HasValue && channel.HasValue && channel.Value is SocketThreadChannel tChannel)
					{
						queryString = @"UPDATE usersinservers SET mesCount = mesCount-1 WHERE userID=@userID AND serverID=@serverID";
						Tasks.Add(DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", tChannel.Guild.Id), new MySqlParameter("@userID", mes.Value.Author.Id)));
					}
				}
				catch (Exception ex)
				{
					ErrorLog.WriteError(ex);
				}
				await Task.WhenAll(Tasks.ToArray());
			}

			public static async Task ProcessMessagesBulkDeleted(IReadOnlyCollection<Cacheable<IMessage, ulong>> messages, Cacheable<IMessageChannel, ulong> channel)
			{
				//Im hoping this is reasonable?
				using var sem = new SemaphoreSlim(5);
				var tasks = messages.Select(async message =>
				{
					await sem.WaitAsync();
					try
					{
						await ProcessMessageDeleted(message, channel);
					}
					finally
					{
						sem.Release();
					}
				});
				await Task.WhenAll(tasks);
			}
			#endregion
			#region Server
			public static async Task ProcessJoinedServer(SocketGuild e)
			{
				await BotExamineServers.UpdateDatabaseServer(e);
			}
			public static async Task ProcessServerUpdated(SocketGuild before, SocketGuild after)
			{
				//A server can't have a empty name or no users so if this happens assume its a bad update and dont do it
				if (string.IsNullOrWhiteSpace(after.Name) || after.Users.Count == 0)
					return;
				var queryString = @"INSERT INTO servers (serverID, ownerID, serverName, userCount, channelCount, roleCount, regionID, createdDate)
				VALUES(@serverID, @ownerID, @serverName, @userCount, @channelCount, @roleCount, @regionID, @createdDate)
				ON DUPLICATE KEY UPDATE serverID=@serverID, ownerID=@ownerID, serverName=@serverName, userCount=@userCount, channelCount=@channelCount, roleCount=@roleCount,
				regionID=@regionID, createdDate=@createdDate";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", after.Id), new MySqlParameter("@ownerID", after.OwnerId),
				new MySqlParameter("@serverName", after.Name), new MySqlParameter("@userCount", after.Users.Count), new MySqlParameter("@channelCount", after.Channels.Count),
				new MySqlParameter("@roleCount", after.Roles.Count), new MySqlParameter("@regionID", after.VoiceRegionId), new MySqlParameter("@createdDate", after.CreatedAt.UtcDateTime));
			}
			#endregion
			#region User
			public static async Task ProcessUserJoined(SocketGuildUser e)
			{
				var queryString = @"INSERT INTO users (userID, userName, mention, isBot, isWebHook)
				VALUES(@userID, @userName, @mention, @isBot, @isWebHook)
				ON DUPLICATE KEY UPDATE userID=@userID, userName=@userName, mention=@mention, isBot=@isBot, isWebHook=@isWebHook";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@userID", e.Id), new MySqlParameter("@userName", e.Username),
				new MySqlParameter("@mention", e.Mention.Replace("!", String.Empty)), new MySqlParameter("@isBot", e.IsBot), new MySqlParameter("@isWebHook", e.IsWebhook));

				var roleIds = new List<ulong>();
				foreach (var role in e.Roles)
					roleIds.Add(role.Id);

				DateTime? joinedAtDateTime = e.JoinedAt.HasValue ? ((e.JoinedAt.Value.UtcDateTime) as DateTime?) : null;
				queryString = @"INSERT INTO usersInServers (serverID, userID, discriminator, nickName, nickNameMention, joinedDate, avatarID, avatarUrl, lastOnline, isDeleted, roleIds, mesCount)
				VALUES(@serverID, @userID, @discriminator, @nickName, @nickNameMention, @joinedDate, @avatarID, @avatarUrl, @lastOnline, @isDeleted, @roleIds, @mesCount)
				ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, discriminator=@discriminator, nickName=@nickName, nickNameMention=@nickNameMention, 
				joinedDate=@joinedDate, avatarID=@avatarID, avatarUrl=@avatarUrl, lastOnline=@lastOnline, isDeleted=@isDeleted, roleIDs=@roleIds";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Guild.Id), new MySqlParameter("@userID", e.Id),
				new MySqlParameter("@discriminator", e.Discriminator), new MySqlParameter("@nickName", e.Nickname), new MySqlParameter("@nickNameMention", e.Mention.Replace("!", String.Empty)),
				new MySqlParameter("@joinedDate", joinedAtDateTime), new MySqlParameter("@avatarID", e.AvatarId), new MySqlParameter("@avatarUrl", e.GetAvatarUrl()),
				new MySqlParameter("@lastOnline", joinedAtDateTime), new MySqlParameter("@isDeleted", false), new MySqlParameter("@roleIds", JsonConvert.SerializeObject(roleIds)), new MySqlParameter("@mesCount", value: 0));
			}

			public static async Task ProcessUserBannedStatus(SocketUser u, SocketGuild s, bool banned)
			{
				var queryString = "UPDATE usersInServers SET isBanned=@isBanned WHERE userID=@userID AND serverID=@serverID";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isBanned", banned), new MySqlParameter("@userID", u.Id),
				new MySqlParameter("@serverID", s.Id));
			}

			public static async Task ProcessUserLeft(SocketGuild guild, SocketUser user)
			{
				var queryString = "UPDATE usersInServers SET isDeleted=@isDeleted WHERE userID=@userID AND serverID=@serverID";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@userID", user.Id),
				new MySqlParameter("@serverID", guild.Id));
			}

			public static async Task ProcessUserUpdated(SocketUser before, SocketUser after)
			{
				var queryString = @"INSERT INTO users (userID, userName, mention, isBot, isWebHook)
				VALUES(@userID, @userName, @mention, @isBot, isWebHook=@isWebHook)
				ON DUPLICATE KEY UPDATE userID=@userID, userName=@userName, mention=@mention, isBot=@isBot";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@userID", after.Id), new MySqlParameter("@userName", after.Username),
				new MySqlParameter("@mention", after.Mention.Replace("!", String.Empty)), new MySqlParameter("@isBot", after.IsBot), new MySqlParameter("@isWebHook", after.IsWebhook));
			}

			public static async Task ProcessUserServerUpdated(SocketGuildUser after)
			{
				var roleIds = new List<ulong>();
				foreach (var role in after.Roles)
					roleIds.Add(role.Id);

				DateTime? joinedAtDateTime = after.JoinedAt.HasValue ? ((after.JoinedAt.Value.UtcDateTime) as DateTime?) : null;
				var queryString = @"INSERT INTO usersInServers (serverID, userID, discriminator, nickName, nickNameMention, joinedDate, avatarID, avatarUrl, lastOnline, roleIds, mesCount)
				VALUES(@serverID, @userID, @discriminator, @nickName, @nickNameMention, @joinedDate, @avatarID, @avatarUrl, @lastOnline, @roleIds, @mesCount)
				ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, discriminator=@discriminator, nickName=@nickName, nickNameMention=@nickNameMention,
				joinedDate=@joinedDate, avatarID=@avatarID, avatarUrl=@avatarUrl, lastOnline=@lastOnline, roleIDs=@roleIds";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", after.Guild.Id), new MySqlParameter("@userID", after.Id),
				new MySqlParameter("@discriminator", after.Discriminator), new MySqlParameter("@nickName", after.Nickname), new MySqlParameter("@nickNameMention", after.Mention.Replace("!", String.Empty)),
				new MySqlParameter("@joinedDate", joinedAtDateTime), new MySqlParameter("@avatarID", after.AvatarId), new MySqlParameter("@avatarUrl", after.GetAvatarUrl()),
				new MySqlParameter("@lastOnline", joinedAtDateTime), new MySqlParameter("@roleIds", JsonConvert.SerializeObject(roleIds)), new MySqlParameter("@mesCount", value: 0));
			}
			#endregion
			#region Role
			public static async Task ProcessRoleCreated(SocketRole e)
			{
				var queryString = @"INSERT INTO roles (serverID, roleID, roleName, roleColor, roleMention, isEveryone, isDeleted)
				VALUES(@serverID, @roleID, @roleName, @roleColor, @roleMention, @isEveryone, @isDeleted)
				ON DUPLICATE KEY UPDATE serverID=@serverID, roleID=@roleID, roleName=@roleName, roleColor=@roleColor, roleMention=@roleMention, isEveryone=@isEveryone,
				isDeleted=@isDeleted";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Guild.Id), new MySqlParameter("@roleID", e.Id),
				new MySqlParameter("@roleName", e.Name), new MySqlParameter("@roleColor", e.Color.ToString()), new MySqlParameter("@roleMention", e.Mention),
				new MySqlParameter("@isEveryone", e.IsEveryone), new MySqlParameter("@isDeleted", false));
			}

			public static async Task ProcessRoleUpdated(SocketRole before, SocketRole after)
			{
				var queryString = @"INSERT INTO roles (serverID, roleID, roleName, roleColor, roleMention, isEveryone)
				VALUES(@serverID, @roleID, @roleName, @roleColor, @roleMention, @isEveryone)
				ON DUPLICATE KEY UPDATE serverID=@serverID, roleID=@roleID, roleName=@roleName, roleColor=@roleColor, roleMention=@roleMention, isEveryone=@isEveryone";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", after.Guild.Id), new MySqlParameter("@roleID", after.Id),
				new MySqlParameter("@roleName", after.Name), new MySqlParameter("@roleColor", after.Color.ToString()), new MySqlParameter("@roleMention", after.Mention),
				new MySqlParameter("@isEveryone", after.IsEveryone));
			}

			public static async Task ProcessRoleDeleted(SocketRole e)
			{
				var queryString = "UPDATE roles SET isDeleted=@isDeleted WHERE roleID=@roleID";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@roleID", e.Id));
			}
			#endregion
			#region Channel
			public static async Task ProcessChannelCreated(SocketChannel e)
			{
				if (e is SocketGuildChannel gChannel)
				{
					var gTChannel = gChannel as SocketTextChannel;
					var queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType, isDeleted)
					VALUES(@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType, @isDeleted)
					ON DUPLICATE KEY UPDATE serverID=@serverID, channelID=@channelID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition,
					channelType=@channelType, isDeleted=@isDeleted";
					await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", gChannel.Guild.Id), new MySqlParameter("@channelID", e.Id),
					new MySqlParameter("@channelMention", gTChannel?.Mention), new MySqlParameter("@channelName", gChannel.Name), new MySqlParameter("@channelPosition", gChannel.Position),
					new MySqlParameter("@channelType", gTChannel != null ? 0 : 2), new MySqlParameter("@isDeleted", false));
				}
			}

			public static async Task ProcessChannelUpdated(SocketChannel before, SocketChannel after)
			{
				if (after is SocketGuildChannel gChannel)
				{
					var gTChannel = gChannel as SocketTextChannel;
					var queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType)
					VALUES(@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType)
					ON DUPLICATE KEY UPDATE serverID=@serverID, channelID=@channelID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition,
					channelType=@channelType";
					await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", gChannel.Guild.Id), new MySqlParameter("@channelID", after.Id),
					new MySqlParameter("@channelMention", gTChannel?.Mention), new MySqlParameter("@channelName", gChannel.Name), new MySqlParameter("@channelPosition", gChannel.Position),
					new MySqlParameter("@channelType", gTChannel != null ? 0 : 2), new MySqlParameter("@isDeleted", false));
				}
			}

			public static async Task ProcessChannelDestroyed(SocketChannel e)
			{
				var queryString = "UPDATE channels SET isDeleted=@isDeleted WHERE channelID=@channelID";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@channelID", e.Id));
			}

			public static async Task ProcessThreadCreated(SocketThreadChannel e)
			{
				var queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType, isDeleted, threadChannelId)
					VALUES(@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType, @isDeleted, @threadChannelId)
					ON DUPLICATE KEY UPDATE serverID=@serverID, channelID=@channelID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition,
					channelType=@channelType, isDeleted=@isDeleted, threadChannelId=@threadChannelId";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Guild.Id), new MySqlParameter("@channelID", e.Id),
				new MySqlParameter("@channelMention", e?.Mention), new MySqlParameter("@channelName", e.Name), new MySqlParameter("@channelPosition", e.Position),
				new MySqlParameter("@channelType", 0), new MySqlParameter("@isDeleted", false), new MySqlParameter("@threadChannelId", e.ParentChannel.Id));
			}

			public static async Task ProcessThreadUpdated(SocketThreadChannel after)
			{
				var queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType, isDeleted, threadChannelId)
					VALUES(@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType, @isDeleted, @threadChannelId)
					ON DUPLICATE KEY UPDATE serverID=@serverID, channelID=@channelID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition,
					channelType=@channelType, isDeleted=@isDeleted, threadChannelId=@threadChannelId";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", after.Guild.Id), new MySqlParameter("@channelID", after.Id),
				new MySqlParameter("@channelMention", after?.Mention), new MySqlParameter("@channelName", after.Name), new MySqlParameter("@channelPosition", after.Position),
				new MySqlParameter("@channelType", 0), new MySqlParameter("@isDeleted", false), new MySqlParameter("@threadChannelId", after.ParentChannel.Id));
			}

			public static async Task ProcessThreadDestroyed(Cacheable<SocketThreadChannel, ulong> e)
			{
				var queryString = "UPDATE channels SET isDeleted=@isDeleted WHERE channelID=@channelID";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@channelID", e.Id));
			}
			#endregion
			#region Reactions
			public static async Task ProcessReactionAdded(Cacheable<IUserMessage, ulong> mes, Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
			{
				var emote = react.Emote as Emote;
				var c = await channel.GetOrDownloadAsync();
				var guildId = (c as SocketGuildChannel)?.Guild?.Id ?? (c as SocketThreadChannel)?.Guild?.Id;
				var queryString = @"INSERT INTO reactions (serverID, userID, channelID, messageID, emojiID, emojiName, isDeleted)
				VALUES(@serverID, @userID, @channelID, @messageID, @emojiID, @emojiName, @isDeleted)
				ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, channelID=@channelID, messageID=@messageID, emojiID=@emojiID, emojiName=@emojiName, isDeleted=@isDeleted";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", guildId), new MySqlParameter("@channelID", channel.Id),
				new MySqlParameter("@userID", react.UserId), new MySqlParameter("@messageID", react.MessageId), new MySqlParameter("@emojiID", (emote == null ? 0 : emote.Id)), new MySqlParameter("@emojiName", react.Emote.Name),
				new MySqlParameter("@isDeleted", false));
			}

			public static async Task ClientReactionRemoved(Cacheable<IUserMessage, ulong> mes, Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
			{
				var emote = react.Emote as Emote;
				var queryString = "UPDATE reactions SET isDeleted=@isDeleted WHERE messageID=@messageID AND emojiID=@emojiID AND userID = @userID";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", react.MessageId),
				new MySqlParameter("@emojiID", emote == null ? 0 : emote.Id), new MySqlParameter("@userID", react.UserId));
			}

			public static async Task ClientReactionsCleared(Cacheable<IUserMessage, ulong> mes, Cacheable<IMessageChannel, ulong> channel)
			{
				var queryString = "UPDATE reactions SET isDeleted=@isDeleted WHERE messageID=@messageID";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", mes.Id));
			}

			public static async Task ClientReactionsEmoteRemoved(Cacheable<IUserMessage, ulong> mes, Cacheable<IMessageChannel, ulong> channel, IEmote emote)
			{
				var queryString = "UPDATE reactions SET isDeleted=@isDeleted WHERE channelID=@channelID AND messageID=@messageID AND emojiName=@emojiName";
				await DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@channelID", channel.Id), new MySqlParameter("@messageID", mes.Id),
					new MySqlParameter("@emojiName", emote.Name));
			}
			#endregion
			#region Other Functions
			public static async Task CheckMessageForEmoji(SocketMessage e, ulong channelId, ulong guildId)
			{
				var emojiRegex = new Regex(@"<a?:.*?:\d*?>");
				var Matches = emojiRegex.Matches(e.Content);
				var emojiRows = new List<string>();
				var nameRegex = new Regex(@":(.*?):");
				var idRegex = new Regex(@":(\d*?)>");
				foreach (Match m in Matches)
				{
					try
					{
						//making a lot of assumptions here.
						var name = nameRegex.Match(m.Value).Groups[1].Value;
						ulong? emoteId = ulong.Parse(idRegex.Match(m.Value).Groups[1].Value);
						emojiRows.Add($"({guildId}, {e.Author.Id}, {channelId}, {e.Id}, {(emoteId.HasValue ? emoteId : 0)}, '{name}')");
					}
					catch (Exception ex)
					{
						ErrorLog.WriteError(ex);
						continue;
					}
				}
				if (emojiRows.Count > 0)
				{
					var emojiQueryString = $"INSERT emojiUses (serverID, userID, channelID, messageID, emojiID, emojiName) VALUES {string.Join(",", emojiRows)}";
					await DataLayerShortcut.ExecuteNonQuery(emojiQueryString);
				}
			}
			#endregion
		}
	}
}
