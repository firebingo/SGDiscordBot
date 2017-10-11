﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SGMessageBot.AI;
using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	public class BotCommandHandler
	{
		private CommandService commands;
		private DiscordSocketClient Client;
		private BotCommandProcessor processor;
		private Markov markovAi;
		private IServiceProvider map;

		public async Task installCommandService(IServiceProvider _map)
		{
			Client = _map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient;
			processor = _map.GetService(typeof(BotCommandProcessor)) as BotCommandProcessor;
			markovAi = _map.GetService(typeof(Markov)) as Markov;
			commands = new CommandService();
			map = _map;
			await commands.AddModulesAsync(Assembly.GetEntryAssembly());
			Client.MessageReceived += HandleCommand;
		}

		public async Task<bool> removeCommandService()
		{
			var modules = commands.Modules.ToList();
			for (var i = modules.Count - 1; i > -1; --i)
			{
				await commands.RemoveModuleAsync(modules[i]);
			}
			Client.MessageReceived -= HandleCommand;
			return Task.FromResult<bool>(true).Result;
		}

		public async Task HandleCommand(SocketMessage e)
		{
			var uMessage = e as SocketUserMessage;
			if (uMessage == null) return;
			int argPos = 0;
			if (uMessage.HasMentionPrefix(Client.CurrentUser, ref argPos))
			{
				var context = new CommandContext(Client, uMessage);
				//This is bad and I should feel bad.
				//For future the commands themselves shouldnt be dependant on awating the command processor and the
				// processor itself should handle any operations that need to happen after the processing such
				// as SendMessageAsync.
				IResult result = null;
				Thread runThread = new Thread(async () => {
					result = await commands.ExecuteAsync(context, argPos, map);
					if (!result.IsSuccess)
						await uMessage.Channel.SendMessageAsync(result.ErrorReason);
					});
				runThread.Start();

				//var result = await commands.ExecuteAsync(context, argPos, map).ConfigureAwait(false);
				//if (!result.IsSuccess)
				//	await uMessage.Channel.SendMessageAsync(result.ErrorReason);
			}
		}
	}

	[RequireGuildMessage]
	[RequireModRole]
	public class AdminModule : ModuleBase
	{
		private BotCommandProcessor processor;
		private BotCommandsRunning running;
		private Markov markovAi;

		public AdminModule(IServiceProvider m)
		{
			processor = m.GetService(typeof(BotCommandProcessor)) as BotCommandProcessor;
			running = m.GetService(typeof(BotCommandsRunning)) as BotCommandsRunning;
			markovAi = m.GetService(typeof(Markov)) as Markov;
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

#if DEBUG
		[Command("disconnect"), Summary("For debug purpose, disconnects bot")]
		public async Task removeCommands()
		{
			Context.Client.StopAsync();
			return;
		}

		[Command("wait"), Summary("For debug purpose, waits x seconds")]
		public async Task waitSeconds(int seconds)
		{
			var guid = Guid.NewGuid();
			running.Add(guid, Context.Channel as SocketTextChannel);
			Context.Channel.TriggerTypingAsync();
			Thread.Sleep(seconds * 1000);
			running.Remove(guid);
			return;
		}
#endif

		[Command("reloadmessages"), Summary("Regets all the messages for a given channel.")]
		public async Task reloadMessages([Summary("The channel to reload")] IMessageChannel channel = null)
		{
			var guid = Guid.NewGuid();
			running.Add(guid, Context.Channel as SocketTextChannel);
			if (channel != null)
			{
				var result = await BotExamineServers.updateMessageHistoryChannel(Context, channel);
				running.Remove(guid);
				await Context.Channel.SendMessageAsync(result);
			}
			else
			{
				var result = await BotExamineServers.updateMessageHistoryServer(Context);
				running.Remove(guid);
				await Context.Channel.SendMessageAsync(result);
			}
		}

		[Command("rolecounts"), Summary("Gets user role counts for server.")]
		public async Task roleCounts([Summary("Whether to mention the roles in the list")]bool useMentions = true)
		{
			var guid = Guid.NewGuid();
			running.Add(guid, Context.Channel as SocketTextChannel);
			var result = "";
			var textChannel = Context.Channel as SocketTextChannel;
			if (textChannel == null)
			{
				await Context.Channel.SendMessageAsync("Channel is not a text channel");
				running.Remove(guid);
				return;
			}
			result = await processor.calculateRoleCounts(textChannel, useMentions, Context);
			await textChannel.SendMessageAsync(result);
			running.Remove(guid);
		}

		[Command("rolecounts"), Summary("Gets user role counts for server.")]
		public async Task roleCounts([Summary("The channel to output the list to.")] SocketChannel outputChannel = null, [Summary("Whether to mention the roles in the list")]bool useMentions = true)
		{
			var guid = Guid.NewGuid();
			running.Add(guid, Context.Channel as SocketTextChannel);
			var result = "";
			if (outputChannel == null)
			{
				outputChannel = Context.Channel as SocketChannel;
			}
			var textChannel = outputChannel as SocketTextChannel;
			if (textChannel == null)
			{
				await Context.Channel.SendMessageAsync("Channel is not a text channel");
				running.Remove(guid);
				return;
			}
			result = await processor.calculateRoleCounts(textChannel, useMentions, Context);
			await textChannel.SendMessageAsync(result);
			running.Remove(guid);
		}

		[Command("buildcorpus"), Summary("Rebuilds the corpus for AI chat commands")]
		public async Task buildCorpus()
		{
			var guid = Guid.NewGuid();
			running.Add(guid, Context.Channel as SocketTextChannel);
			try
			{
				await markovAi.rebuildCorpus();
			}
			catch(Exception e)
			{
				await Context.Channel.SendMessageAsync($"Exception: {e.Message}");
				running.Remove(guid);
			}
			await Context.Channel.SendMessageAsync("Operation Complete");
			running.Remove(guid);
		}
	}

	//[Group("||")]
	[RequireGuildMessage]
	public class StatsModule : ModuleBase
	{
		private BotCommandProcessor processor;
		private BotCommandsRunning running;
		private Markov markovAi;

		public StatsModule(IServiceProvider m)
		{
			processor = m.GetService(typeof(BotCommandProcessor)) as BotCommandProcessor;
			running = m.GetService(typeof(BotCommandsRunning)) as BotCommandsRunning;
			markovAi = m.GetService(typeof(Markov)) as Markov;
		}

		[Command("messagecount"), Summary("Gets message counts for the server.")]
		public async Task messageCounts([Summary("the user to get message counts for")] string input = null)
		{
			var guid = Guid.NewGuid();
			running.Add(guid, Context.Channel as SocketTextChannel);
			var result = "";
			var inputParsed = -1;
			bool pRes = int.TryParse(input, out inputParsed);
			inputParsed = pRes ? inputParsed : -1;
			if (input == null || input == String.Empty)
				inputParsed = 0;

			if (inputParsed > -1)
			{
				result = await processor.calculateTopMessageCounts(inputParsed, Context);
				if (result.Contains("||"))
				{
					var results = result.Split(new string[] { "||" }, StringSplitOptions.None);
					foreach (var res in results)
					{
						await Context.Channel.SendMessageAsync(res);
					}
					running.Remove(guid);
					return;
				}
				else
				{
					await Context.Channel.SendMessageAsync(result);
					running.Remove(guid);
					return;
				}
			}

			if (Regex.IsMatch(input, @"<@&\d+>"))
			{
				result = await processor.calculateRoleMessageCounts(input, Context);
				await Context.Channel.SendMessageAsync(result);
				running.Remove(guid);
				return;
			}

			input = input != null ? input.Replace("!", String.Empty) : input;
			result = await processor.calculateUserMessageCounts(input, Context);
			await Context.Channel.SendMessageAsync(result);
			running.Remove(guid);
			return;
		}

		[Command("emojicount"), Summary("Gets counts of emojis used")]
		public async Task emojiCounts([Summary("The emoji or user to get counts for")] string input = null,
			[Summary("The emoji to get for a specific user, or the user to get top counts for")] string input2 = null)
		{
			var guid = Guid.NewGuid();
			running.Add(guid, Context.Channel as SocketTextChannel);
			var result = "";
			var inputParsed = -1;
			bool pRes = int.TryParse(input, out inputParsed);
			inputParsed = pRes ? inputParsed : -1;
			if (input == null || input == String.Empty)
				inputParsed = 0;

			try
			{
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
								running.Remove(guid);
								return;
							}
							else
							{
								await Context.Channel.SendMessageAsync(result);
								running.Remove(guid);
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
						running.Remove(guid);
						return;
					}
					else
					{
						await Context.Channel.SendMessageAsync(result);
						running.Remove(guid);
						return;
					}
				}

				if (Regex.IsMatch(input, @"<:.+:\d+>"))
				{
					result = await processor.calculateEmojiCounts(input, Context);
					await Context.Channel.SendMessageAsync(result);
					running.Remove(guid);
					return;
				}

				input = input.Replace("!", string.Empty);
				if (Regex.IsMatch(input, @"<@\d+>"))
				{
					result = await processor.calculateUserEmojiCounts(input, Context);
					await Context.Channel.SendMessageAsync(result);
					running.Remove(guid);
					return;
				}
			}
			catch (Exception e)
			{
				running.Remove(guid);
				await Context.Channel.SendMessageAsync(e.Message);
				return;
			}

			running.Remove(guid);
			await Context.Channel.SendMessageAsync("Invalid Input");
		}

		[Command("chat"), Summary("Makes the bot return a generated message")]
		public async Task generateChatMessage()
		{
			var guid = Guid.NewGuid();
			running.Add(guid, Context.Channel as SocketTextChannel);
			try
			{
				var result = await markovAi.generateMessage();
				await Context.Channel.SendMessageAsync(result);
			}
			catch(Exception e)
			{
				running.Remove(guid);
				await Context.Channel.SendMessageAsync(e.Message);
				return;
			}
			running.Remove(guid);
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

	public class BotCommandsRunning
	{
		private Dictionary<Guid, SocketTextChannel> _commands;
		private object _commandsLock;
		private TimerCallback timerCallback;
		private Timer timer;

		public BotCommandsRunning()
		{
			_commands = new Dictionary<Guid, SocketTextChannel>();
			timerCallback = CycleCommandCheck;
			timer = new Timer(timerCallback, null, 0, 5000);
			_commandsLock = new object();
		}

		private void CycleCommandCheck(object objectInfo)
		{
			if (_commands != null && _commands.Count > 0)
			{
				lock (_commandsLock)
				{
					foreach (var c in _commands)
					{
						try
						{
							if(c.Value != null)
								c.Value.TriggerTypingAsync();
						}
						catch (Exception e)
						{
							ErrorLog.writeLog(e.Message);
							continue;
						}
					}
				}
			}
		}

		public void Add(Guid key, SocketTextChannel value)
		{
			lock (_commandsLock)
			{
				_commands.Add(key, value);
			}
		}

		public void Remove(Guid key)
		{
			lock (_commandsLock)
			{
				_commands.Remove(key);
			}
		}

		public void resetCommandsTimer()
		{
			lock (_commandsLock)
			{
				_commands = new Dictionary<Guid, SocketTextChannel>();
				timer.Dispose();
			}
			_commandsLock = new object();
		}
	}
}
