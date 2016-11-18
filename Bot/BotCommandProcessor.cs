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
	public class BotCommandProcessor
	{
		#region Calc Functions
		public async Task<DateTime> getEarlistMessage()
		{
			var result = new DateTime();

			return Task.FromResult<DateTime>(result).Result;
		}

		public async Task<List<UserCountModel>> calculateMessageCounts(string user)
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
		private void readEarliestDate(IDataReader reader, DateTime data)
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
