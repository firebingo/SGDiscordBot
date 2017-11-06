using Discord.Commands;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	public class BotCommandProcessor
	{
		private string dateFormat = "yyyy/MM/dd";

		#region Calc Functions
		public async Task<DateModel> getEarliestMessage(ICommandContext context)
		{
			var result = new DateModel();
			var queryString = "SELECT mesTime FROM messages WHERE serverID = @serverID AND mesTime IS NOT NULL ORDER BY mesTime LIMIT 1";
			DataLayerShortcut.ExecuteReader<DateModel>(readEarliestDate, result, queryString, new MySqlParameter("@serverID", context.Guild.Id));
			return Task.FromResult<DateModel>(result).Result;
		}

		public async Task<int> getTotalMessageCount(ICommandContext context)
		{
			int? result = 0;
			var queryString = "SELECT COUNT(*) FROM messages WHERE isDeleted = false AND serverID = @serverID";
			result = DataLayerShortcut.ExecuteScalarInt(queryString, new MySqlParameter("@serverID", context.Guild.Id));
			return Task.FromResult<int>(result.HasValue ? result.Value : 0).Result;
		}

		#region Admin Functions
		public async Task<string> calculateRoleCounts(SocketTextChannel channel, bool useMentions, ICommandContext context)
		{
			var result = "";
			try
			{
				Dictionary<ulong, int> counts = new Dictionary<ulong, int>();
				var roles = context.Guild.Roles;
				foreach (var role in roles)
				{
					counts.Add(role.Id, 0);
				}
				var users = await context.Guild.GetUsersAsync();
				foreach (var user in users)
				{
					foreach (var role in user.RoleIds)
					{
						if (counts.ContainsKey(role))
						{
							counts[role]++;
						}
					}
				}
				result = $"Current Role Counts: ({DateTime.UtcNow.ToString(dateFormat)})\n";
				counts = counts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
				foreach (var count in counts)
				{
					var role = context.Guild.GetRole(count.Key);
					if(role != null && role.Name != "@everyone" && count.Value > 0)
					{
						result += $"{(useMentions ? role.Mention : role.Name)}: {count.Value}\n";
					}
				}
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
				result = e.Message;
			}
			return Task.FromResult<string>(result).Result;
		}
		#endregion

		#region Message Counts
		public async Task<string> calculateTopMessageCounts(int count, ICommandContext context)
		{
			var result = "";
			var earliest = await getEarliestMessage(context);
			var totalCount = await getTotalMessageCount(context);
			var nextSplitLength = 2000;
			List<UserCountModel> results = new List<UserCountModel>();
			var queryString = "";
			if (count == 0)
				queryString = $@"SELECT usersinservers.userID, usersinservers.nickNameMention, usersinservers.mesCount FROM usersinservers 
				WHERE usersinservers.serverID=@serverID ORDER BY mesCount DESC LIMIT 1";
			else
				queryString = $@"SELECT usersinservers.userID, usersinservers.nickNameMention, usersinservers.mesCount FROM usersinservers 
				WHERE usersinservers.serverID=@serverID ORDER BY mesCount DESC LIMIT {count}";
			DataLayerShortcut.ExecuteReader<List<UserCountModel>>(readMessageCounts, results, queryString, new MySqlParameter("@serverID", context.Guild.Id));
			if (count == 0)
			{
				var mostCount = results.FirstOrDefault();
				var percent = Math.Round(((float)mostCount.messageCount / (float)totalCount) * 100, 2);
				result = $"User with most messages: {mostCount.userMention} with {mostCount.messageCount} messages which is {percent}% of the server's messages. Starting at {earliest.date.ToString(dateFormat)}";
			}
			else
			{
				var resultsCount = results.Count;
				result = $"Top {resultsCount} users:";
				foreach (var user in results)
				{
					var toAdd = $"\n{user.userMention}: {user.messageCount} messages, {Math.Round(((float)user.messageCount / (float)totalCount) * 100, 2)}%";
					if (result.Length + toAdd.Length + 2 > nextSplitLength)
					{
						result += $"||{toAdd}";
						nextSplitLength += 2000;
					}
					else
						result += toAdd;
				}
				result += $"\nStarting at {earliest.date.ToString(dateFormat)}";
			}

			return Task.FromResult<string>(result).Result;
		}

		public async Task<string> calculateUserMessageCounts(string user, ICommandContext context)
		{
			var result = "";
			var earliest = await getEarliestMessage(context);
			var totalCount = await getTotalMessageCount(context);
			List<UserCountModel> results = new List<UserCountModel>();
			var queryString = @"SELECT usersinservers.userID, usersinservers.nickNameMention, usersinservers.mesCount FROM usersinservers 
			WHERE usersinservers.serverID=@serverID AND usersinservers.nickNameMention=@mention";
			DataLayerShortcut.ExecuteReader<List<UserCountModel>>(readMessageCounts, results, queryString, new MySqlParameter("@mention", user), new MySqlParameter("@serverID", context.Guild.Id));
			var userCount = results.FirstOrDefault();
			var percent = Math.Round(((float)userCount.messageCount / (float)totalCount) * 100, 2);
			result = $"User {userCount.userMention} has sent {userCount.messageCount} messages which is {percent}% of the server's messages. Starting at {earliest.date.ToString(dateFormat)}";
			return Task.FromResult<string>(result).Result;
		}

		public async Task<string> calculateRoleMessageCounts(string role, ICommandContext context)
		{
			var result = "";
			var totalRoleCount = 0;
			var earliest = await getEarliestMessage(context);
			var totalCount = await getTotalMessageCount(context);

			//parse the roleID from the mention passed in.
			var roleId = Regex.Replace(role, "[<|>|@|&]", "");
			ulong roleIdParse = 0;
			var parseRes = ulong.TryParse(roleId, out roleIdParse);
			if (!parseRes) //if the roleid was not parseable.
				return Task.FromResult<string>("Could not find role").Result;
			//Go through every user in the guild and check if their roleids contains our role.
			//Because why would SocketRole have a list of users in the role, that would make sense.
			var UsersInRole = new List<SocketGuildUser>();
			var UsersInGuild = context.Guild.GetUsersAsync().Result;
			foreach (var user in UsersInGuild)
			{
				if (user.RoleIds.Contains(roleIdParse))
					UsersInRole.Add(user as SocketGuildUser);
			}
			List<UserCountModel> results = new List<UserCountModel>();
			//iterate over each user and get their message count on the server.
			foreach (var user in UsersInRole)
			{
				List<UserCountModel> userResults = new List<UserCountModel>();
				var queryString = @"SELECT usersinservers.userID, usersinservers.nickNameMention, usersinservers.mesCount FROM usersinservers 
				WHERE usersinservers.serverID=@serverID AND usersinservers.nickNameMention=@mention LIMIT 1";
				DataLayerShortcut.ExecuteReader<List<UserCountModel>>(readMessageCounts, userResults, queryString, new MySqlParameter("@mention", user.Mention.Replace("!", string.Empty)), new MySqlParameter("@serverID", context.Guild.Id));
				var userFound = userResults.FirstOrDefault();
				if (userFound == null)
					continue;
				totalRoleCount += userFound.messageCount;
				results.AddRange(userResults);
			}
			if (results.Count == 0) //if no user's were found for the Guild or Role.
				return Task.FromResult<string>("This role has no users with sent messages.").Result;
			results = results.OrderByDescending(r => r.messageCount).ToList();
			var topUser = results.FirstOrDefault();
			var percent = Math.Round(((float)totalRoleCount / (float)totalCount) * 100, 2);
			var topuserPercent = Math.Round(((float)topUser.messageCount / (float)totalCount) * 100, 2);
			var topUserRolePercent = Math.Round(((float)topUser.messageCount / (float)totalRoleCount) * 100, 2);
			result = $"Role {role} has {results.Count} users with {totalRoleCount} messages, which is {percent}% of the server's messages.\nThe user with the most messages in the role is {topUser.userMention} with {topUser.messageCount} which is {topUserRolePercent}% of the role's messages, and {topuserPercent}% of the server's messages.\nStarting at {earliest.date.ToString(dateFormat)}";

			return Task.FromResult<string>(result).Result;
		}

		public async Task<string> reloadMessageCounts(ICommandContext context)
		{
			var result = string.Empty;
			List<UsersInServersModel> users = new List<UsersInServersModel>();
			try
			{
				var queryString = @"SELECT usersinservers.userID FROM usersinservers WHERE usersinservers.serverID=@serverID";
				DataLayerShortcut.ExecuteReader<List<UsersInServersModel>>(readUsersInServers, users, queryString, new MySqlParameter("@serverID", context.Guild.Id));
				Parallel.ForEach(users, (user) =>
				{
					try
					{
						var sparams = new MySqlParameter[3] { new MySqlParameter("@serverID", context.Guild.Id), new MySqlParameter("@userID", user.userID), null };
						var t = DataLayerShortcut.ExecuteScalarUInt(@"SELECT COUNT(*) FROM messages WHERE messages.userID=@userID AND messages.serverID=@serverID AND NOT messages.isDeleted", sparams);
						if (t.HasValue)
							user.mesCount = t.Value;
						sparams[2] = new MySqlParameter("@mesCount", user.mesCount);
						DataLayerShortcut.ExecuteNonQuery(@"UPDATE usersinservers SET mesCount = @mesCount WHERE usersinservers.userID=@userID AND usersinservers.serverID=@serverID", sparams);
					}
					catch(Exception e)
					{
						result += e.Message;
						return;
					}
				});
			}
			catch (Exception e)
			{
				result += e.Message;
			}

			if (result != string.Empty)
				ErrorLog.writeLog(result);
			return result;
		}
		#endregion

		#region Emoji Counts
		/// <summary>
		/// Gets a top n count of emojis for a server
		/// </summary>
		/// <param name="count"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public async Task<string> calculateTopEmojiCounts(int count, ICommandContext context)
		{
			var result = "";
			var earliest = await getEarliestMessage(context);
			var emojiUseModels = new List<EmojiUseModel>();
			var totalCount = 0;
			var nextSplitLength = 2000;
			getEmojiModels(context, ref emojiUseModels);
			var topEmojis = new Dictionary<string, EmojiCountModel>();
			foreach(var use in emojiUseModels)
			{
				var key = string.Empty;
				if (context.Guild.Emotes.FirstOrDefault(x => x.Id == use.emojiID) != null)
					key = $"<:{use.emojiName}:{use.emojiID.ToString()}>";
				else
					key = $":{use.emojiName}:";
				if (topEmojis.ContainsKey(key))
					topEmojis[key].useCount++;
				else
				{
					var m = new EmojiCountModel();
					m.emojiID = use.emojiID;
					m.emojiName = use.emojiName;
					m.useCount = 1;
					topEmojis.Add(key, m);
				}
				totalCount++;
			}
			if (count > topEmojis.Count)
				count = topEmojis.Count;
			topEmojis = topEmojis.OrderByDescending(e => e.Value.useCount).ToDictionary(e => e.Key, e => e.Value);
			if (count == 0)
			{
				var topEmoji = topEmojis.FirstOrDefault();
				if (topEmoji.Key != null)
				{
					var percent = Math.Round(((float)topEmoji.Value.useCount / (float)totalCount) * 100, 2);
					var topUsers = new Dictionary<string, int>();
					foreach(var use in emojiUseModels)
					{
						if (use.emojiID != topEmoji.Value.emojiID)
							continue;
						if (topUsers.ContainsKey(use.userMention))
							topUsers[use.userMention]++;
						else
							topUsers.Add(use.userMention, 1);
					}
					var topUser = topUsers.OrderByDescending(e => e.Value).ToDictionary(e => e.Key, e => e.Value).FirstOrDefault();
					if (topUser.Key != null)
					{
						var userPercent = Math.Round(((float)topUser.Value / (float)topEmoji.Value.useCount) * 100, 2);
						result = $"Emoji with most uses: {topEmoji.Key} with {topEmoji.Value.useCount} uses which is {percent}% of emoji uses.\nEmoji is most used by {topUser.Key} with {topUser.Value} uses, which is {userPercent}% of the emoji's use.\nStarting at {earliest.date.ToString(dateFormat)}";
					}
					else
						return "Failed to find top user";
				}
				else
					return "No emoji has been used on this server.";
			}
			else
			{
				result = $"Top {count} emojis:";
				var max = 0;
				foreach (var res in topEmojis)
				{
					if (max >= count)
						break;

					var toAdd = $"\n{res.Key}: {res.Value.useCount} uses, {Math.Round(((float)res.Value.useCount / (float)totalCount) * 100, 2)}%";
					if (result.Length + toAdd.Length + 2 > nextSplitLength)
					{
						result += $"||{toAdd}";
						nextSplitLength += 2000;
					}
					else
						result += toAdd;
					max++;
				}
				if (max == 0) //If the foreach didnt run that probally means there weren't any Emojis used in messages.
					return "No emoji has been used on this server.";
				result += $"\nStarting at {earliest.date.ToString(dateFormat)}";
			}

			return Task.FromResult<string>(result).Result;
		}

		/// <summary>
		/// Gets the top n count of emojis for a user on a server
		/// </summary>
		/// <param name="count"></param>
		/// <param name="user"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public async Task<string> calculateTopEmojiCountsUser(int count, string user, ICommandContext context)
		{
			var result = "";
			var userId = getIDFromMention(user);
			if(userId == 0)
			{
				return Task.FromResult<string>("Could not get user from mention.").Result;
			}
			var earliest = await getEarliestMessage(context);
			var emojiUseModels = new List<EmojiUseModel>();
			var totalCount = 0;
			var totalUserCount = 0;
			var nextSplitLength = 2000;
			getEmojiModels(context, ref emojiUseModels);
			var topEmojis = new Dictionary<string, EmojiCountModel>();
			var topEmojisUser = new Dictionary<string, EmojiCountModel>();
			foreach (var use in emojiUseModels)
			{
				var key = string.Empty;
				if (context.Guild.Emotes.FirstOrDefault(x => x.Id == use.emojiID) != null)
					key = $"<:{use.emojiName}:{use.emojiID.ToString()}>";
				else
					key = $":{use.emojiName}:";
				if (topEmojis.ContainsKey(key))
					topEmojis[key].useCount++;
				else
				{
					var m = new EmojiCountModel();
					m.emojiID = use.emojiID;
					m.emojiName = use.emojiName;
					m.useCount = 1;
					topEmojis.Add(key, m);
				}
				totalCount++;
				if (use.userID == userId)
				{
					if (topEmojisUser.ContainsKey(key))
						topEmojisUser[key].useCount++;
					else
					{
						var m = new EmojiCountModel();
						m.emojiID = use.emojiID;
						m.emojiName = use.emojiName;
						m.useCount = 1;
						topEmojisUser.Add(key, m);
					}
					totalUserCount++;
				}
			}
			if (count > topEmojisUser.Count)
				count = topEmojisUser.Count;
			topEmojis = topEmojis.OrderByDescending(e => e.Value.useCount).ToDictionary(e => e.Key, e => e.Value);
			topEmojisUser = topEmojisUser.OrderByDescending(e => e.Value.useCount).ToDictionary(e => e.Key, e => e.Value);
			if (count == 0)
			{
				return Task.FromResult<string>("Count can not be 0. Use emojicount @user instead.").Result;
			}
			else
			{
				result = $"Top {count} emojis for {user}:";
				var max = 0;
				foreach (var res in topEmojisUser)
				{
					if (max >= count)
						break;

					var userPercent = Math.Round(((float)res.Value.useCount / (float)totalUserCount) * 100, 2);
					var emojiPercent = Math.Round(((float)res.Value.useCount / (float)(topEmojis[res.Key].useCount)) * 100, 2);
					var totalPercent = Math.Round(((float)res.Value.useCount / (float)totalCount) * 100, 2);
					var toAdd = $"\n{res.Key}: {res.Value.useCount} uses, user: {userPercent}%, emoji: {emojiPercent}%, total: {totalPercent}%";
					if (result.Length + toAdd.Length + 2 > nextSplitLength)
					{
						result += $"||{toAdd}";
						nextSplitLength += 2000;
					}
					else
						result += toAdd;
					max++;
				}
				if (max == 0) //If the foreach didnt run that probally means there weren't any Emojis used in messages.
					return Task.FromResult<string>("No emoji has been used on this server.").Result;
				result += $"\nStarting at {earliest.date.ToString(dateFormat)}";
			}
			return Task.FromResult<string>(result).Result;
		}

		/// <summary>
		/// Gets the uses and top user for a specific emoji
		/// </summary>
		/// <param name="emojiMention"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public async Task<string> calculateEmojiCounts(string emojiMention, ICommandContext context)
		{
			var result = "";
			var emojiID = getIDFromMention(emojiMention);
			if (emojiID == 0)
				return "Failed to find id from emoji";
			var earliest = await getEarliestMessage(context);
			var emojiUseModels = new List<EmojiUseModel>();
			var totalCount = 0;
			var reqEmojiCount = 0;
			getEmojiModels(context, ref emojiUseModels);

			var topEmojis = new Dictionary<string, EmojiCountModel>();
			foreach (var use in emojiUseModels)
			{
				var key = string.Empty;
				if (context.Guild.Emotes.FirstOrDefault(x => x.Id == use.emojiID) != null)
					key = $"<:{use.emojiName}:{use.emojiID.ToString()}>";
				else
					key = $":{use.emojiName}:";
				if (topEmojis.ContainsKey(key))
					topEmojis[key].useCount++;
				else
				{
					var m = new EmojiCountModel();
					m.emojiID = use.emojiID;
					m.emojiName = use.emojiName;
					m.useCount = 1;
					topEmojis.Add(key, m);
				}
				totalCount++;
				if (use.emojiID == emojiID)
					reqEmojiCount++;
			}

			if (topEmojis.FirstOrDefault(x => x.Value.emojiID == emojiID).Key == null)
				return $"{emojiMention} has not been used on this server.";
			topEmojis = topEmojis.OrderByDescending(e => e.Value.useCount).ToDictionary(e => e.Key, e => e.Value);
			var topUsers = new Dictionary<string, int>();
			foreach (var use in emojiUseModels)
			{
				if (use.emojiID != emojiID)
					continue;
				if (topUsers.ContainsKey(use.userMention))
					topUsers[use.userMention]++;
				else
					topUsers.Add(use.userMention, 1);
			}
			var topUser = topUsers.OrderByDescending(e => e.Value).ToDictionary(e => e.Key, e => e.Value).FirstOrDefault();

			var percent = Math.Round(((float)reqEmojiCount / (float)totalCount) * 100, 2);
			var userPercent = Math.Round(((float)topUser.Value / (float)reqEmojiCount) * 100, 2);
			result = $"{emojiMention} has been used {reqEmojiCount} times, which is {percent}% of emoji uses.\nThe emoji is most used by {topUser.Key} with {topUser.Value} uses, which is {userPercent}% of the emoji's use.\nStarting at {earliest.date.ToString(dateFormat)}";

			return result;
		}

		/// <summary>
		/// Gets the most used emoji for a user
		/// </summary>
		/// <param name="userMention"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public async Task<string> calculateUserEmojiCounts(string userMention, ICommandContext context)
		{
			var result = "";
			var userID = getIDFromMention(userMention);
			if (userID == 0)
				return "Failed to find id from user mention";
			var earliest = await getEarliestMessage(context);
			var emojiUseModels = new List<EmojiUseModel>();
			var totalCount = 0;
			getEmojiModels(context, ref emojiUseModels);
			var totalUserCount = 0;
			var topEmojis = new Dictionary<string, EmojiCountModel>();
			var topEmojisUser = new Dictionary<string, EmojiCountModel>();
			//go through every messages and get the count of the emoji usage in the message.
			foreach (var use in emojiUseModels)
			{
				var key = string.Empty;
				if (context.Guild.Emotes.FirstOrDefault(x => x.Id == use.emojiID) != null)
					key = $"<:{use.emojiName}:{use.emojiID.ToString()}>";
				else
					key = $":{use.emojiName}:";
				if (topEmojis.ContainsKey(key))
					topEmojis[key].useCount++;
				else
				{
					var m = new EmojiCountModel();
					m.emojiID = use.emojiID;
					m.emojiName = use.emojiName;
					m.useCount = 1;
					topEmojis.Add(key, m);
				}
				totalCount++;
				if (use.userID == userID)
				{
					if (topEmojisUser.ContainsKey(key))
						topEmojisUser[key].useCount++;
					else
					{
						var m = new EmojiCountModel();
						m.emojiID = use.emojiID;
						m.emojiName = use.emojiName;
						m.useCount = 1;
						topEmojisUser.Add(key, m);
					}
					totalUserCount++;
				}
			}
			topEmojis = topEmojis.OrderByDescending(e => e.Value.useCount).ToDictionary(e => e.Key, e => e.Value);
			var topEmoji = topEmojisUser.OrderByDescending(e => e.Value.useCount).ToDictionary(e => e.Key, e => e.Value).FirstOrDefault();
			if (topEmoji.Key == null)
				return "User has not used any emoji on this server";
			var totalPercent = Math.Round(((float)topEmoji.Value.useCount / (float)totalCount) * 100, 2);
			var userPercent = Math.Round(((float)topEmoji.Value.useCount / (float)totalUserCount) * 100, 2);
			result = $"{userMention} most used emoji is {topEmoji.Key} with {topEmoji.Value.useCount} uses which is {userPercent}% of the user's emoji use, and {totalPercent}% of the server's emoji use.\nStarting at {earliest.date.ToString(dateFormat)}";

			return result;
		}
		#endregion

		#region Nadeko Counts
		public async Task<string> calculateNadekoUserCounts(SocketUser user, CommandContext context)
		{
			var result = "";
			int? totalCommandCount = 0;
			int? userCommandCount = 0;
			var query = "SELECT COUNT(*) FROM Command WHERE ServerId = @serverId";
			totalCommandCount = DataLayerShortcut.executeScalarLite(context.Guild.Id, query, new SQLiteParameter("@serverId", context.Guild.Id));
			query = "SELECT COUNT(*) FROM Command WHERE UserId = @userId AND ServerId = @serverId";
			userCommandCount = DataLayerShortcut.executeScalarLite(context.Guild.Id, query, new SQLiteParameter("@userId", user.Id), new SQLiteParameter("@serverId", context.Guild.Id));
			if (!totalCommandCount.HasValue || !userCommandCount.HasValue)
			{
				result = "Failed to get command counts. Possible failure to connect to db.";
				return Task.FromResult<string>(result).Result;
			}
			var percent = Math.Round(((float)userCommandCount.Value / (float)totalCommandCount.Value) * 100, 2);
			result = $"{user.Mention} has sent {userCommandCount} commands to NadekoBot, which is {percent}% of commands sent.";
			return Task.FromResult<string>(result).Result;
		}
		#endregion
		#endregion

		#region Data Readers
		private void readEarliestDate(IDataReader reader, DateModel data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null)
			{
				data.date = reader.GetDateTime(0);
			}
		}

		private void readMessageCounts(IDataReader reader, List<UserCountModel> data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null)
			{
				if (reader.FieldCount >= 3)
				{
					var userObject = new UserCountModel();
					ulong? temp = reader.GetValue(0) as ulong?;
					userObject.userID = temp.HasValue ? temp.Value : 0;
					userObject.userMention = reader.GetString(1);
					userObject.messageCount = reader.GetInt32(2);
					data.Add(userObject);
				}
			}
		}

		private void readUsersInServers(IDataReader reader, List<UsersInServersModel> data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null)
			{
				if(reader.FieldCount >= 1)
				{
					var userObject = new UsersInServersModel();
					ulong? temp = reader.GetValue(0) as ulong?;
					userObject.userID = temp.HasValue ? temp.Value : 0;
					data.Add(userObject);
				}
			}
		}

		private void readEmojiCounts(IDataReader reader, List<EmojiUseModel> data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null)
			{
				if (reader.FieldCount >= 4)
				{
					var emojiObject = new EmojiUseModel();
					ulong? temp = reader.GetValue(0) as ulong?;
					emojiObject.emojiID = temp.HasValue ? temp.Value : 0;
					emojiObject.emojiName = reader.GetString(1);
					temp = reader.GetValue(2) as ulong?;
					emojiObject.userID = temp.HasValue ? temp.Value : 0;
					emojiObject.userMention = reader.GetString(3);
					data.Add(emojiObject);
				}
			}
		}
		#endregion

		#region Helper Functions
		private void getEmojiModels(ICommandContext context, ref List<EmojiUseModel> emojiModels)
		{
			var queryParams = new MySqlParameter[] {
				new MySqlParameter("@serverID", context.Guild.Id),
				new MySqlParameter("@botID", SGMessageBot.botConfig.botInfo.botId)
			};
			//var queryString = $"SELECT mS.userID, mS.messageID, uS.nickNameMention, mS.rawText FROM messages AS mS LEFT JOIN usersinservers AS uS ON mS.userID = uS.userID WHERE uS.serverID = @serverID AND mS.mesText LIKE @emojiID AND mS.serverID = @serverID AND mS.isDeleted = 0 AND mS.userID != @botID AND mS.mesText NOT LIKE '%emojicount%'";
			var queryString = $"SELECT eU.emojiID, eU.emojiName, eU.userID, uS.nickNameMention " +
				$"FROM emojiuses AS eU LEFT JOIN messages as mS ON eU.messageID = mS.messageID LEFT JOIN usersinservers AS uS ON eU.userID = uS.userID " +
			    $"WHERE uS.serverID = @serverID AND eU.serverID = @serverID AND eU.isDeleted = 0 AND eU.userID != @botID AND mS.mesText NOT LIKE '%emojicount%'";
			DataLayerShortcut.ExecuteReader<List<EmojiUseModel>>(readEmojiCounts, emojiModels, queryString, queryParams);
		}

		private ulong getIDFromMention(string mention)
		{
			var match = Regex.Match(mention, @"\d+");
			if(!match.Success)
				return 0;
			var result = (ulong)0;
			var success = UInt64.TryParse(match.Value, out result);
			if (!success)
				return 0;
			return result;
		}
		#endregion
	}
}
