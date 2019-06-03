using Discord.Commands;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SGMessageBot.DiscordBot
{
	public class BotCommandProcessor
	{
		private readonly string dateFormat = "yyyy/MM/dd";

		#region Calc Functions
		public async Task<DateModel> GetEarliestMessage(ICommandContext context)
		{
			var result = new DateModel();
			var queryString = "SELECT mesTime FROM messages WHERE serverID = @serverID AND mesTime IS NOT NULL ORDER BY mesTime LIMIT 1";
			await DataLayerShortcut.ExecuteReader(ReadEarliestDate, result, queryString, new MySqlParameter("@serverID", context.Guild.Id));
			return result;
		}

		public async Task<int> GetTotalMessageCount(ICommandContext context)
		{
			int? result = 0;
			var queryString = "SELECT COUNT(*) FROM messages WHERE isDeleted = false AND serverID = @serverID";
			result = await DataLayerShortcut.ExecuteScalarInt(queryString, new MySqlParameter("@serverID", context.Guild.Id));
			return result ?? 0;
		}

		#region Admin Functions
		public async Task<string> CalculateRoleCounts(SocketTextChannel channel, bool useMentions, ICommandContext context)
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
				ErrorLog.WriteError(e);
				result = e.Message;
			}
			return Task.FromResult<string>(result).Result;
		}
		#endregion

		#region Message Counts
		public async Task<string> CalculateTopMessageCounts(int count, ICommandContext context)
		{
			var result = "";
			var earliest = await GetEarliestMessage(context);
			var totalCount = await GetTotalMessageCount(context);
			var nextSplitLength = 2000;
			List<UserCountModel> results = new List<UserCountModel>();
			var queryString = "";
			if (count == 0)
				queryString = $@"SELECT usersinservers.userID, usersinservers.nickNameMention, usersinservers.mesCount FROM usersinservers 
				WHERE usersinservers.serverID=@serverID ORDER BY mesCount DESC LIMIT 1";
			else
				queryString = $@"SELECT usersinservers.userID, usersinservers.nickNameMention, usersinservers.mesCount FROM usersinservers 
				WHERE usersinservers.serverID=@serverID ORDER BY mesCount DESC LIMIT {count}";
			await DataLayerShortcut.ExecuteReader<List<UserCountModel>>(ReadMessageCounts, results, queryString, new MySqlParameter("@serverID", context.Guild.Id));
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

		public async Task<string> CalculateUserMessageCounts(string user, ICommandContext context)
		{
			var result = "";
			var earliest = await GetEarliestMessage(context);
			var totalCount = await GetTotalMessageCount(context);
			List<UserCountModel> results = new List<UserCountModel>();
			var queryString = @"SELECT usersinservers.userID, usersinservers.nickNameMention, usersinservers.mesCount FROM usersinservers 
			WHERE usersinservers.serverID=@serverID AND usersinservers.nickNameMention=@mention";
			await DataLayerShortcut.ExecuteReader<List<UserCountModel>>(ReadMessageCounts, results, queryString, new MySqlParameter("@mention", user), new MySqlParameter("@serverID", context.Guild.Id));
			var userCount = results.FirstOrDefault();
			var percent = Math.Round(((float)userCount.messageCount / (float)totalCount) * 100, 2);
			result = $"User {userCount.userMention} has sent {userCount.messageCount} messages which is {percent}% of the server's messages. Starting at {earliest.date.ToString(dateFormat)}";
			return Task.FromResult<string>(result).Result;
		}

		public async Task<string> CalculateRoleMessageCounts(string role, ICommandContext context)
		{
			var result = "";
			var totalRoleCount = 0;
			var earliest = await GetEarliestMessage(context);
			var totalCount = await GetTotalMessageCount(context);

			//parse the roleID from the mention passed in.
			var roleId = Regex.Replace(role, "[<|>|@|&]", "");
			var parseRes = ulong.TryParse(roleId, out var roleIdParse);
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
				await DataLayerShortcut.ExecuteReader<List<UserCountModel>>(ReadMessageCounts, userResults, queryString, new MySqlParameter("@mention", user.Mention.Replace("!", string.Empty)), new MySqlParameter("@serverID", context.Guild.Id));
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

		public async Task<string> ReloadMessageCounts(ICommandContext context)
		{
			var result = string.Empty;
			List<UsersInServersModel> users = new List<UsersInServersModel>();
			try
			{
				var queryString = @"SELECT usersinservers.userID FROM usersinservers WHERE usersinservers.serverID=@serverID";
				await DataLayerShortcut.ExecuteReader<List<UsersInServersModel>>(ReadUsersInServers, users, queryString, new MySqlParameter("@serverID", context.Guild.Id));
				List<Task<uint?>> tasks = new List<Task<uint?>>();
				List<MySqlParameter[]> uparams = new List<MySqlParameter[]>();
				foreach(var u in users)
				{
					var sparams = new MySqlParameter[3] { new MySqlParameter("@serverID", context.Guild.Id), new MySqlParameter("@userID", u.userID), null };
					uparams.Add(sparams);
					tasks.Add(DataLayerShortcut.ExecuteScalarUInt(@"SELECT COUNT(*) FROM messages WHERE messages.userID=@userID AND messages.serverID=@serverID AND NOT messages.isDeleted", sparams));
				}
				await Task.WhenAll(tasks.ToArray());
				Parallel.For(0, tasks.Count, (i) =>
				{
					try
					{
						var t = tasks[i].Result;
						if (t.HasValue)
						{
							uparams[i][2] = new MySqlParameter("@mesCount", t.Value);
							Task.Run(() => DataLayerShortcut.ExecuteNonQuery(@"UPDATE usersinservers SET mesCount = @mesCount WHERE usersinservers.userID=@userID AND usersinservers.serverID=@serverID", uparams[i]));
						}
					}
					catch (Exception ex)
					{
						result += ex.Message;
						return;
					}
				});
			}
			catch (Exception ex)
			{
				result += ex.Message;
			}

			if (result != string.Empty)
				ErrorLog.WriteLog(result);
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
		public async Task<string> CalculateTopEmojiCounts(int count, ICommandContext context)
		{
			var result = "";
			var earliest = await GetEarliestMessage(context);
			var emojiUseModels = new List<EmojiUseModel>();
			var totalCount = 0;
			var nextSplitLength = 2000;
			GetEmojiModels(context, ref emojiUseModels);
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
					var m = new EmojiCountModel
					{
						emojiID = use.emojiID,
						emojiName = use.emojiName,
						useCount = 1
					};
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
		public async Task<string> CalculateTopEmojiCountsUser(int count, string user, ICommandContext context)
		{
			var result = "";
			var userId = GetIDFromMention(user);
			if(userId == 0)
			{
				return Task.FromResult<string>("Could not get user from mention.").Result;
			}
			var earliest = await GetEarliestMessage(context);
			var emojiUseModels = new List<EmojiUseModel>();
			var totalCount = 0;
			var totalUserCount = 0;
			var nextSplitLength = 2000;
			GetEmojiModels(context, ref emojiUseModels);
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
					var m = new EmojiCountModel
					{
						emojiID = use.emojiID,
						emojiName = use.emojiName,
						useCount = 1
					};
					topEmojis.Add(key, m);
				}
				totalCount++;
				if (use.userID == userId)
				{
					if (topEmojisUser.ContainsKey(key))
						topEmojisUser[key].useCount++;
					else
					{
						var m = new EmojiCountModel
						{
							emojiID = use.emojiID,
							emojiName = use.emojiName,
							useCount = 1
						};
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
		public async Task<string> CalculateEmojiCounts(string emojiMention, ICommandContext context)
		{
			var result = "";
			var emojiID = GetIDFromMention(emojiMention);
			if (emojiID == 0)
				return "Failed to find id from emoji";
			var earliest = await GetEarliestMessage(context);
			var emojiUseModels = new List<EmojiUseModel>();
			var totalCount = 0;
			var reqEmojiCount = 0;
			GetEmojiModels(context, ref emojiUseModels);

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
					var m = new EmojiCountModel
					{
						emojiID = use.emojiID,
						emojiName = use.emojiName,
						useCount = 1
					};
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
		public async Task<string> CalculateUserEmojiCounts(string userMention, ICommandContext context)
		{
			var result = "";
			var userID = GetIDFromMention(userMention);
			if (userID == 0)
				return "Failed to find id from user mention";
			var earliest = await GetEarliestMessage(context);
			var emojiUseModels = new List<EmojiUseModel>();
			var totalCount = 0;
			GetEmojiModels(context, ref emojiUseModels);
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
					var m = new EmojiCountModel
					{
						emojiID = use.emojiID,
						emojiName = use.emojiName,
						useCount = 1
					};
					topEmojis.Add(key, m);
				}
				totalCount++;
				if (use.userID == userID)
				{
					if (topEmojisUser.ContainsKey(key))
						topEmojisUser[key].useCount++;
					else
					{
						var m = new EmojiCountModel
						{
							emojiID = use.emojiID,
							emojiName = use.emojiName,
							useCount = 1
						};
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

		#region Other Functions
		#endregion

		#endregion

		#region Data Readers
		private void ReadEarliestDate(IDataReader reader, DateModel data)
		{
			reader = reader as DbDataReader;
			if (reader != null)
			{
				data.date = reader.GetDateTime(0);
			}
		}

		private void ReadMessageCounts(IDataReader reader, List<UserCountModel> data)
		{
			reader = reader as DbDataReader;
			if (reader != null)
			{
				if (reader.FieldCount >= 3)
				{
					var userObject = new UserCountModel();
					ulong? temp = reader.GetValue(0) as ulong?;
					userObject.userID = temp ?? 0;
					userObject.userMention = reader.GetString(1);
					userObject.messageCount = reader.GetInt32(2);
					data.Add(userObject);
				}
			}
		}

		private void ReadUsersInServers(IDataReader reader, List<UsersInServersModel> data)
		{
			reader = reader as DbDataReader;
			if (reader != null)
			{
				if(reader.FieldCount >= 1)
				{
					var userObject = new UsersInServersModel();
					ulong? temp = reader.GetValue(0) as ulong?;
					userObject.userID = temp ?? 0;
					data.Add(userObject);
				}
			}
		}

		private void ReadEmojiCounts(IDataReader reader, List<EmojiUseModel> data)
		{
			reader = reader as DbDataReader;
			if (reader != null)
			{
				if (reader.FieldCount >= 4)
				{
					var emojiObject = new EmojiUseModel();
					ulong? temp = reader.GetValue(0) as ulong?;
					emojiObject.emojiID = temp ?? 0;
					emojiObject.emojiName = reader.GetString(1);
					temp = reader.GetValue(2) as ulong?;
					emojiObject.userID = temp ?? 0;
					emojiObject.userMention = reader.GetString(3);
					data.Add(emojiObject);
				}
			}
		}

		private static void ReadMessagesText(IDataReader reader, List<MessageTextModel> data)
		{
			reader = reader as DbDataReader;
			if (reader != null)
			{
				var message = new MessageTextModel(reader.GetString(0));
				data.Add(message);
			}
		}
		#endregion

		#region Helper Functions
		private void GetEmojiModels(ICommandContext context, ref List<EmojiUseModel> emojiModels)
		{
			var queryParams = new MySqlParameter[] {
				new MySqlParameter("@serverID", context.Guild.Id),
				new MySqlParameter("@botID", SGMessageBot.BotConfig.BotInfo.DiscordConfig.botId)
			};
			//var queryString = $"SELECT mS.userID, mS.messageID, uS.nickNameMention, mS.rawText FROM messages AS mS LEFT JOIN usersinservers AS uS ON mS.userID = uS.userID WHERE uS.serverID = @serverID AND mS.mesText LIKE @emojiID AND mS.serverID = @serverID AND mS.isDeleted = 0 AND mS.userID != @botID AND mS.mesText NOT LIKE '%emojicount%'";
			var queryString = $"SELECT eU.emojiID, eU.emojiName, eU.userID, uS.nickNameMention " +
				$"FROM emojiuses AS eU LEFT JOIN messages as mS ON eU.messageID = mS.messageID LEFT JOIN usersinservers AS uS ON eU.userID = uS.userID " +
			    $"WHERE uS.serverID = @serverID AND eU.serverID = @serverID AND eU.isDeleted = 0 AND eU.userID != @botID AND mS.mesText NOT LIKE '%emojicount%'";
			var ret = DataLayerShortcut.ExecuteReader<List<EmojiUseModel>>(ReadEmojiCounts, emojiModels, queryString, queryParams);
			ret.RunSynchronously();
		}

		private ulong GetIDFromMention(string mention)
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

		public static async Task<List<MessageTextModel>> LoadMessages(DateTime? minDate = null, int? skip = null, int? limit = null)
		{
			var result = new List<MessageTextModel>();
			if (!minDate.HasValue)
				minDate = DateTime.MinValue;

			var paramlist = new List<MySqlParameter>
			{
				new MySqlParameter("@botId", SGMessageBot.BotConfig.BotInfo.DiscordConfig.botId),
				new MySqlParameter("@date", minDate),
			};

			var query = string.Empty;
			if (skip.HasValue && limit.HasValue)
			{
				query = "SELECT txt FROM (SELECT COALESCE(mesText, editedMesText) AS txt FROM messages WHERE NOT isDeleted AND userId != @botId AND mesTime > @date) x WHERE txt != '' AND txt NOT LIKE '%@botId%' LIMIT @skip, @limit";
				paramlist.Add(new MySqlParameter("@skip", skip.Value));
				paramlist.Add(new MySqlParameter("@limit", limit.Value));
			}
			else
				query = "SELECT txt FROM (SELECT COALESCE(mesText, editedMesText) AS txt FROM messages WHERE NOT isDeleted AND userId != @botId AND mesTime > @date) x WHERE txt != '' AND txt NOT LIKE '%@botId%'";

			await DataLayerShortcut.ExecuteReader<List<MessageTextModel>>(ReadMessagesText, result, query, paramlist.ToArray());

			return result;
		}
		#endregion
	}
}
