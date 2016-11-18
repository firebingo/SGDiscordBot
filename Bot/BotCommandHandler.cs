﻿using Discord;
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
			await commands.AddModules(Assembly.GetEntryAssembly());
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
				var result = await commands.Execute(context, argPos, map);
				if (!result.IsSuccess)
					await uMessage.Channel.SendMessageAsync(result.ErrorReason);
			}
		}
	}

	[RequireGuildMessage]
	[RequireModRole]
	public class AdminModule : ModuleBase
	{
		[Command("shutdown"), Summary("Tells the bot to shutdown.")]
		public async Task shutdown()
		{
			await Context.Channel.SendMessageAsync("Goodbye").ConfigureAwait(false);
			await Task.Delay(2000).ConfigureAwait(false);
			Environment.Exit(0);
		}

		[Command("restart"), Summary("Tells the bot to restart")]
		public async Task restart()
		{
			await Context.Channel.SendMessageAsync("Restarting...");
			await Task.Delay(2000);
			System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
			Environment.Exit(0);
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
		public async Task messageCounts([Summary("the user to get message counts for")] string user = null)
		{
			user = user != null ? user.Replace("!", String.Empty) : user;
			var result = await processor.calculateMessageCounts(user);
			var earliest = await processor.getEarlistMessage();
			var totalCount = await processor.getTotalMessageCount();
			if (user == null || user.Trim() == String.Empty)
			{
				result = result.OrderBy(x => x.messageCount).Reverse().ToList();
				if (result.Count == 0)
					await Context.Channel.SendMessageAsync($"No users have sent a message on this server.");
				else
				{
					var mostCount = result.FirstOrDefault();
					var percent = Math.Round(((float)mostCount.messageCount / (float)totalCount) * 100, 2);
					await Context.Channel.SendMessageAsync($"User with most messages: {mostCount.userMention} with {mostCount.messageCount} messages which is {percent}% of the server's messages. As of {earliest.date.ToString("yyyy/MM/dd")}");
				}
			}
			else
			{
				if (result.Count == 0)
					await Context.Channel.SendMessageAsync($"User {user} has not sent any messages.");
				else
				{
					var userCount = result.FirstOrDefault();
					var percent = Math.Round(((float)userCount.messageCount / (float)totalCount) * 100, 2);
					await Context.Channel.SendMessageAsync($"User {userCount.userMention} has sent {userCount.messageCount} messages which is {percent}% of the server's messages. As of {earliest.date.ToString("yyyy/MM/dd")}");
				}
			}
		}
	}
}
