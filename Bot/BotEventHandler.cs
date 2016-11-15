using Discord;
using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	public static class BotEventHandler
	{
		#region Messages
		public static async void ClientMessageReceived(object sender, MessageEventArgs e)
		{
			Console.WriteLine("client message received");
			try
			{
				await BotEventProcessor.ProcessMessageReceived(sender, e).ConfigureAwait(false);
			}
			catch { }
		}

		public static async void ClientMessageUpdated(object sender, MessageUpdatedEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessMessageUpdated(sender, e).ConfigureAwait(false);
			}
			catch { }
		}

		public static async void ClientMessageDeleted(object sender, MessageEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessMessageDeleted(sender, e).ConfigureAwait(false);
			}
			catch { }
		}
		#endregion
		#region Server
		public static async void ClientJoinedServer(object sender, ServerEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessJoinedServer(sender, e).ConfigureAwait(false);
			}
			catch { }
		}
		public static async void ClientServerUpdated(object sender, ServerUpdatedEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessServerUpdated(sender, e).ConfigureAwait(false);
			}
			catch { }
		}
		#endregion
		#region User
		public static async void ClientUserJoined(object sender, UserEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessUserJoined(sender, e).ConfigureAwait(false);
			}
			catch { }
		}

		public static async void ClientUserUnbanned(object sender, UserEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessUserBannedStatus(sender, e, false).ConfigureAwait(false);
			}
			catch { }
		}

		public static async void ClientUserBanned(object sender, UserEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessUserBannedStatus(sender, e, true).ConfigureAwait(false);
			}
			catch { }
		}

		public static async void ClientUserLeft(object sender, UserEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessUserLeft(sender, e).ConfigureAwait(false);
			}
			catch { }
		}

		public static async void ClientUserUpdated(object sender, UserUpdatedEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessUserUpdated(sender, e).ConfigureAwait(false);
			}
			catch { }
		}
		#endregion
		#region Role
		public static async void ClientRoleCreated(object sender, RoleEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessRoleCreated(sender, e).ConfigureAwait(false);
			}
			catch { }
		}

		public static async void ClientRoleUpdated(object sender, RoleUpdatedEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessRoleUpdated(sender, e).ConfigureAwait(false);
			}
			catch { }
		}

		public static async void ClientRoleDeleted(object sender, RoleEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessRoleDeleted(sender, e).ConfigureAwait(false);
			}
			catch { }
		}
		#endregion
		#region Channel
		public static async void ClientChannelCreated(object sender, ChannelEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessChannelCreated(sender, e).ConfigureAwait(false);
			}
			catch { }
		}

		public static async void ClientChannelUpdated(object sender, ChannelUpdatedEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessChannelUpdated(sender, e).ConfigureAwait(false);
			}
			catch { }
		}

		public static async void ClientChannelDestroyed(object sender, ChannelEventArgs e)
		{
			try
			{
				await BotEventProcessor.ProcessChannelDestroyed(sender, e).ConfigureAwait(false);
			}
			catch { }
		}
		#endregion

		protected static class BotEventProcessor
		{
			#region Messages
			public static async Task ProcessMessageReceived(object sender, MessageEventArgs e)
			{
				Console.WriteLine("process message received");
				try
				{
					var queryString = @"INSERT INTO messages (serverID, userID, channelID, messageID, rawText, mesText, mesStatus, mesTime)
					VALUES (@serverID, @userID, @channelID, @messageID, @rawText, @mesText, @mesStatus, @mesTime)
					ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, channelID=@channelID, messageID=@messageID, 
					rawText=@rawText, mesText=@mesText, mesStatus=@mesStatus, mesTime=@mesTime";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Server.Id), new MySqlParameter("@userID", e.User.Id),
					new MySqlParameter("@channelID", e.Channel.Id), new MySqlParameter("@messageID", e.Message.Id), new MySqlParameter("@rawText", e.Message.RawText),
					new MySqlParameter("@mesText", e.Message.Text), new MySqlParameter("@mesStatus", e.Message.State), new MySqlParameter("@mesTime", e.Message.Timestamp));

					foreach (var attach in e.Message.Attachments)
					{
						queryString = @"INSERT INTO attachments (messageID, attachID, fileName, height, width, proxyURL, attachURL, attachSize)
						VALUES (@messageID, @attachID, @fileName, @height, @width, @proxyURL, @attachURL, @attachSize)
						ON DUPLICATE KEY UPDATE messageID=@messageID, attachID=@attachID, fileName=@fileName, height=@height, 
						width=@width, proxyURL=@proxyURL, attachURL=@attachURL, attachSize=@attachSize";
						DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@messageID", e.Message.Id), new MySqlParameter("@attachID", attach.Id),
						new MySqlParameter("@fileName", attach.Filename), new MySqlParameter("@height", attach.Height), new MySqlParameter("@width", attach.Width),
						new MySqlParameter("@proxyURL", attach.ProxyUrl), new MySqlParameter("@attachURL", attach.Url), new MySqlParameter("@attachSize", attach.Size));
					}
				}
				catch { }
				Console.WriteLine("Processed Message");
			}

			public static async Task ProcessMessageUpdated(object sender, MessageUpdatedEventArgs e)
			{
				try
				{
					var queryString = @"INSERT INTO messages (serverID, userID, channelID, messageID, mesText, rawText, editedRawText, editedMesText, mesStatus, mesEditedTime)
					VALUES (@serverID, @userID, @channelID, @messageID, @mesText, @rawText, @editedRawText, @editedMesText, @mesStatus, @mesEditedTime)
					ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, channelID=@channelID, messageID=@messageID, 
					editedRawText=@editedRawText, editedMesText=@editedMesText, mesStatus=@mesStatus, mesEditedTime=@mesEditedTime";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Server.Id), new MySqlParameter("@userID", e.User.Id),
					new MySqlParameter("@channelID", e.Channel.Id), new MySqlParameter("@messageID", e.After.Id), new MySqlParameter("@editedRawText", e.After.RawText),
					new MySqlParameter("@editedMesText", e.After.Text), new MySqlParameter("@mesStatus", e.After.State), new MySqlParameter("@mesEditedTime", e.After.EditedTimestamp),
					new MySqlParameter("@mesText", e.Before.Text), new MySqlParameter("@rawText", e.Before.RawText));

					foreach (var attach in e.After.Attachments)
					{
						queryString = @"INSERT INTO attachments (messageID, attachID, fileName, height, width, proxyURL, attachURL, attachSize)
						VALUES (@messageID, @attachID, @fileName, @height, @width, @proxyURL, @attachURL, @attachSize)
						ON DUPLICATE KEY UPDATE messageID=@messageID, attachID=@attachID, fileName=@fileName, height=@height, 
						width=@width, proxyURL=@proxyURL, attachURL=@attachURL, attachSize=@attachSize";
						DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@messageID", e.After.Id), new MySqlParameter("@attachID", attach.Id),
						new MySqlParameter("@fileName", attach.Filename), new MySqlParameter("@height", attach.Height), new MySqlParameter("@width", attach.Width),
						new MySqlParameter("@proxyURL", attach.ProxyUrl), new MySqlParameter("@attachURL", attach.Url), new MySqlParameter("@attachSize", attach.Size));
					}
				}
				catch { }
			}

			public static async Task ProcessMessageDeleted(object sender, MessageEventArgs e)
			{
				try
				{
					var queryString = "UPDATE messages SET isDeleted=@isDeleted WHERE messageID=@messageID";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", e.Message.Id));
					queryString = "UPDATE attachments SET isDeleted=@isDeleted WHERE messageID=@messageID";
					DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@messageID", e.Message.Id));
				}
				catch { }
			}
			#endregion
			#region Server
			public static async Task ProcessJoinedServer(object sender, ServerEventArgs e)
			{
				await BotExamineServers.updateDatabaseServer(e.Server);
			}
			public static async Task ProcessServerUpdated(object sender, ServerUpdatedEventArgs e)
			{
				var queryString = @"INSERT INTO servers (serverID, ownerID, serverName, userCount, channelCount, roleCount, regionID, createdDate)
				VALUES(@serverID, @ownerID, @serverName, @userCount, @channelCount, @roleCount, @regionID, @createdDate)
				ON DUPLICATE KEY UPDATE serverID=@serverID, ownerID=@ownerID, serverName=@serverName, userCount=@userCount, channelCount=@channelCount, roleCount=@roleCount,
				regionID=@regionID, createdDate=@createdDate";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.After.Id), new MySqlParameter("@ownerID", e.After.Owner.Id),
				new MySqlParameter("@serverName", e.After.Name), new MySqlParameter("@userCount", e.After.UserCount), new MySqlParameter("@channelCount", e.After.ChannelCount),
				new MySqlParameter("@roleCount", e.After.RoleCount), new MySqlParameter("@regionID", e.After.Region.Id), new MySqlParameter("@createdDate", e.After.Owner.JoinedAt));
			}
			#endregion
			#region User
			public static async Task ProcessUserJoined(object sender, UserEventArgs e)
			{
				var queryString = @"INSERT INTO users (userID, userName, mention, isBot)
				VALUES(@userID, @userName, @mention, @isBot)
				ON DUPLICATE KEY UPDATE userID=@userID, userName=@userName, mention=@mention, isBot=@isBot";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@userID", e.User.Id), new MySqlParameter("@userName", e.User.Name),
				new MySqlParameter("@mention", e.User.Mention), new MySqlParameter("@isBot", e.User.IsBot));

				queryString = @"INSERT INTO usersInServers (serverID, userID, discriminator, nickName, nickNameMention, joinedDate, avatarID, avatarUrl, lastOnline, isDeleted)
				VALUES(@serverID, @userID, @discriminator, @nickName, @nickNameMention, @joinedDate, @avatarID, @avatarUrl, @lastOnline, @isDeleted)
				ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, discriminator=@discriminator, nickName=@nickName, nickNameMention=@nickNameMention, 
				joinedDate=@joinedDate, avatarID=@avatarID, avatarUrl=@avatarUrl, lastOnline=@lastOnline, isDeleted=@isDeleted";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Server.Id), new MySqlParameter("@userID", e.User.Id),
				new MySqlParameter("@discriminator", e.User.Discriminator), new MySqlParameter("@nickName", e.User.Nickname), new MySqlParameter("@nickNameMention", e.User.NicknameMention),
				new MySqlParameter("@joinedDate", e.User.JoinedAt), new MySqlParameter("@avatarID", e.User.AvatarId), new MySqlParameter("@avatarUrl", e.User.AvatarUrl),
				new MySqlParameter("@lastOnline", e.User.LastOnlineAt), new MySqlParameter("@isDeleted", false));
			}

			public static async Task ProcessUserBannedStatus(object sender, UserEventArgs e, bool banned)
			{
				var queryString = "UPDATE usersInServers SET isBanned=@isBanned WHERE userID=@userID";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isBanned", banned), new MySqlParameter("@userID", e.User.Id));
			}

			public static async Task ProcessUserLeft(object sender, UserEventArgs e)
			{
				var queryString = "UPDATE usersInServers SET isDeleted=@isDeleted WHERE userID=@userID";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@userID", e.User.Id));
			}

			public static async Task ProcessUserUpdated(object sender, UserUpdatedEventArgs e)
			{
				var queryString = @"INSERT INTO users (userID, userName, mention, isBot)
				VALUES(@userID, @userName, @mention, @isBot)
				ON DUPLICATE KEY UPDATE userID=@userID, userName=@userName, mention=@mention, isBot=@isBot";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@userID", e.After.Id), new MySqlParameter("@userName", e.After.Name),
				new MySqlParameter("@mention", e.After.Mention), new MySqlParameter("@isBot", e.After.IsBot));

				queryString = @"INSERT INTO usersInServers (serverID, userID, discriminator, nickName, nickNameMention, joinedDate, avatarID, avatarUrl, lastOnline)
				VALUES(@serverID, @userID, @discriminator, @nickName, @nickNameMention, @joinedDate, @avatarID, @avatarUrl, @lastOnline)
				ON DUPLICATE KEY UPDATE serverID=@serverID, userID=@userID, discriminator=@discriminator, nickName=@nickName, nickNameMention=@nickNameMention, 
				joinedDate=@joinedDate, avatarID=@avatarID, avatarUrl=@avatarUrl, lastOnline=@lastOnline";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Server.Id), new MySqlParameter("@userID", e.After.Id),
				new MySqlParameter("@discriminator", e.After.Discriminator), new MySqlParameter("@nickName", e.After.Nickname), new MySqlParameter("@nickNameMention", e.After.NicknameMention),
				new MySqlParameter("@joinedDate", e.After.JoinedAt), new MySqlParameter("@avatarID", e.After.AvatarId), new MySqlParameter("@avatarUrl", e.After.AvatarUrl),
				new MySqlParameter("@lastOnline", e.After.LastOnlineAt));
			}
			#endregion
			#region Role
			public static async Task ProcessRoleCreated(object sender, RoleEventArgs e)
			{
				var queryString = @"INSERT INTO roles (serverID, roleID, roleName, roleColor, roleMention, isEveryone, isDeleted)
				VALUES(@serverID, @roleID, @roleName, @roleColor, @roleMention, @isEveryone, @isDeleted)
				ON DUPLICATE KEY UPDATE serverID=@serverID, roleID=@roleID, roleName=@roleName, roleColor=@roleColor, roleMention=@roleMention, isEveryone=@isEveryone,
				isDeleted=@isDeleted";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Server.Id), new MySqlParameter("@roleID", e.Role.Id),
				new MySqlParameter("@roleName", e.Role.Name), new MySqlParameter("@roleColor", e.Role.Color.ToString()), new MySqlParameter("@roleMention", e.Role.Mention),
				new MySqlParameter("@isEveryone", e.Role.IsEveryone), new MySqlParameter("@isDeleted", false));
			}

			public static async Task ProcessRoleUpdated(object sender, RoleUpdatedEventArgs e)
			{
				var queryString = @"INSERT INTO roles (serverID, roleID, roleName, roleColor, roleMention, isEveryone)
				VALUES(@serverID, @roleID, @roleName, @roleColor, @roleMention, @isEveryone)
				ON DUPLICATE KEY UPDATE serverID=@serverID, roleID=@roleID, roleName=@roleName, roleColor=@roleColor, roleMention=@roleMention, isEveryone=@isEveryone";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Server.Id), new MySqlParameter("@roleID", e.After.Id),
				new MySqlParameter("@roleName", e.After.Name), new MySqlParameter("@roleColor", e.After.Color.ToString()), new MySqlParameter("@roleMention", e.After.Mention),
				new MySqlParameter("@isEveryone", e.After.IsEveryone));
			}

			public static async Task ProcessRoleDeleted(object sender, RoleEventArgs e)
			{
				var queryString = "UPDATE roles SET isDeleted=@isDeleted WHERE roleID=@roleID";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@roleID", e.Role.Id));
			}
			#endregion
			#region Channel
			public static async Task ProcessChannelCreated(object sender, ChannelEventArgs e)
			{
				var queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType, isDeleted)
				VALUES(@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType, @isDeleted)
				ON DUPLICATE KEY UPDATE serverID=@serverID, channelID=@channelID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition,
				channelType=@channelType, isDeleted=@isDeleted";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Server.Id), new MySqlParameter("@channelID", e.Channel.Id),
				new MySqlParameter("@channelMention", e.Channel.Mention), new MySqlParameter("@channelName", e.Channel.Name), new MySqlParameter("@channelPosition", e.Channel.Position),
				new MySqlParameter("@channelType", e.Channel.Type.Value), new MySqlParameter("@isDeleted", false));
			}

			public static async Task ProcessChannelUpdated(object sender, ChannelUpdatedEventArgs e)
			{
				var queryString = @"INSERT INTO channels (serverID, channelID, channelMention, channelName, channelPosition, channelType)
				VALUES(@serverID, @channelID, @channelMention, @channelName, @channelPosition, @channelType)
				ON DUPLICATE KEY UPDATE serverID=@serverID, channelID=@channelID, channelMention=@channelMention, channelName=@channelName, channelPosition=@channelPosition,
				channelType=@channelType";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@serverID", e.Server.Id), new MySqlParameter("@channelID", e.After.Id),
				new MySqlParameter("@channelMention", e.After.Mention), new MySqlParameter("@channelName", e.After.Name), new MySqlParameter("@channelPosition", e.After.Position),
				new MySqlParameter("@channelType", e.After.Type.Value), new MySqlParameter("@isDeleted", false));
			}

			public static async Task ProcessChannelDestroyed(object sender, ChannelEventArgs e)
			{
				var queryString = "UPDATE channels SET isDeleted=@isDeleted WHERE channelID=@channelID";
				DataLayerShortcut.ExecuteNonQuery(queryString, new MySqlParameter("@isDeleted", true), new MySqlParameter("@channelID", e.Channel.Id));
			}
			#endregion
		}
	}
}
