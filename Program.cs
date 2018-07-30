using SGMessageBot.Config;
using System;
using System.Threading.Tasks;
using SGMessageBot.DiscordBot;
using SGMessageBot.SteamBot;

namespace SGMessageBot
{
	class SGMessageBot
	{
		private static DiscordMain DiscordThread;
		private static SteamMain SteamThread;
		public static TimeThread TimeThread;

		public static BotConfig BotConfig { get; private set; }

		static void Main(string[] args)
		{
			BotConfig = new BotConfig();
			var botCResult = BotConfig.LoadConfig();
			BotConfig.SaveCredConfig();

			TimeThread = new TimeThread();
			TimeThread.Start();

			new SGMessageBot().RunApp().GetAwaiter().GetResult();
		}

		private async Task RunApp()
		{
			Task loadDiscord = Task.CompletedTask;
			if (BotConfig.BotInfo.DiscordEnabled)
			{
				DiscordThread = new DiscordMain();
				loadDiscord = Task.Run(() => DiscordThread.RunBot());
			}

			Task loadSteam = Task.CompletedTask;
			if (BotConfig.BotInfo.SteamEnabled)
			{
				SteamThread = new SteamMain();
				loadSteam = Task.Run(() => SteamThread.RunBot());
			}

			await loadDiscord;
			await loadSteam;

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