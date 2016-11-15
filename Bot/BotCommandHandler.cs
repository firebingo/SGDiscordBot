using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	public static class BotCommandHandler
	{
		public static void createCommands(DiscordClient Client)
		{
			Client.GetService<CommandService>().CreateCommand("shutdown").Alias("shutdown").Do(async e =>
			{
				var hasPerm = false;
				foreach (var permRole in SGMessageBot.BotConfig.credInfo.commandRoleIds)
				{
					var checkMatch = e.User.Roles.Where(r => r.Id == permRole).FirstOrDefault();
					if (checkMatch != null)
						hasPerm = true;
				}
				if (hasPerm)
				{
					await e.Channel.SendMessage("Goodbye").ConfigureAwait(false);
					await Task.Delay(2000).ConfigureAwait(false);
					Environment.Exit(0);
				}
			});

			Client.GetService<CommandService>().CreateCommand("restart").Alias("restart").Do(async e =>
			{
				var hasPerm = false;
				foreach (var permRole in SGMessageBot.BotConfig.credInfo.commandRoleIds)
				{
					var checkMatch = e.User.Roles.Where(r => r.Id == permRole).FirstOrDefault();
					if (checkMatch != null)
						hasPerm = true;
				}
				if (hasPerm)
				{
					await e.Channel.SendMessage("Restarting...");
					await Task.Delay(2000);
					System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
					Environment.Exit(0);
				}
			});
		}
	}
}
