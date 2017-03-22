using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	public class BotCommandHandler
	{
		private CommandService commands;
		private DiscordSocketClient Client;
		private BotCommandProcessor processor;
		private IDependencyMap map;

		public async Task installCommandService(IDependencyMap _map)
		{
			Client = _map.Get<DiscordSocketClient>();
			processor = _map.Get<BotCommandProcessor>();
			commands = new CommandService();
			_map.Add(commands);
			map = _map;
			await commands.AddModulesAsync(Assembly.GetEntryAssembly());
			Client.MessageReceived += HandleCommand;
		}

		public async Task HandleCommand(SocketMessage e)
		{
			var uMessage = e as SocketUserMessage;
			if (uMessage == null) return;
			int argPos = 0;
			if (uMessage.HasMentionPrefix(Client.CurrentUser, ref argPos))
			{
				var context = new CommandContext(Client, uMessage);
				var result = await commands.ExecuteAsync(context, argPos, map);
				if (!result.IsSuccess)
					await uMessage.Channel.SendMessageAsync(result.ErrorReason);
			}
		}
	}

	[RequireGuildMessage]
	[RequireModRole]
	public class AdminModule : ModuleBase
	{
		private BotCommandProcessor processor;

		public AdminModule(IDependencyMap m)
		{
			processor = m.Get<BotCommandProcessor>();
		}

		[Command("shutdown"), Summary("Tells the bot to shutdown.")]
		public async Task Shutdown()
		{
			await Context.Channel.SendMessageAsync("Goodbye").ConfigureAwait(false);
			Context.Client.StopAsync();
			await Task.Delay(2000).ConfigureAwait(false);
			Environment.Exit(0);
		}

		[Command("restart"), Summary("Tells the bot to restart")]
		public async Task restart()
		{
			await Context.Channel.SendMessageAsync("Restarting...");
			Context.Client.StopAsync();
			await Task.Delay(2000);
			System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
			Environment.Exit(0);
		}

		[Command("reloadmessages"), Summary("Regets all the messages for a given channel.")]
		public async Task reloadMessages([Summary("The channel to reload")] string input = null)
		{
			if(input == null || input == string.Empty)
			{
				var result = await BotExamineServers.updateMessageHistoryChannel(Context);
				await Context.Channel.SendMessageAsync(result);
			}
			else if (input == "all")
			{
				var result = await BotExamineServers.updateMessageHistoryServer(Context);
				await Context.Channel.SendMessageAsync(result);
			}
			else
			{
				await Context.Channel.SendMessageAsync("Invalid input");
			}
		}

		[Command("rolecounts"), Summary("Gets user role counts for server.")]
		public async Task roleCounts([Summary("Whether to mention the roles in the list")]bool useMentions = true)
		{
			var result = "";
			var textChannel = Context.Channel as SocketTextChannel;
			if (textChannel == null)
			{
				await Context.Channel.SendMessageAsync("Channel is not a text channel");
				return;
			}
			result = await processor.calculateRoleCounts(textChannel, useMentions, Context);
			await textChannel.SendMessageAsync(result);
		}

		[Command("rolecounts"), Summary("Gets user role counts for server.")]
		public async Task roleCounts([Summary("The channel to output the list to.")] SocketChannel outputChannel = null, [Summary("Whether to mention the roles in the list")]bool useMentions = true)
		{
			var result = "";
			if (outputChannel == null)
			{
				outputChannel = Context.Channel as SocketChannel;
			}
			var textChannel = outputChannel as SocketTextChannel;
			if(textChannel == null)
			{
				await Context.Channel.SendMessageAsync("Channel is not a text channel");
				return;
			}
			result = await processor.calculateRoleCounts(textChannel, useMentions, Context);
			await textChannel.SendMessageAsync(result);
		}
	}

	//[Group("||")]
	[RequireGuildMessage]
	public class StatsModule : ModuleBase
	{
		private BotCommandProcessor processor;

		public StatsModule(IDependencyMap m)
		{
			processor = m.Get<BotCommandProcessor>();
		}

		[Command("messagecount"), Summary("Gets message counts for the server.")]
		public async Task messageCounts([Summary("the user to get message counts for")] string input = null)
		{
			var result = "";
			var inputParsed = -1;
			bool pRes = int.TryParse(input, out inputParsed);
			inputParsed = pRes ? inputParsed : -1;
			if (input == null || input == String.Empty)
				inputParsed = 0;

			if (inputParsed > -1)
			{
				result = await processor.calculateTopMessageCounts(inputParsed, Context);
				if(result.Contains("||"))
				{
					var results = result.Split(new string[] { "||" }, StringSplitOptions.None);
					foreach(var res in results)
					{
						await Context.Channel.SendMessageAsync(res);
					}
					return;
				}
				else
				{
					await Context.Channel.SendMessageAsync(result);
					return;
				}
			}

			if(Regex.IsMatch(input, @"<@&\d+>"))
			{
				result = await processor.calculateRoleMessageCounts(input, Context);
				await Context.Channel.SendMessageAsync(result);
				return;
			}

			input = input != null ? input.Replace("!", String.Empty) : input;
			result = await processor.calculateUserMessageCounts(input, Context);
			await Context.Channel.SendMessageAsync(result);
			return;
		}

		[Command("emojicount"), Summary("Gets counts of emojis used")]
		public async Task emojiCounts([Summary("The emoji or user to get counts for")] string input = null, 
			[Summary("The emoji to get for a specific user, or the user to get top counts for")] string input2 = null)
		{
			var result = "";
			var inputParsed = -1;
			bool pRes = int.TryParse(input, out inputParsed);
			inputParsed = pRes ? inputParsed : -1;
			if (input == null || input == String.Empty)
				inputParsed = 0;

			if (inputParsed > -1)
			{
				//if we are getting a top n count for a user
				if (input2 != null)
				{
					input2 = input2.Replace("!", string.Empty);
					if (Regex.IsMatch(input2, @"<@\d+>"))
					{
						result = await processor.calculateTopEmojiCountsUser(inputParsed, input2, Context);
						if (result.Contains("||"))
						{
							var results = result.Split(new string[] { "||" }, StringSplitOptions.None);
							foreach (var res in results)
							{
								await Context.Channel.SendMessageAsync(res);
							}
							return;
						}
						else
						{
							await Context.Channel.SendMessageAsync(result);
							return;
						}
					}
				}
				result = await processor.calculateTopEmojiCounts(inputParsed, Context);
				if (result.Contains("||"))
				{
					var results = result.Split(new string[] { "||" }, StringSplitOptions.None);
					foreach (var res in results)
					{
						await Context.Channel.SendMessageAsync(res);
					}
					return;
				}
				else
				{
					await Context.Channel.SendMessageAsync(result);
					return;
				}
			}

			if(Regex.IsMatch(input, @"<:.+:\d+>"))
			{
				result = await processor.calculateEmojiCounts(input, Context);
				await Context.Channel.SendMessageAsync(result);
				return;
			}

			input = input.Replace("!", string.Empty);
			if (Regex.IsMatch(input, @"<@\d+>"))
			{
				result = await processor.calculateUserEmojiCounts(input, Context);
				await Context.Channel.SendMessageAsync(result);
				return;
			}

			await Context.Channel.SendMessageAsync("Invalid Input");
		}

		//This no longer works with newer versions of nadeko so its commented until i figure out an alternative.
		//[Command("nadekocount"), Summary("Gets counts of commands sent to Nadeko Bot")]
		//public async Task nadekoCount([Summary("The user to get counts for")] SocketUser user = null)
		//{
		//	var result = "";
		//	if (user == null)
		//	{
		//		await Context.Channel.SendMessageAsync("Must Specify User");
		//		return;
		//	}
		//	result = await processor.calculateNadekoUserCounts(user, Context);
		//	await Context.Channel.SendMessageAsync(result);
		//	return;
		//}
	}
}
