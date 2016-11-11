using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	public static class BotEventHandler
	{
		public static async void Client_MessageReceived(object sender, MessageEventArgs e)
		{
			try
			{
				await Task.Delay(2000).ConfigureAwait(false);
			}
			catch { }
		}
	}
}
