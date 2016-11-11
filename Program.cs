using Discord;
using Discord.Commands;
using MySql.Data.MySqlClient;
using SGMessageBot.Config;
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

		public static DBConfig DBConfig { get; private set; }
		

		static void Main(string[] args)
		{
			Client = new DiscordClient();
			DBConfig = new DBConfig();
			BotConfig = new BotConfig();
			var botCResult = BotConfig.loadCredConfig();
			var dbResult = DBConfig.loadDBConfig();
			BotMention = $"<@{BotConfig.credInfo.botId}>";

			#region DB Open
			try
			{
				MySqlConnection conn;
				conn = new MySql.Data.MySqlClient.MySqlConnection();
				conn.ConnectionString = DBConfig.connectionString;
				conn.Open();
			}
			catch (MySqlException e)
			{
				Console.WriteLine(e.Message);
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
				AllowMentionPrefix = false,
				CustomPrefixHandler = m => 0,
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

			//run the bot
			Client.ExecuteAndWait(async () =>
			{
				try
				{
					await Client.Connect(BotConfig.credInfo.token, TokenType.Bot).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Token is wrong. Don't set a token if you don't have an official BOT account.");
					Console.WriteLine(ex);
					Console.ReadKey();
					return;
				}

				await Task.Delay(1000).ConfigureAwait(false);

				Client.ClientAPI.SendingRequest += (s, e) =>
				{
					var request = e.Request as Discord.API.Client.Rest.SendMessageRequest;
					if (request == null) return;
					// meew0 is magic
					request.Content = request.Content?.Replace("@everyone", "@everyοne").Replace("@here", "@һere") ?? "_error_";
					if (string.IsNullOrWhiteSpace(request.Content))
						e.Cancel = true;
				};
				SGMessageBot.Ready = true;
				SGMessageBot.OnReady();
				Console.WriteLine("Ready!");
				//reply to personal messages and forward if enabled.
				Client.MessageReceived += Client_MessageReceived;
			});
			Console.WriteLine("Exiting...");
			Console.ReadKey();
			#endregion
		}

		private static async void Client_MessageReceived(object sender, MessageEventArgs e)
		{
			try
			{
				await Task.Delay(2000).ConfigureAwait(false);
			}
			catch { }
		}
	}
}

[Serializable]
public class BaseResult
{
	public bool success;
	public string message;
}