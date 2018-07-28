using Discord.WebSocket;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using SGMessageBot.DataBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SGMessageBot.DiscordBot
{
	public static class OtherFunctions
	{
		private static Dictionary<string, DateTime> times = new Dictionary<string, DateTime>();

		public static void loadTimes()
		{
			if (File.Exists("Data/SGTimes.json"))
			{
				var tempTimes = JsonConvert.DeserializeObject<Dictionary<string, long>>(File.ReadAllText("Data/SGTimes.json"));
				times.Clear();
				foreach(var t in tempTimes)
				{
					times.Add(t.Key, new DateTime(t.Value, DateTimeKind.Utc));
				}
			}
		}

		public static string SGRewatchNext()
		{
			var now = DateTime.UtcNow;
			loadTimes();
			var next = times.FirstOrDefault(x => x.Value > now);
			if (!next.Equals(default(KeyValuePair<string, DateTime>)))
			{
				var data = next.Key.Split('|');
				var nextTime = next.Value - now;
				return $"Next showing is Symphogear {data[1]}{(data[1] != "" ? " " : "") }- Episode {data[0]} in: {nextTime.ToString("h\\hm\\m")}.";
			}
			return "Rewatch has ended.";
		}

		public static void sendMessageTrack(SocketGuild guild)
		{
			var config = SGMessageBot.BotConfig.BotInfo.DiscordConfig.messageCount;
			if(config != null && config.ContainsKey(guild.Id) && config[guild.Id].enabled)
			{
				var count = DataLayerShortcut.ExecuteScalarInt("SELECT COUNT(*) FROM messages WHERE isDeleted = false AND serverID = @serverId", new MySqlParameter("@serverId", guild.Id));
				if (count.HasValue && count.Value >= config[guild.Id].messageCount - 1)
				{
					var channel = guild.GetChannel(config[guild.Id].channelId);
					if (channel != null)
					{
						var gChannel = channel as SocketTextChannel;
						var message = config[guild.Id].message.Replace("%c%", config[guild.Id].messageCount.ToString());
						gChannel?.SendMessageAsync(message);
						config[guild.Id].enabled = false;
						SGMessageBot.BotConfig.SaveCredConfig();
					}
				}
			}
		}

		public static void ShuffleList<T>(this IList<T> list, int? seed = null)
		{
			Random rand = null;
			if (seed.HasValue)
				rand = new Random(seed.Value);
			else
				rand = new Random();
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rand.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
	}
}
