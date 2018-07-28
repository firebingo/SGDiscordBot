using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SGMessageBot.DiscordBot
{
	public class StatTracker
	{
		private void insertRow(StatModel res)
		{
			var query = @"INSERT INTO stats (serverID, statType, statTime, statValue, statText, dateGroup)
							VALUES(@serverID, @statType, @statTime, @statValue, @statText, @dateGroup)";
			DataLayerShortcut.ExecuteNonQuery(query, new MySqlParameter("@serverID", res.serverID), new MySqlParameter("@statType", res.statType), new MySqlParameter("@statTime", res.statTime),
				 new MySqlParameter("@statValue", res.statValue), new MySqlParameter("@statText", res.statText), new MySqlParameter("@dateGroup", res.dateGroup));
		}

		public void onHourChanged(object sender, DateTime time)
		{
			foreach (var server in SGMessageBot.BotConfig.BotInfo.DiscordConfig.statServerIds)
			{
				var res = CalculateStat(StatType.userCount, server, time.AddHours(-1.0));
				res.dateGroup = DateGroup.hour;
				insertRow(res);
				res = CalculateStat(StatType.uniqueUsers, server, time.AddHours(-1.0));
				res.dateGroup = DateGroup.hour;
				insertRow(res);
			}
		}

		public void onDayChanged(object sender, DateTime time)
		{
			foreach (var server in SGMessageBot.BotConfig.BotInfo.DiscordConfig.statServerIds)
			{
				var res = CalculateStat(StatType.userCount, server, time.AddHours(-24.0));
				res.dateGroup = DateGroup.day;
				insertRow(res);
				res = CalculateStat(StatType.uniqueUsers, server, time.AddHours(-24.0));
				res.dateGroup = DateGroup.day;
				insertRow(res);
			}
		}

		public void onWeekChanged(object sender, DateTime time)
		{
			foreach (var server in SGMessageBot.BotConfig.BotInfo.DiscordConfig.statServerIds)
			{
				var res = CalculateStat(StatType.userCount, server, time.AddDays(-7.0));
				res.dateGroup = DateGroup.week;
				insertRow(res);
				res = CalculateStat(StatType.uniqueUsers, server, time.AddDays(-7.0));
				res.dateGroup = DateGroup.week;
				insertRow(res);
			}
		}

		public void onMonthChanged(object sender, DateTime time)
		{
			foreach (var server in SGMessageBot.BotConfig.BotInfo.DiscordConfig.statServerIds)
			{
				var res = CalculateStat(StatType.userCount, server, time.AddMonths(-1));
				res.dateGroup = DateGroup.month;
				insertRow(res);
				res = CalculateStat(StatType.uniqueUsers, server, time.AddMonths(-1));
				res.dateGroup = DateGroup.month;
				insertRow(res);
			}
		}

		public void onYearChanged(object sender, DateTime time)
		{
			foreach (var server in SGMessageBot.BotConfig.BotInfo.DiscordConfig.statServerIds)
			{
				var res = CalculateStat(StatType.userCount, server, time.AddYears(-1));
				res.dateGroup = DateGroup.year;
				insertRow(res);
				res = CalculateStat(StatType.uniqueUsers, server, time.AddYears(-1));
				res.dateGroup = DateGroup.year;
				insertRow(res);
			}
		}

		private StatModel CalculateStat(StatType type, ulong serverid, DateTime fromDate)
		{
			var retVal = new StatModel()
			{
				serverID = serverid,
				statType = type,
				statTime = DateTime.UtcNow
			};
			var query = "";
			switch (type)
			{
				case StatType.userCount:
					{
						query = "SELECT COUNT(*) FROM usersinservers WHERE serverID=@serverID AND NOT isDeleted";
						var res = DataLayerShortcut.ExecuteScalarInt(query, new MySqlParameter("@serverID", serverid));
						retVal.statValue = res.HasValue ? res.Value : 0;
						break;
					}
				case StatType.uniqueUsers:
					{
						query = "SELECT COUNT(*) FROM (SELECT COUNT(*) from MESSAGES WHERE mesTime > @statTime GROUP BY userID) cqu";
						var res = DataLayerShortcut.ExecuteScalarInt(query, new MySqlParameter("@statTime", fromDate));
						retVal.statValue = res.HasValue ? res.Value : 0;
						break;
					}
			}
			return retVal;
		}
	}
}
