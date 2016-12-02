using Discord.Commands;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	public class BotCommandProcessor
	{
		#region Calc Functions
		public async Task<DateModel> getEarliestMessage(CommandContext context)
		{
			var result = new DateModel();
			var queryString = "SELECT mesTime FROM messages WHERE serverID = @serverID ORDER BY mesTime LIMIT 1";
			DataLayerShortcut.ExecuteReader<DateModel>(readEarliestDate, result, queryString, new MySqlParameter("@serverID", context.Guild.Id));
			return Task.FromResult<DateModel>(result).Result;
		}

		public async Task<int> getTotalMessageCount(CommandContext context)
		{
			int? result = 0;
			var queryString = "SELECT COUNT(*) FROM messages WHERE isDeleted = false AND serverID = @serverID";
			result = DataLayerShortcut.ExecuteScalar(queryString, new MySqlParameter("@serverID", context.Guild.Id));
			return Task.FromResult<int>(result.HasValue ? result.Value : 0).Result;
		}

		#region Message Counts
		public async Task<string> calculateTopMessageCounts(int count, CommandContext context)
		{
			var result = "";
			var earliest = await getEarliestMessage(context);
			var totalCount = await getTotalMessageCount(context);
			List<UserCountModel> results = new List<UserCountModel>();
			var queryString = "";
			if (count == 0)
				queryString = $@"SELECT messages.userID, usersinservers.nickNameMention, count(messages.userID) AS mesCount FROM messages 
				LEFT JOIN usersinservers ON messages.userID=usersinservers.userID WHERE messages.serverID=@serverID AND usersinservers.serverID=@serverID AND messages.isDeleted = false GROUP BY userID ORDER BY mesCount DESC LIMIT 1";
			else
				queryString = $@"SELECT messages.userID, usersinservers.nickNameMention, count(messages.userID) AS mesCount FROM messages 
				LEFT JOIN usersinservers ON messages.userID=usersinservers.userID WHERE messages.serverID=@serverID AND usersinservers.serverID=@serverID AND messages.isDeleted = false GROUP BY userID ORDER BY mesCount DESC LIMIT {count}";
			DataLayerShortcut.ExecuteReader<List<UserCountModel>>(readMessageCounts, results, queryString, new MySqlParameter("@serverID", context.Guild.Id));
			if (count == 0)
			{
				var mostCount = results.FirstOrDefault();
				var percent = Math.Round(((float)mostCount.messageCount / (float)totalCount) * 100, 2);
				result = $"User with most messages: {mostCount.userMention} with {mostCount.messageCount} messages which is {percent}% of the server's messages. Starting at {earliest.date.ToString("yyyy/MM/dd")}";
			}
			else
			{
				var resultsCount = results.Count;
				result = $"Top {resultsCount} users:";
				foreach (var user in results)
				{
					result += $"\n{user.userMention}: {user.messageCount} messages, {Math.Round(((float)user.messageCount / (float)totalCount) * 100, 2)}%";
				}
				result += $"\nStarting at {earliest.date.ToString("yyyy/MM/dd")}";
			}

			return Task.FromResult<string>(result).Result;
		}

		public async Task<string> calculateUserMessageCounts(string user, CommandContext context)
		{
			var result = "";
			var earliest = await getEarliestMessage(context);
			var totalCount = await getTotalMessageCount(context);
			List<UserCountModel> results = new List<UserCountModel>();
			var queryString = @"SELECT messages.userID, usersinservers.nickNameMention, count(messages.userID) FROM messages LEFT JOIN usersinservers ON messages.userID=usersinservers.userID 
			WHERE messages.serverID=@serverID AND usersinservers.serverID=@serverID AND usersinservers.nickNameMention=@mention AND messages.isDeleted = false GROUP BY userID";
			DataLayerShortcut.ExecuteReader<List<UserCountModel>>(readMessageCounts, results, queryString, new MySqlParameter("@mention", user), new MySqlParameter("@serverID", context.Guild.Id));
			var userCount = results.FirstOrDefault();
			var percent = Math.Round(((float)userCount.messageCount / (float)totalCount) * 100, 2);
			result = $"User {userCount.userMention} has sent {userCount.messageCount} messages which is {percent}% of the server's messages. Starting at {earliest.date.ToString("yyyy/MM/dd")}";
			return Task.FromResult<string>(result).Result;
		}

		public async Task<string> calculateRoleMessageCounts(string role, CommandContext context)
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
				var queryString = @"SELECT messages.userID, usersinservers.nickNameMention, count(messages.userID) from messages LEFT JOIN usersinservers ON messages.userID=usersinservers.userID 
				WHERE usersinservers.serverID=messages.serverID AND usersinservers.nickNameMention=@mention AND messages.isDeleted = false GROUP BY userID LIMIT 1";
				DataLayerShortcut.ExecuteReader<List<UserCountModel>>(readMessageCounts, userResults, queryString, new MySqlParameter("@mention", user.Mention.Replace("!", string.Empty)));
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
			result = $"Role {role} has {results.Count} users with {totalRoleCount} messages, which is {percent}% of the server's messages.\nThe user with the most messages in the role is {topUser.userMention} with {topUser.messageCount} which is {topUserRolePercent}% of the role's messages, and {topuserPercent}% of the server's messages.\nStarting at {earliest.date.ToString("yyyy/MM/dd")}";

			return Task.FromResult<string>(result).Result;
		}
		#endregion

		#region Emoji Counts
		public async Task<string> calculateTopEmojiCounts(int count, CommandContext context)
		{
			var result = "";
			var earliest = await getEarliestMessage(context);
			var emojiModels = new Dictionary<string, List<EmojiMessageModel>>();
			var emojis = context.Guild.Emojis;
			if (count > emojis.Count)
				count = emojis.Count;
			var totalCount = 0;
			foreach (var emoji in emojis)
			{
				var emojiCounter = new List<EmojiMessageModel>();
				var queryParams = new MySqlParameter[] {
					new MySqlParameter("@serverID", context.Guild.Id),
					new MySqlParameter("@emojiID", $"%{emoji.Id}%"),
					new MySqlParameter("@botID", SGMessageBot.botConfig.credInfo.botId)
				};
				var queryString = $"SELECT mS.userID, mS.messageID, uS.nickNameMention, mS.rawText FROM messages AS mS LEFT JOIN usersinservers AS uS ON mS.userID = uS.userID WHERE uS.serverID = @serverID AND mS.mesText LIKE @emojiID AND mS.serverID = @serverID AND mS.isDeleted = 0 AND mS.userID != @botID AND mS.mesText NOT LIKE '%emojicount%'";
				DataLayerShortcut.ExecuteReader<List<EmojiMessageModel>>(readEmojiCounts, emojiCounter, queryString, queryParams);
				foreach(var res in emojiCounter)
				{
					res.emojiID = emoji.Id;
				}
				emojiModels.Add($"<:{emoji.Name}:{emoji.Id}>", emojiCounter);
				//queryString = "SELECT count(*) FROM messages AS mS LEFT JOIN usersinservers AS uS ON mS.userID = uS.userID WHERE uS.serverID = @serverID AND mS.mesText LIKE @emojiID AND mS.serverID = @serverID AND mS.isDeleted = 0 AND mS.userID != @botID AND mS.mesText NOT LIKE '%emojicount%'";
				//var emCount = DataLayerShortcut.ExecuteScalar(queryString, queryParams);
				//totalCount += emCount.HasValue ? emCount.Value : 0;
			}
			emojiModels.OrderByDescending(e => e.Value.Count).ToDictionary(e => e.Key, e => e.Value);
			//go through every messages and get the count of the emoji usage in the message.
			foreach(var res in emojiModels)
			{
				foreach(var emoji in res.Value)
				{
					emoji.useCount = Regex.Matches(emoji.mesText, $"{emoji.emojiID.ToString()}").Count;
					totalCount += emoji.useCount;
				}
			}
			if (count == 0)
			{
				var topEmoji = emojiModels.FirstOrDefault();
				if (topEmoji.Key != null)
				{
					var totalEmojiCount = 0;
					foreach(var emoji in topEmoji.Value)
					{
						totalEmojiCount += emoji.useCount;
					}
					var percent = Math.Round(((float)totalEmojiCount / (float)totalCount) * 100, 2);
					var topUsers = new Dictionary<string, int>();
					foreach(var emResult in topEmoji.Value)
					{
						if (topUsers.ContainsKey(emResult.mention))
							topUsers[emResult.mention] += emResult.useCount;
						else
							topUsers.Add(emResult.mention, emResult.useCount);
					}
					var topUser = topUsers.OrderByDescending(e => e.Value).ToDictionary(e => e.Key, e => e.Value).FirstOrDefault();
					if (topUser.Key != null)
					{
						var userPercent = Math.Round(((float)topUser.Value / (float)totalEmojiCount) * 100, 2);
						result = $"Emoji with most uses: {topEmoji.Key} with {totalEmojiCount} uses which is {percent}% of emoji uses.\nEmoji is most used by {topUser.Key} with {topUser.Value} uses, which is {userPercent}% of the emoji's use.\nStarting at {earliest.date.ToString("yyyy/MM/dd")}";
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
				foreach (var res in emojiModels)
				{
					if (max > count)
						break;

					var totalEmojiCount = 0;
					foreach (var emoji in res.Value)
					{
						totalEmojiCount += emoji.useCount;
					}

					result += $"\n{res.Key}: {totalEmojiCount} uses, {Math.Round(((float)totalEmojiCount / (float)totalCount) * 100, 2)}%";
					max++;
				}
				if (max == 0) //If the foreach didnt run that probally means there weren't any Emojis used in messages.
					return "No emoji has been used on this server.";
				result += $"\nStarting at {earliest.date.ToString("yyyy/MM/dd")}";
			}

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

		private void readEmojiCounts(IDataReader reader, List<EmojiMessageModel> data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null)
			{
				if (reader.FieldCount >= 3)
				{
					var emojiObject = new EmojiMessageModel();
					ulong? temp = reader.GetValue(0) as ulong?;
					emojiObject.userID = temp.HasValue ? temp.Value : 0;
					temp = reader.GetValue(1) as ulong?;
					emojiObject.messageID = temp.HasValue ? temp.Value : 0;
					emojiObject.mention = reader.GetString(2);
					emojiObject.mesText = reader.GetString(3);
					emojiObject.useCount = 0;
					emojiObject.emojiID = 0;
					data.Add(emojiObject);
				}
			}
		}
		#endregion
	}
}
