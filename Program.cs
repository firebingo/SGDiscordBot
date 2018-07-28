using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using SGMessageBot.Config;
using SGMessageBot.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SGMessageBot.Helpers;
using SGMessageBot.AI;
using System.Threading;
using SGMessageBot.DiscordBot;

namespace SGMessageBot
{
	class SGMessageBot
	{
		private static DiscordMain DiscordThread;
		public static TimeThread TimeThread;

		public static BotConfig BotConfig { get; private set; }

		static void Main(string[] args)
		{
			BotConfig = new BotConfig();
			var botCResult = BotConfig.LoadConfig();
			BotConfig.SaveCredConfig();

			TimeThread = new TimeThread();
			TimeThread.Start();

			new SGMessageBot().runApp().GetAwaiter().GetResult();
		}

		private async Task runApp()
		{
			Task loadDiscord = Task.CompletedTask;
			if (BotConfig.BotInfo.DiscordEnabled)
			{
				DiscordThread = new DiscordMain();
				loadDiscord = Task.Run(() => DiscordThread.RunBot());
			}

			await loadDiscord;

			//Delay until application quit
			await Task.Delay(-1);

			TimeThread.Stop();

			Console.WriteLine("Exiting!");
		}
	}
}

[Serializable]
public class BaseResult
{
	public bool Success;
	public string Message;
}