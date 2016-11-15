using Discord;
using Discord.Commands;
using MySql.Data.MySqlClient;
using SGMessageBot.Bot;
using SGMessageBot.Config;
using SGMessageBot.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot
{
	class SGMessageBot
	{
		public static DiscordClient Client { get; private set; }
		public static BotConfig BotConfig { get; private set; }
		public static string BotMention = "";
		public static bool Ready { get; set; }
		public static Action OnReady = delegate { };
		
		static void Main(string[] args)
		{
			Client = new DiscordClient();
			BotConfig = new BotConfig();
			var botCResult = BotConfig.loadCredConfig();
			BotMention = $"<@{BotConfig.credInfo.botId}>";

			#region DB Init
			var dbResult = DataLayerShortcut.loadConfig();
			if(!dbResult.success)
				Console.WriteLine(dbResult.message);
			var dbTestResult = DataLayerShortcut.testConnection();
			if (!dbTestResult.success)
			{
				Console.WriteLine(dbTestResult.message);
				if(!DataLayerShortcut.schemaExists)
				{
					var createResult = DataLayerShortcut.createDataBase();
					if(!createResult.success)
						Console.WriteLine(createResult.message);
				}
			}
			#endregion

			#region Discord Client
			//create new discord client and log
			Client = new DiscordClient(new DiscordConfigBuilder()
			{
				MessageCacheSize = 10,
				ConnectionTimeout = int.MaxValue,
				LogLevel = LogSeverity.Warning,
				LogHandler = (s, e) =>
					Console.WriteLine($"Severity: {e.Severity}" +
									  $"ExceptionMessage: {e.Exception?.Message ?? "-"}" +
									  $"Message: {e.Message}"),
			});

			//create a command service
			var commandService = new CommandService(new CommandServiceConfigBuilder
			{
				AllowMentionPrefix = true,
				HelpMode = HelpMode.Disabled,
				ErrorHandler = async (s, e) =>
				{
					if (e.ErrorType != CommandErrorType.BadPermissions)
						return;
					if (string.IsNullOrWhiteSpace(e.Exception?.Message))
						return;
					try
					{
						await e.Channel.SendMessage(e.Exception.Message).ConfigureAwait(false);
					}
					catch { }
				}
			});

			//add command service
			Client.AddService<CommandService>(commandService);
			BotCommandHandler.createCommands(Client);

			//run the bot
			Client.ExecuteAndWait(async () =>
			{
				try
				{
					await Client.Connect(BotConfig.credInfo.token, TokenType.Bot).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Incorrect Token");
					Console.WriteLine(ex);
					Console.ReadKey();
					return;
				}

				await Task.Delay(1500).ConfigureAwait(false);

				Client.ClientAPI.SentRequest += (s, e) =>
				{
					Console.WriteLine($"[Request of type {e.Request.GetType()} sent in {e.Milliseconds}]");

					var request = e.Request as Discord.API.Client.Rest.SendMessageRequest;
					if (request == null) return;

					Console.WriteLine($"[Content: { request.Content }");
				};
				await BotExamineServers.startupCheck(Client.Servers);
				Console.WriteLine("Ready!");
				SGMessageBot.Ready = true;
				SGMessageBot.OnReady();
				Client.MessageReceived += BotEventHandler.ClientMessageReceived;
				Client.MessageUpdated += BotEventHandler.ClientMessageUpdated;
				Client.MessageDeleted += BotEventHandler.ClientMessageDeleted;
				Client.JoinedServer += BotEventHandler.ClientJoinedServer;
				Client.UserJoined += BotEventHandler.ClientUserJoined;
				Client.UserUpdated += BotEventHandler.ClientUserUpdated;
				Client.UserLeft += BotEventHandler.ClientUserLeft;
				Client.UserBanned += BotEventHandler.ClientUserBanned;
				Client.UserUnbanned += BotEventHandler.ClientUserUnbanned;
				Client.ServerUpdated += BotEventHandler.ClientServerUpdated;
				Client.RoleCreated += BotEventHandler.ClientRoleCreated;
				Client.RoleUpdated += BotEventHandler.ClientRoleUpdated;
				Client.RoleDeleted += BotEventHandler.ClientRoleDeleted;
				Client.ChannelCreated += BotEventHandler.ClientChannelCreated;
				Client.ChannelUpdated += BotEventHandler.ClientChannelUpdated;
				Client.ChannelDestroyed += BotEventHandler.ClientChannelDestroyed;
			});
			Console.WriteLine("Exiting...");
			Console.ReadKey();
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