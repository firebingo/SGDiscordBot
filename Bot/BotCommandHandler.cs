using Discord;
using Discord.Commands;
using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	public static class BotCommandHandler
	{
		public static void createCommands(DiscordClient Client)
		{
			Client.GetService<CommandService>().CreateCommand("shutdown").Alias("shutdown").Do(async e =>
			{
				var hasPerm = false;
				foreach (var permRole in SGMessageBot.BotConfig.credInfo.commandRoleIds)
				{
					var checkMatch = e.User.Roles.Where(r => r.Id == permRole).FirstOrDefault();
					if (checkMatch != null)
						hasPerm = true;
				}
				if (hasPerm)
				{
					await e.Channel.SendMessage("Goodbye").ConfigureAwait(false);
					await Task.Delay(2000).ConfigureAwait(false);
					Environment.Exit(0);
				}
			});

			Client.GetService<CommandService>().CreateCommand("restart").Alias("restart").Do(async e =>
			{
				var hasPerm = false;
				foreach (var permRole in SGMessageBot.BotConfig.credInfo.commandRoleIds)
				{
					var checkMatch = e.User.Roles.Where(r => r.Id == permRole).FirstOrDefault();
					if (checkMatch != null)
						hasPerm = true;
				}
				if (hasPerm)
				{
					await e.Channel.SendMessage("Restarting...");
					await Task.Delay(2000);
					System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
					Environment.Exit(0);
				}
			});

			Client.GetService<CommandService>().CreateCommand("messagecount")
			.Alias("messagecount")
			.Parameter("UserToCalc", ParameterType.Optional)
			.Do(async e =>
			{
				var hasPerm = false;
				foreach (var permRole in SGMessageBot.BotConfig.credInfo.commandRoleIds)
				{
					var checkMatch = e.User.Roles.Where(r => r.Id == permRole).FirstOrDefault();
					if (checkMatch != null)
						hasPerm = true;
				}
				if (hasPerm)
				{
					var userArg = e.GetArg(0);
					var result = await calculateMessageCounts(userArg);
					if(userArg == null || userArg.Trim() == String.Empty)
					{
						result = result.OrderBy(x => x.messageCount).ToList();
						if (result.Count == 0)
							await e.Channel.SendMessage($"No users have sent a message on this server.");
						else
						{
							var mostCount = result.FirstOrDefault();
							await e.Channel.SendMessage($"User with most messages: {mostCount.userMention} with {mostCount.messageCount} messages.");
						}
					}
					else
					{
						if (result.Count == 0)
							await e.Channel.SendMessage($"User {userArg} has not sent any messages.");
						else
						{
							var userCount = result.FirstOrDefault();
							await e.Channel.SendMessage($"User {userCount.userMention} has sent {userCount.messageCount} messages.");
						}
					}
				}
			});
		}

		#region Calc Functions
		private static async Task<DateTime> getEarlistMessage()
		{
			var result = new DateTime();

			return Task.FromResult<DateTime>(result).Result;
		}

		private static async Task<List<UserCountModel>> calculateMessageCounts(string user)
		{
			var queryString = "";
			List<UserCountModel> results = new List<UserCountModel>();
			if (user == null || user.Trim() == String.Empty)
			{
				queryString = "SELECT messages.userID, usersinservers.nickNameMention, count(messages.userID) from messages LEFT JOIN usersinservers ON messages.userID=usersinservers.userID WHERE usersinservers.serverID=messages.serverID GROUP BY userID";
				DataLayerShortcut.ExecuteReader<List<UserCountModel>>(readMessageCounts, results, queryString);
			}
			else
			{
				queryString = "SELECT messages.userID, usersinservers.nickNameMention, count(messages.userID) from messages LEFT JOIN usersinservers ON messages.userID=usersinservers.userID WHERE usersinservers.serverID=messages.serverID AND usersinservers.nickNameMention=@mention GROUP BY userID";
				DataLayerShortcut.ExecuteReader<List<UserCountModel>>(readMessageCounts, results, queryString, new MySqlParameter("@mention", user));
			}
			return Task.FromResult<List<UserCountModel>>(results).Result;
		}
		#endregion

		#region Data Readers
		private static void readEarliestDate(IDataReader reader, DateTime data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null)
			{
				while (reader.Read())
				{
					data = reader.GetDateTime(0);
				}
			}
		}

		private static void readMessageCounts(IDataReader reader, List<UserCountModel> data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null)
			{
				while (reader.Read())
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
		}
		#endregion
	}
}
