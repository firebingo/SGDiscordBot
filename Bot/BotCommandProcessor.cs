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
		public async Task<DateModel> getEarlistMessage()
		{
			var result = new DateModel();
			var queryString = "SELECT mesTime FROM messages ORDER BY mesTime LIMIT 1;";
			DataLayerShortcut.ExecuteReader<DateModel>(readEarliestDate, result, queryString);
			return Task.FromResult<DateModel>(result).Result;
		}

		public async Task<int> getTotalMessageCount()
		{
			int? result = 0;
			var queryString = "SELECT COUNT(*) FROM messages WHERE isDeleted = false";
			result = DataLayerShortcut.ExecuteScalar(queryString);
			return Task.FromResult<int>(result.HasValue ? result.Value : 0).Result;
		}

		public async Task<string> calculateTopMessageCounts(int count)
		{
			var result = "";
			var earliest = await getEarlistMessage();
			var totalCount = await getTotalMessageCount();
			List<UserCountModel> results = new List<UserCountModel>();
			var queryString = "";
			if (count == 0)
				queryString = $@"SELECT messages.userID, usersinservers.nickNameMention, count(messages.userID) as mesCount from messages 
				LEFT JOIN usersinservers ON messages.userID=usersinservers.userID WHERE usersinservers.serverID=messages.serverID AND messages.isDeleted = false GROUP BY userID ORDER BY mesCount DESC LIMIT 1";
			else
				queryString = $@"SELECT messages.userID, usersinservers.nickNameMention, count(messages.userID) as mesCount from messages 
				LEFT JOIN usersinservers ON messages.userID=usersinservers.userID WHERE usersinservers.serverID=messages.serverID AND messages.isDeleted = false GROUP BY userID ORDER BY mesCount DESC LIMIT {count}";
			DataLayerShortcut.ExecuteReader<List<UserCountModel>>(readMessageCounts, results, queryString);
			if(count == 0)
			{
				var mostCount = results.FirstOrDefault();
				var percent = Math.Round(((float)mostCount.messageCount / (float)totalCount) * 100, 2);
				result = $"User with most messages: {mostCount.userMention} with {mostCount.messageCount} messages which is {percent}% of the server's messages. Starting at {earliest.date.ToString("yyyy/MM/dd")}";
			}
			else
			{
				var resultsCount = results.Count;
				result = $"Top {resultsCount} users:";
				foreach(var user in results)
				{
					result += $"\n{user.userMention}: {user.messageCount} messages, {Math.Round(((float)user.messageCount / (float)totalCount) * 100, 2)}";
				}
				result += $"\nStarting at {earliest.date.ToString("yyyy/MM/dd")}";
			}

			return Task.FromResult<string>(result).Result;
		}

		public async Task<string> calculateUserMessageCounts(string user)
		{
			var result = "";
			var earliest = await getEarlistMessage();
			var totalCount = await getTotalMessageCount();
			List<UserCountModel> results = new List<UserCountModel>();
			var queryString = @"SELECT messages.userID, usersinservers.nickNameMention, count(messages.userID) from messages LEFT JOIN usersinservers ON messages.userID=usersinservers.userID 
			WHERE usersinservers.serverID=messages.serverID AND usersinservers.nickNameMention=@mention AND messages.isDeleted = false GROUP BY userID";
			DataLayerShortcut.ExecuteReader<List<UserCountModel>>(readMessageCounts, results, queryString, new MySqlParameter("@mention", user));
			var userCount = results.FirstOrDefault();
			var percent = Math.Round(((float)userCount.messageCount / (float)totalCount) * 100, 2);
			result = $"User {userCount.userMention} has sent {userCount.messageCount} messages which is {percent}% of the server's messages. Starting at {earliest.date.ToString("yyyy/MM/dd")}";
			return Task.FromResult<string>(result).Result;
		}

		public async Task<string> calculateRoleMessageCounts(string role, CommandContext context)
		{
			var result = "";
			var totalRoleCount = 0;
			var earliest = await getEarlistMessage();
			var totalCount = await getTotalMessageCount();

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
			foreach(var user in UsersInGuild)
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
			if(results.Count == 0) //if no user's were found for the Guild or Role.
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
		#endregion
	}
}
