using Discord.WebSocket;
using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SGMessageBot.DiscordBot
{
	public static class AprilFools
	{
		private static string runningYear = string.Empty;

		public static void StartYear(string year)
		{
			if (runningYear == string.Empty)
			{
				runningYear = year;
				switch (runningYear)
				{
					case "2018":
						StartDdlc2018();
						break;
				}
			}
		}

		public static void EndYear()
		{
			runningYear = string.Empty;
		}

		public static void Madoka2017(SocketMessage e)
		{
			if (runningYear == "2017")
			{
				Regex reg = new Regex("madoka.*\\?");
				Regex reg1 = new Regex("why.*madoka");
				if (reg.IsMatch(e.Content.ToLower()) || reg1.IsMatch(e.Content.ToLower()))
				{
					e.Channel.SendMessageAsync("*Check the date*");
				}
			}
		}

		public static void StartDdlc2018()
		{
			Thread ddlcThread = new Thread(Ddlc2018);
			ddlcThread.Start();
		}

		public static void Ddlc2018()
		{
			char[] seed = "Just Monika".ToCharArray();
			var rand = new Random(seed.Sum(x => (int)x));
			double runningTime = 0.0;
			do
			{
				try
				{
					Thread.Sleep(60000);
					runningTime += 60;
					if (!SGMessageBot.BotConfig.BotInfo.DiscordConfig.randomMessageSend.ContainsKey("ddlc"))
					{
						runningYear = string.Empty;
						return;
					}
					var toCheck = SGMessageBot.BotConfig.BotInfo.DiscordConfig.randomMessageSend["ddlc"];
					var percent = (runningTime / toCheck.maxSeconds) * 100;
					var pickList = new List<bool>();
					for (var i = 0; i < 100; ++i)
					{
						if (i < percent - 1)
							pickList.Add(true);
						else
							pickList.Add(false);
					}
					var r = rand.Next(0, 100);
					pickList.ShuffleList();
					if (pickList[r])
					{
						if (DiscordMain.DiscordClient.GetGuild(toCheck.serverId) == null)
						{
							runningYear = string.Empty;
							return;
						}
						if (!(DiscordMain.DiscordClient.GetChannel(toCheck.channelId) is SocketTextChannel channel))
						{
							runningYear = string.Empty;
							return;
						}
						var r2 = rand.Next(0, toCheck.messagesToPick.Count);
						channel.SendMessageAsync(toCheck.messagesToPick[r2]);
						runningTime = 0;
					}
				}
				catch(Exception ex)
				{
					runningYear = string.Empty;
					ErrorLog.WriteLog($"message: {ex.Message}, trace: {ex.StackTrace}");
					return;
				}
			} while (runningYear == "2018");
		}
	}
}
