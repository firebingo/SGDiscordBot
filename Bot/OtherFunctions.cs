using Discord.WebSocket;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using SGMessageBot.DataBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SGMessageBot.Bot
{
	public static class OtherFunctions
	{
		//This should probably be in a data file somehow instead of being compiled, but oh well, its temporary.
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
			var config = SGMessageBot.botConfig.botInfo.messageCount;
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
						SGMessageBot.botConfig.saveCredConfig();
					}
				}
			}
		}
	}
}
