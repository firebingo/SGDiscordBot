using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using SGMessageBot.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SGMessageBot.DiscordBot
{
	public class StatTracker
	{
		private void InsertRow(StatModel res)
		{
			var query = @"INSERT INTO stats (serverID, statType, statTime, statValue, statText, dateGroup)
							VALUES(@serverID, @statType, @statTime, @statValue, @statText, @dateGroup)";
			Task.Run(() => DataLayerShortcut.ExecuteNonQuery(query, new MySqlParameter("@serverID", res.serverID), new MySqlParameter("@statType", res.statType), new MySqlParameter("@statTime", res.statTime),
				 new MySqlParameter("@statValue", res.statValue), new MySqlParameter("@statText", res.statText), new MySqlParameter("@dateGroup", res.dateGroup)));
		}

		public async Task OnHourChanged(object sender, DateTime time)
		{
			foreach (var server in SGMessageBot.BotConfig.BotInfo.DiscordConfig.statServerIds)
			{
				try
				{
					var res = await CalculateStat(StatType.userCount, server, time.AddHours(-1.0));
					res.dateGroup = DateGroup.hour;
					InsertRow(res);
					res = await CalculateStat(StatType.uniqueUsers, server, time.AddHours(-1.0));
					res.dateGroup = DateGroup.hour;
					InsertRow(res);
				}
				catch (Exception ex)
				{
					ErrorLog.WriteError(ex);
				}
			}
		}

		public async Task OnDayChanged(object sender, DateTime time)
		{
			foreach (var server in SGMessageBot.BotConfig.BotInfo.DiscordConfig.statServerIds)
			{
				try
				{
					var res = await CalculateStat(StatType.userCount, server, time.AddHours(-24.0));
					res.dateGroup = DateGroup.day;
					InsertRow(res);
					res = await CalculateStat(StatType.uniqueUsers, server, time.AddHours(-24.0));
					res.dateGroup = DateGroup.day;
					InsertRow(res);
				}
				catch (Exception ex)
				{
					ErrorLog.WriteError(ex);
				}
			}
		}

		public async Task OnWeekChanged(object sender, DateTime time)
		{
			foreach (var server in SGMessageBot.BotConfig.BotInfo.DiscordConfig.statServerIds)
			{
				try
				{
					var res = await CalculateStat(StatType.userCount, server, time.AddDays(-7.0));
					res.dateGroup = DateGroup.week;
					InsertRow(res);
					res = await CalculateStat(StatType.uniqueUsers, server, time.AddDays(-7.0));
					res.dateGroup = DateGroup.week;
					InsertRow(res);
				}
				catch (Exception ex)
				{
					ErrorLog.WriteError(ex);
				}
			}
		}

		public async Task OnMonthChanged(object sender, DateTime time)
		{
			foreach (var server in SGMessageBot.BotConfig.BotInfo.DiscordConfig.statServerIds)
			{
				try
				{
					var res = await CalculateStat(StatType.userCount, server, time.AddMonths(-1));
					res.dateGroup = DateGroup.month;
					InsertRow(res);
					res = await CalculateStat(StatType.uniqueUsers, server, time.AddMonths(-1));
					res.dateGroup = DateGroup.month;
					InsertRow(res);
				}
				catch (Exception ex)
				{
					ErrorLog.WriteError(ex);
				}
			}
		}

		public async Task OnYearChanged(object sender, DateTime time)
		{
			foreach (var server in SGMessageBot.BotConfig.BotInfo.DiscordConfig.statServerIds)
			{
				try
				{
					var res = await CalculateStat(StatType.userCount, server, time.AddYears(-1));
					res.dateGroup = DateGroup.year;
					InsertRow(res);
					res = await CalculateStat(StatType.uniqueUsers, server, time.AddYears(-1));
					res.dateGroup = DateGroup.year;
					InsertRow(res);
				}
				catch (Exception ex)
				{
					ErrorLog.WriteError(ex);
				}
			}
		}

		private async Task<StatModel> CalculateStat(StatType type, ulong serverid, DateTime fromDate)
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
						var res = await DataLayerShortcut.ExecuteScalarInt(query, new MySqlParameter("@serverID", serverid));
						retVal.statValue = res ?? 0;
						break;
					}
				case StatType.uniqueUsers:
					{
						query = "SELECT COUNT(*) FROM (SELECT COUNT(*) from MESSAGES WHERE mesTime > @statTime AND serverID=@serverID GROUP BY userID) cqu";
						var res = await DataLayerShortcut.ExecuteScalarInt(query, new MySqlParameter("@statTime", fromDate), new MySqlParameter("@serverID", serverid));
						retVal.statValue = res ?? 0;
						break;
					}
			}
			return retVal;
		}
	}
}
