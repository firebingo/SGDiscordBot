using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	public class StatTracker
	{
		private bool doRun = false;
		private ConcurrentDictionary<StatType, DateTime> NextUpdateTimes;

		public void Start()
		{
			NextUpdateTimes = new ConcurrentDictionary<StatType, DateTime>(new Dictionary<StatType, DateTime>()
			{
				{ StatType.userCount, DateTime.Now.Date },
				{ StatType.uniqueUsers, DateTime.Now.Date }
			});
			Thread runThread = new Thread(RunThread)
			{
				Name = "StatTracker",
				IsBackground = true
			};
			runThread.Start();
			doRun = true;
		}

		public void Stop()
		{
			NextUpdateTimes.Clear();
			doRun = false;
		}

		private void RunThread()
		{
			do
			{
				Parallel.ForEach(NextUpdateTimes, (stat) =>
				{
					foreach (var server in SGMessageBot.botConfig.botInfo.statServerIds)
					{
						if (DateTime.UtcNow > stat.Value)
						{
							var res = CalculateStat(stat.Key, server, out var nextUpdate);
							var query = @"INSERT INTO stats (serverID, statType, statTime, statValue, statText)
							VALUES(@serverID, @statType, @statTime, @statValue, @statText)";
							DataLayerShortcut.ExecuteNonQuery(query, new MySqlParameter("@serverID", res.serverID), new MySqlParameter("@statType", res.statType), new MySqlParameter("@statTime", res.statTime),
								 new MySqlParameter("@statValue", res.statValue), new MySqlParameter("@statText", res.statText));
							NextUpdateTimes[stat.Key] = nextUpdate;
						}
					}
				});
				Thread.Sleep(100);
			} while (doRun);
		}

		private StatModel CalculateStat(StatType type, ulong serverid, out DateTime nextUpdate)
		{
			var retVal = new StatModel()
			{
				serverID = serverid,
				statType = type,
				statTime = DateTime.UtcNow
			};
			nextUpdate = DateTime.UtcNow.AddHours(24);
			var query = "";
			switch (type)
			{
				case StatType.userCount:
					{
						query = "SELECT userCount FROM servers WHERE serverID=@serverID";
						var res = DataLayerShortcut.ExecuteScalarInt(query, new MySqlParameter("@serverID", serverid));
						retVal.statValue = res.HasValue ? res.Value : 0;
						nextUpdate = DateTime.UtcNow.AddHours(24);
						break;
					}
				case StatType.uniqueUsers:
					{
						query = "SELECT COUNT(*) FROM (SELECT COUNT(*) from MESSAGES WHERE mesTime > @statTime GROUP BY userID) cqu";
						var res = DataLayerShortcut.ExecuteScalarInt(query, new MySqlParameter("@statTime", DateTime.UtcNow.AddHours(-24.0)));
						retVal.statValue = res.HasValue ? res.Value : 0;
						nextUpdate = DateTime.UtcNow.AddHours(24);
						break;
					}
			}
			return retVal;
		}
	}
}
