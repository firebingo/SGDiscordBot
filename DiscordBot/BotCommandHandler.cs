using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SGMessageBot.AI;
using SGMessageBot.Config;
using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SGMessageBot.DiscordBot
{
	public class BotCommandHandler
	{
		private CommandService commands;
		private DiscordSocketClient socketClient;
		private BotCommandProcessor processor;
		private Markov markovAi;
		private IServiceProvider dependencyMap;
		private BotCommandsRunning running;

		public async Task InstallCommandService(IServiceProvider _map)
		{
			socketClient = _map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient;
			processor = _map.GetService(typeof(BotCommandProcessor)) as BotCommandProcessor;
			markovAi = _map.GetService(typeof(Markov)) as Markov;
			running = new BotCommandsRunning();
			commands = new CommandService();
			dependencyMap = _map;
			await commands.AddModulesAsync(Assembly.GetEntryAssembly(), _map);
			socketClient.MessageReceived += HandleCommand;
		}

		public async Task<bool> RemoveCommandService()
		{
			var modules = commands.Modules.ToList();
			for (var i = modules.Count - 1; i > -1; --i)
			{
				await commands.RemoveModuleAsync(modules[i]);
			}
			running.Clear();
			socketClient.MessageReceived -= HandleCommand;
			return Task.FromResult<bool>(true).Result;
		}

		public Task HandleCommand(SocketMessage e)
		{
			if (!(e is SocketUserMessage uMessage)) return Task.CompletedTask;
			int argPos = 0;
			if (uMessage.HasMentionPrefix(socketClient.CurrentUser, ref argPos))
			{
				if (e.Author.IsBot && SGMessageBot.BotConfig.BotInfo.DiscordConfig.ignoreOtherBots)
					return Task.CompletedTask;
				if (SGMessageBot.BotConfig.BotInfo.DiscordConfig.ignoreCommandsFrom.Contains(e.Author.Id))
					return Task.CompletedTask;
				var context = new CommandContext(socketClient, uMessage);
				//This is bad and I should feel bad.
				//For future the commands themselves shouldnt be dependant on awating the command processor and the
				// processor itself should handle any operations that need to happen after the processing such
				// as SendMessageAsync.
				IResult result = null;
				Thread runThread = new Thread(async () => {
					var guid = Guid.NewGuid();
					running.Add(guid, context.Channel as SocketTextChannel);
					result = await commands.ExecuteAsync(context, argPos, dependencyMap);
					running.Remove(guid);
					if (!result.IsSuccess)
						await uMessage.Channel.SendMessageAsync(result.ErrorReason);
				});
				runThread.Start();

				//var result = await commands.ExecuteAsync(context, argPos, map).ConfigureAwait(false);
				//if (!result.IsSuccess)
				//	await uMessage.Channel.SendMessageAsync(result.ErrorReason);
			}
			return Task.CompletedTask;
		}
	}

	[RequireGuildMessage]
	[RequireModRole]
	public class AdminModule : ModuleBase
	{
		private BotCommandProcessor processor;
		private Markov markovAi;

		public AdminModule(IServiceProvider m)
		{
			processor = m.GetService(typeof(BotCommandProcessor)) as BotCommandProcessor;
			markovAi = m.GetService(typeof(Markov)) as Markov;
		}

		[Command("shutdown"), Summary("Tells the bot to shutdown.")]
		public async Task Shutdown()
		{
			await Context.Channel.SendMessageAsync("Goodbye").ConfigureAwait(false);
			await Context.Client.StopAsync();
			await Task.Delay(2000).ConfigureAwait(false);
			Environment.Exit(0);
		}

		[Command("restart"), Summary("Tells the bot to restart")]
		public async Task Restart()
		{
			await Context.Channel.SendMessageAsync("Restarting...");
			await Context.Client.StopAsync();
			await Task.Delay(2000);
			System.Diagnostics.Process.Start(Assembly.GetExecutingAssembly().Location);
			Environment.Exit(0);
		}

#if DEBUG
		[Command("disconnect"), Summary("For debug purpose, disconnects bot")]
		public void RemoveCommands()
		{
			Task.Run(() => Context.Client.StopAsync());
			return;
		}

		[Command("wait"), Summary("For debug purpose, waits x seconds")]
		public async Task WaitSeconds(int seconds)
		{
			await Task.Delay(seconds * 1000);
			await Context.Channel.SendMessageAsync($"Waited {seconds} seconds");
			return;
		}
#endif

		[Command("reloadmessages"), Summary("Regets all the messages for a given channel.")]
		public async Task ReloadMessages([Summary("The channel to reload")] IMessageChannel channel = null)
		{
			if (channel != null)
			{
				var result = await BotExamineServers.UpdateMessageHistoryChannel(Context, channel);
				await Context.Channel.SendMessageAsync(result);
			}
			else
			{
				var result = await BotExamineServers.UpdateMessageHistoryServer(Context);
				await Context.Channel.SendMessageAsync(result);
			}
		}

		[Command("rolecounts"), Summary("Gets user role counts for server.")]
		public async Task RoleCounts([Summary("Whether to mention the roles in the list")]bool useMentions = true)
		{
			var result = "";
			if (!(Context.Channel is SocketTextChannel textChannel))
			{
				await Context.Channel.SendMessageAsync("Channel is not a text channel");
				return;
			}
			result = await processor.CalculateRoleCounts(textChannel, useMentions, Context);
			await textChannel.SendMessageAsync(result);
		}

		[Command("rolecounts"), Summary("Gets user role counts for server.")]
		public async Task RoleCounts([Summary("The channel to output the list to.")] SocketChannel outputChannel = null, [Summary("Whether to mention the roles in the list")]bool useMentions = true)
		{
			var result = "";
			if (outputChannel == null)
			{
				outputChannel = Context.Channel as SocketChannel;
			}
			if (!(outputChannel is SocketTextChannel textChannel))
			{
				await Context.Channel.SendMessageAsync("Channel is not a text channel");
				return;
			}
			result = await processor.CalculateRoleCounts(textChannel, useMentions, Context);
			await textChannel.SendMessageAsync(result);
		}

		[Command("buildcorpus"), Summary("Rebuilds the corpus for AI chat commands")]
		public async Task BuildCorpus([Summary("Will force a dump of the corpus table and full rebuild")] bool forceRebuild = false)
		{
			try
			{
				await markovAi.RebuildCorpus(forceRebuild);
			}
			catch(Exception ex)
			{
				await Context.Channel.SendMessageAsync($"Exception: {ex.Message}");
			}
			await Context.Channel.SendMessageAsync("Operation Complete");
		}

		[Command("messagetrack"), Summary("Enables sending a message when a specific message count is reached")]
		public async Task MessageTrackSetup(bool enabled, int count, SocketTextChannel channel, string message)
		{
			var newConfig = new MessageCountTracker()
			{
				enabled = enabled,
				messageCount = count,
				channelId = channel.Id,
				message = message
			};
			if (SGMessageBot.BotConfig.BotInfo.DiscordConfig.messageCount == null)
				SGMessageBot.BotConfig.BotInfo.DiscordConfig.messageCount = new Dictionary<ulong, MessageCountTracker>();

			if (SGMessageBot.BotConfig.BotInfo.DiscordConfig.messageCount.ContainsKey(Context.Guild.Id))
				SGMessageBot.BotConfig.BotInfo.DiscordConfig.messageCount[Context.Guild.Id] = newConfig;
			else
				SGMessageBot.BotConfig.BotInfo.DiscordConfig.messageCount.Add(Context.Guild.Id, newConfig);
			SGMessageBot.BotConfig.SaveCredConfig();
			await Context.Channel.SendMessageAsync("Operation Complete");
		}

		[Command("reloadmescount"), Summary("Reloads the message count column for usersinservers")]
		public async Task ReloadMessageCounts()
		{
			await processor.ReloadMessageCounts(Context);
			await Context.Channel.SendMessageAsync("Operation Complete");
		}

		[Command("aprilfools"), Summary("Enable/disable april fools event for a year")]
		public async Task EnableAprilFools(string year, bool enable)
		{
			if (enable)
				AprilFools.StartYear(year);
			else
				AprilFools.EndYear();

			await Context.Channel.SendMessageAsync("Operation Complete");
		}

		[Command("debuglog"), Summary("Enable/disable a debuglog id")]
		public async Task EnableDebugLog(int number, bool enable)
		{
			if (!Enum.IsDefined(typeof(DebugLogTypes), number))
				await Context.Channel.SendMessageAsync($"Debug log {number} does not exist");

			var type = (DebugLogTypes)number;
			if (enable)
			{
				SGMessageBot.BotConfig.BotInfo.debugLogIds.RemoveAll(x => x == type);
				SGMessageBot.BotConfig.BotInfo.debugLogIds.Add(type);
			}
			else
				SGMessageBot.BotConfig.BotInfo.debugLogIds.RemoveAll(x => x == type);

			SGMessageBot.BotConfig.SaveCredConfig();

			await Context.Channel.SendMessageAsync($"Debug log {type} {(enable ? "enabled" : "disabled")}");
		}
	}

	//[Group("||")]
	[RequireGuildMessage]
	public class StatsModule : ModuleBase
	{
		private BotCommandProcessor processor;
		private Markov markovAi;

		public StatsModule(IServiceProvider m)
		{
			processor = m.GetService(typeof(BotCommandProcessor)) as BotCommandProcessor;
			markovAi = m.GetService(typeof(Markov)) as Markov;
		}

		[Command("messagecount"), Summary("Gets message counts for the server.")]
		public async Task MessageCounts([Summary("the user to get message counts for")] string input = null)
		{
			var result = "";
			var inputParsed = -1;
			bool pRes = int.TryParse(input, out inputParsed);
			inputParsed = pRes ? inputParsed : -1;
			if (input == null || input == String.Empty)
				inputParsed = 0;

			if (inputParsed > -1)
			{
				result = await processor.CalculateTopMessageCounts(inputParsed, Context);
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

			if (Regex.IsMatch(input, @"<@&\d+>"))
			{
				result = await processor.CalculateRoleMessageCounts(input, Context);
				await Context.Channel.SendMessageAsync(result);
				return;
			}

			input = input != null ? input.Replace("!", String.Empty) : input;
			result = await processor.CalculateUserMessageCounts(input, Context);
			await Context.Channel.SendMessageAsync(result);
			return;
		}

		[Command("emojicount"), Summary("Gets counts of emojis used")]
		public async Task EmojiCounts([Summary("The emoji or user to get counts for")] string input = null,
			[Summary("The emoji to get for a specific user, or the user to get top counts for")] string input2 = null)
		{
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
							result = await processor.CalculateTopEmojiCountsUser(inputParsed, input2, Context);
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
					result = await processor.CalculateTopEmojiCounts(inputParsed, Context);
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

				if (Regex.IsMatch(input, @"<:.+:\d+>"))
				{
					result = await processor.CalculateEmojiCounts(input, Context);
					await Context.Channel.SendMessageAsync(result);
					return;
				}

				input = input.Replace("!", string.Empty);
				if (Regex.IsMatch(input, @"<@\d+>"))
				{
					result = await processor.CalculateUserEmojiCounts(input, Context);
					await Context.Channel.SendMessageAsync(result);
					return;
				}
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
				await Context.Channel.SendMessageAsync(e.Message);
				return;
			}

			await Context.Channel.SendMessageAsync("Invalid Input");
		}

		[Command("chat"), Summary("Makes the bot return a generated message")]
		public async Task GenerateChatMessage([Summary("The bot will attempt to start the message with this word")] string input = null)
		{
			try
			{
				if (String.IsNullOrWhiteSpace(input))
					input = string.Empty;
				var split = input.Split(' ');
				input = split[0].Trim();
				var result = await markovAi.GenerateMessage(input);
				if(SGMessageBot.BotConfig.BotInfo.DiscordConfig.escapeMentionsChat)
					result = Regex.Replace(result, "(<@.*?>)", "`$1`");
				if(result.Contains("|?|"))
				{
					var sendSplit = result.Split(new string[] { "|?|" }, StringSplitOptions.None);
					foreach(var send in sendSplit)
						await Context.Channel.SendMessageAsync(send);
				}
				else
					await Context.Channel.SendMessageAsync(result);
			}
			catch(Exception ex)
			{
				await Context.Channel.SendMessageAsync(ex.Message);
				return;
			}
		}
	}

	public class BotCommandsRunning
	{
		private Dictionary<Guid, SocketTextChannel> _commands;
		private object _commandsLock;
		private readonly TimerCallback timerCallback;
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
							ErrorLog.WriteError(e);
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

		public void Clear()
		{
			lock(_commandsLock)
			{
				_commands.Clear();
			}
		}

		public void ResetCommandsTimer()
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
