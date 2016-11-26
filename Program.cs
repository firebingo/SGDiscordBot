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
using System.Runtime.Serialization;

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

		public async Task runBot()
		{
			try
			{
				botConfig = new BotConfig();
				var botCResult = botConfig.loadCredConfig();
				BotMention = $"<@{botConfig.credInfo.botId}>";

				#region DB Init
				var dbResult = DataLayerShortcut.loadConfig();
				if (!dbResult.success)
					Console.WriteLine(dbResult.message);
				var dbTestResult = DataLayerShortcut.testConnection();
				if (!dbTestResult.success)
				{
					Console.WriteLine(dbTestResult.message);
					if (!DataLayerShortcut.schemaExists)
					{
						var createResult = DataLayerShortcut.createDataBase();
						if (!createResult.success)
							Console.WriteLine(createResult.message);
					}
				}
				#endregion

				#region Discord Client
				//create new discord client and log
				Client = new DiscordSocketClient(new DiscordSocketConfig()
				{
					MessageCacheSize = 10,
					ConnectionTimeout = int.MaxValue,
					LogLevel = LogSeverity.Warning
				});
				Client.Log += async (message) => Console.WriteLine($"Discord Error:{message.ToString()}");
				await Client.LoginAsync(TokenType.Bot, botConfig.credInfo.token);
				await Client.ConnectAsync();

				var map = new DependencyMap();
				map.Add(Client);

				//setup and add command service.
				cHandler = new BotCommandHandler();
				cProcessor = new BotCommandProcessor();
				map.Add(cHandler);
				map.Add(cProcessor);
				await cHandler.installCommandService(map);

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

				ready = true;
				Console.WriteLine("Ready!");

				//Delay until application quit
				await Task.Delay(-1);

				Console.WriteLine("Exiting!");
			}
			catch (Exception e)
			{
				return;
			}
			#endregion
		}
	}
}

[Serializable]
public class BaseResult
{
	public bool success;
	public string message;
}