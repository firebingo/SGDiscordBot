using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SGMessageBot.AI;
using SGMessageBot.DataBase;
using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot.DiscordBot
{
	public class DiscordMain
	{
		public static DiscordSocketClient DiscordClient { get; private set; }
		public static bool DiscordReady { get; set; }
		private static BotCommandHandler cDiscordHandler;
		private static BotCommandProcessor cDiscordProcessor;
		private static Markov cDiscordMarkov;
		private static StatTracker DiscordStatTracker;
		private static long discordConnectedTimes = 0;

		public async Task RunBot()
		{
			try
			{
				#region DB Init
				var dbResult = DataLayerShortcut.loadConfig();
				if (!dbResult.Success)
					Console.WriteLine(dbResult.Message);
				var dbTestResult = DataLayerShortcut.testConnection();
				if (!dbTestResult.Success)
					Console.WriteLine(dbTestResult.Message);
				var createResult = DataLayerShortcut.createDataBase();
				if (!createResult.Success)
					Console.WriteLine(createResult.Message);
				#endregion

				#region Other Init
				OtherFunctions.loadTimes();
				DiscordStatTracker = new StatTracker();
				SGMessageBot.TimeThread.AddBindings(DiscordStatTracker.onHourChanged, DiscordStatTracker.onDayChanged, DiscordStatTracker.onWeekChanged, DiscordStatTracker.onMonthChanged, DiscordStatTracker.onYearChanged);
				#endregion

				#region Discord Client
				//create new discord client and log
				DiscordClient = new DiscordSocketClient(new DiscordSocketConfig()
				{
					MessageCacheSize = 10,
					ConnectionTimeout = int.MaxValue,
					LogLevel = LogSeverity.Warning
				});
				DiscordClient.Connected += onConnected;
				DiscordClient.Disconnected += onDisconnected;
				DiscordClient.Log += (message) =>
				{
					Console.WriteLine($"Discord Error:{message.ToString()}");
					ErrorLog.WriteLog($"Discord Error:{message.ToString()}");
					return Task.CompletedTask;
				};
				await DiscordClient.LoginAsync(TokenType.Bot, SGMessageBot.BotConfig.BotInfo.DiscordConfig.token);
				await DiscordClient.StartAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Discord Failed To Start:{ex.ToString()}");
				ErrorLog.WriteError(ex);
				return;
			}
			#endregion
		}



		private async Task onDisconnected(Exception arg)
		{
			try
			{
				await cDiscordHandler.removeCommandService();
				cDiscordHandler = null;
				cDiscordProcessor = null;
				DiscordClient.MessageReceived -= BotEventHandler.ClientMessageReceived;
				DiscordClient.MessageUpdated -= BotEventHandler.ClientMessageUpdated;
				DiscordClient.MessageDeleted -= BotEventHandler.ClientMessageDeleted;
				DiscordClient.JoinedGuild -= BotEventHandler.ClientJoinedServer;
				DiscordClient.GuildUpdated -= BotEventHandler.ClientServerUpdated;
				DiscordClient.UserJoined -= BotEventHandler.ClientUserJoined;
				DiscordClient.UserUnbanned -= BotEventHandler.ClientUserUnbanned;
				DiscordClient.UserBanned -= BotEventHandler.ClientUserBanned;
				DiscordClient.UserLeft -= BotEventHandler.ClientUserLeft;
				DiscordClient.UserUpdated -= BotEventHandler.ClientUserUpdated;
				DiscordClient.GuildMemberUpdated -= BotEventHandler.ClientServerUserUpdated;
				DiscordClient.RoleCreated -= BotEventHandler.ClientRoleCreated;
				DiscordClient.RoleUpdated -= BotEventHandler.ClientRoleUpdated;
				DiscordClient.RoleDeleted -= BotEventHandler.ClientRoleDeleted;
				DiscordClient.ChannelCreated -= BotEventHandler.ClientChannelCreated;
				DiscordClient.ChannelUpdated -= BotEventHandler.ClientChannelUpdated;
				DiscordClient.ChannelDestroyed -= BotEventHandler.ClientChannelDestroyed;
				DiscordClient.ReactionAdded -= BotEventHandler.ClientReactionAdded;
				DiscordClient.ReactionRemoved -= BotEventHandler.ClientReactionRemoved;
				DiscordClient.ReactionsCleared -= BotEventHandler.ClientReactionsCleared;
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
				Console.WriteLine(e.Message);
			}

			Console.WriteLine("Discord Disconnected");
			Console.WriteLine(arg.Message);
			DiscordReady = false;
		}

		private async Task onConnected()
		{
			var serviceProvider = ConfigureServices();
			await cDiscordHandler.installCommandService(serviceProvider);

			//Event hooks
			DiscordClient.MessageReceived += BotEventHandler.ClientMessageReceived;
			DiscordClient.MessageUpdated += BotEventHandler.ClientMessageUpdated;
			DiscordClient.MessageDeleted += BotEventHandler.ClientMessageDeleted;
			DiscordClient.JoinedGuild += BotEventHandler.ClientJoinedServer;
			DiscordClient.GuildUpdated += BotEventHandler.ClientServerUpdated;
			DiscordClient.UserJoined += BotEventHandler.ClientUserJoined;
			DiscordClient.UserUnbanned += BotEventHandler.ClientUserUnbanned;
			DiscordClient.UserBanned += BotEventHandler.ClientUserBanned;
			DiscordClient.UserLeft += BotEventHandler.ClientUserLeft;
			DiscordClient.UserUpdated += BotEventHandler.ClientUserUpdated;
			DiscordClient.GuildMemberUpdated += BotEventHandler.ClientServerUserUpdated;
			DiscordClient.RoleCreated += BotEventHandler.ClientRoleCreated;
			DiscordClient.RoleUpdated += BotEventHandler.ClientRoleUpdated;
			DiscordClient.RoleDeleted += BotEventHandler.ClientRoleDeleted;
			DiscordClient.ChannelCreated += BotEventHandler.ClientChannelCreated;
			DiscordClient.ChannelUpdated += BotEventHandler.ClientChannelUpdated;
			DiscordClient.ChannelDestroyed += BotEventHandler.ClientChannelDestroyed;
			DiscordClient.ReactionAdded += BotEventHandler.ClientReactionAdded;
			DiscordClient.ReactionRemoved += BotEventHandler.ClientReactionRemoved;
			DiscordClient.ReactionsCleared += BotEventHandler.ClientReactionsCleared;

			Task startTask = null;
			if (discordConnectedTimes == 0)
				startTask = BotExamineServers.startupCheck(DiscordClient.Guilds);

			DiscordReady = true;
			Console.WriteLine("Discord Ready!");
			discordConnectedTimes++;
		}

		private IServiceProvider ConfigureServices()
		{
			//setup and add command service.
			cDiscordHandler = new BotCommandHandler();
			cDiscordProcessor = new BotCommandProcessor();
			cDiscordMarkov = new Markov();

			var services = new ServiceCollection()
				.AddSingleton(DiscordClient)
				.AddSingleton(SGMessageBot.BotConfig)
				.AddSingleton(cDiscordHandler)
				.AddSingleton(cDiscordProcessor)
				.AddSingleton(cDiscordMarkov);
			var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
			return provider;
		}
	}
}
