using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SGMessageBot.DiscordBot
{
	public class RequireModRoleAttribute : PreconditionAttribute
	{
		public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			if (!(context.User is SocketGuildUser gUser))
			{
				return Task.FromResult(PreconditionResult.FromError("Could not find user for command."));
			}
			var hasPerm = false;
			foreach (var permRole in SGMessageBot.BotConfig.BotInfo.DiscordConfig.commandRoleIds)
			{
				var checkMatch = gUser.Roles.Where(r => r.Id == permRole).FirstOrDefault()?.Id;
				if (checkMatch.HasValue && checkMatch.Value != 0)
					hasPerm = true;
			}
			if (hasPerm)
				return Task.FromResult(PreconditionResult.FromSuccess());
			else
				return Task.FromResult(PreconditionResult.FromError("You do not have permission to run this command."));
		}
	}

	public class RequireGuildMessageAttribute : PreconditionAttribute
	{
		public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			if (!(context.Channel is SocketGuildChannel gChannel))
			{
				return Task.FromResult(PreconditionResult.FromError("Command can only be used on a server"));
			}
			return Task.FromResult(PreconditionResult.FromSuccess());
		}
	}
}
