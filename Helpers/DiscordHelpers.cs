using Discord;
using Discord.WebSocket;

namespace SGMessageBot.Helpers
{
	internal static class DiscordHelpers
	{
		public static SocketGuild GetGuildFromChannel(ISocketMessageChannel channel)
		{
			return channel.GetType().Name switch
			{
				nameof(SocketGuildChannel) => (channel as SocketGuildChannel).Guild,
				nameof(SocketTextChannel) => (channel as SocketTextChannel).Guild,
				nameof(SocketThreadChannel) => (channel as SocketThreadChannel).Guild,
				nameof(SocketVoiceChannel) => (channel as SocketVoiceChannel).Guild,
				nameof(SocketForumChannel) => (channel as SocketForumChannel).Guild,
				_ => null
			};
		}

		public static SocketGuild GetGuildFromChannel(IMessageChannel channel)
		{
			return channel.GetType().Name switch
			{
				nameof(SocketGuildChannel) => (channel as SocketGuildChannel).Guild,
				nameof(SocketTextChannel) => (channel as SocketTextChannel).Guild,
				nameof(SocketThreadChannel) => (channel as SocketThreadChannel).Guild,
				nameof(SocketVoiceChannel) => (channel as SocketVoiceChannel).Guild,
				nameof(SocketForumChannel) => (channel as SocketForumChannel).Guild,
				_ => null
			};
		}

		public static bool TryGetGuildFromChannel(IMessageChannel channel, out SocketGuild guild)
		{
			guild = null;
			guild = GetGuildFromChannel(channel);
			if (guild == null)
				return false;
			return true;
		}

		public static string GetChannelMention(SocketGuildChannel channel)
		{
			return channel.GetType().Name switch
			{
				nameof(SocketTextChannel) => (channel as SocketTextChannel).Mention,
				nameof(SocketThreadChannel) => (channel as SocketThreadChannel).Mention,
				nameof(SocketVoiceChannel) => (channel as SocketVoiceChannel).Mention,
				nameof(SocketForumChannel) => (channel as SocketForumChannel).Mention,
				_ => null
			};
		}
	}
}
