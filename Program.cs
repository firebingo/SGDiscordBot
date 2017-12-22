using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using SGMessageBot.Bot;
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

namespace SGMessageBot
{
	class SGMessageBot
	{
		static void Main(string[] args)
		{
			new SGMessageBot().runBot().GetAwaiter().GetResult();
		}

		public static DiscordSocketClient Client { get; private set; }
		public static BotConfig botConfig { get; private set; }		
		public static string BotMention = "";
		public static bool ready { get; set; }
		private static BotCommandHandler cHandler;
		private static BotCommandProcessor cProcessor;
		private static Markov cMarkov;
		private static long connectedTimes = 0;

		public async Task runBot()
		{
			try
			{
				botConfig = new BotConfig();
				var botCResult = botConfig.loadCredConfig();
				BotMention = $"<@{botConfig.botInfo.botId}>";

				#region DB Init
				var dbResult = DataLayerShortcut.loadConfig();
				if (!dbResult.success)
					Console.WriteLine(dbResult.message);
				var dbTestResult = DataLayerShortcut.testConnection();
				if (!dbTestResult.success)
					Console.WriteLine(dbTestResult.message);
				var createResult = DataLayerShortcut.createDataBase();
				if (!createResult.success)
					Console.WriteLine(createResult.message);
				#endregion

				#region Other Init
				OtherFunctions.loadTimes();
				#endregion

				#region Discord Client
				//create new discord client and log
				Client = new DiscordSocketClient(new DiscordSocketConfig()
				{
					MessageCacheSize = 10,
					ConnectionTimeout = int.MaxValue,
					LogLevel = LogSeverity.Warning
				});
				Client.Connected += onConnected;
				Client.Disconnected += onDisconnected;
				Client.Log += async (message) => Console.WriteLine($"Discord Error:{message.ToString()}");
				await Client.LoginAsync(TokenType.Bot, botConfig.botInfo.token);
				await Client.StartAsync();

				//Delay until application quit
				await Task.Delay(-1);

				Console.WriteLine("Exiting!");
			}
			catch (Exception e)
			{
				ErrorLog.writeError(e);
				return;
			}
			#endregion
		}

		private async Task onDisconnected(Exception arg)
		{
			try
			{
				await cHandler.removeCommandService();
				cHandler = null;
				cProcessor = null;
				Client.MessageReceived -= BotEventHandler.ClientMessageReceived;
				Client.MessageUpdated -= BotEventHandler.ClientMessageUpdated;
				Client.MessageDeleted -= BotEventHandler.ClientMessageDeleted;
				Client.JoinedGuild -= BotEventHandler.ClientJoinedServer;
				Client.GuildUpdated -= BotEventHandler.ClientServerUpdated;
				Client.UserJoined -= BotEventHandler.ClientUserJoined;
				Client.UserUnbanned -= BotEventHandler.ClientUserUnbanned;
				Client.UserBanned -= BotEventHandler.ClientUserBanned;
				Client.UserLeft -= BotEventHandler.ClientUserLeft;
				Client.UserUpdated -= BotEventHandler.ClientUserUpdated;
				Client.GuildMemberUpdated -= BotEventHandler.ClientServerUserUpdated;
				Client.RoleCreated -= BotEventHandler.ClientRoleCreated;
				Client.RoleUpdated -= BotEventHandler.ClientRoleUpdated;
				Client.RoleDeleted -= BotEventHandler.ClientRoleDeleted;
				Client.ChannelCreated -= BotEventHandler.ClientChannelCreated;
				Client.ChannelUpdated -= BotEventHandler.ClientChannelUpdated;
				Client.ChannelDestroyed -= BotEventHandler.ClientChannelDestroyed;
				Client.ReactionAdded -= BotEventHandler.ClientReactionAdded;
				Client.ReactionRemoved -= BotEventHandler.ClientReactionRemoved;
				Client.ReactionsCleared -= BotEventHandler.ClientReactionsCleared;
			}
			catch (Exception e)
			{
				ErrorLog.writeError(e);
				Console.WriteLine(e.Message);
			}

			Console.WriteLine("Disconnected");
			Console.WriteLine(arg.Message);
			ready = false;
		}

		private async Task onConnected()
		{
			var serviceProvider = ConfigureServices();
			await cHandler.installCommandService(serviceProvider);

			if(connectedTimes == 0)
				await BotExamineServers.startupCheck(Client.Guilds);

			//Event hooks
			Client.MessageReceived += BotEventHandler.ClientMessageReceived;
			Client.MessageUpdated += BotEventHandler.ClientMessageUpdated;
			Client.MessageDeleted += BotEventHandler.ClientMessageDeleted;
			Client.JoinedGuild += BotEventHandler.ClientJoinedServer;
			Client.GuildUpdated += BotEventHandler.ClientServerUpdated;
			Client.UserJoined += BotEventHandler.ClientUserJoined;
			Client.UserUnbanned += BotEventHandler.ClientUserUnbanned;
			Client.UserBanned += BotEventHandler.ClientUserBanned;
			Client.UserLeft += BotEventHandler.ClientUserLeft;
			Client.UserUpdated += BotEventHandler.ClientUserUpdated;
			Client.GuildMemberUpdated += BotEventHandler.ClientServerUserUpdated;
			Client.RoleCreated += BotEventHandler.ClientRoleCreated;
			Client.RoleUpdated += BotEventHandler.ClientRoleUpdated;
			Client.RoleDeleted += BotEventHandler.ClientRoleDeleted;
			Client.ChannelCreated += BotEventHandler.ClientChannelCreated;
			Client.ChannelUpdated += BotEventHandler.ClientChannelUpdated;
			Client.ChannelDestroyed += BotEventHandler.ClientChannelDestroyed;
			Client.ReactionAdded += BotEventHandler.ClientReactionAdded;
			Client.ReactionRemoved += BotEventHandler.ClientReactionRemoved;
			Client.ReactionsCleared += BotEventHandler.ClientReactionsCleared;

			ready = true;
			Console.WriteLine("Ready!");
			connectedTimes++;
		}

		private Task Client_ReactionsCleared(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2)
		{
			throw new NotImplementedException();
		}

		private IServiceProvider ConfigureServices()
		{
			//setup and add command service.
			cHandler = new BotCommandHandler();
			cProcessor = new BotCommandProcessor();
			cMarkov = new Markov();

			var services = new ServiceCollection()
				.AddSingleton(Client)
				.AddSingleton(botConfig)
				.AddSingleton(cHandler)
				.AddSingleton(cProcessor)
				.AddSingleton(cMarkov);
			var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
			return provider;
		}
	}
}

[Serializable]
public class BaseResult
{
	public bool success;
	public string message;
}