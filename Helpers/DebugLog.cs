using System;
using System.IO;
using System.Threading.Tasks;

namespace SGMessageBot.Helpers
{
	public enum DebugLogTypes
	{
		Undefined = 0,
		StatTracker = 1
	}

	public static class DebugLog
	{
		private static readonly object locker = new object();
		private static readonly string folderLocation = string.Empty;
		static DebugLog()
		{
			folderLocation = "Data/Logs";
			if (!Directory.Exists(folderLocation))
				Directory.CreateDirectory(folderLocation);
		}

		public static Task WriteLog(DebugLogTypes filterId, Func<string> logText)
		{
			try
			{
				if (!Enum.IsDefined(typeof(DebugLogTypes), filterId))
					return Task.CompletedTask;
				if (!SGMessageBot.BotConfig.BotInfo.debugLogIds.Contains(filterId))
					return Task.CompletedTask;

				lock (locker)
				{
					var now = DateTime.Now;
					var dateString = now.ToString("yyyyMMdd");
					var path = $"{folderLocation}/{dateString}_DebugLog_{(int)filterId}.log";
					if (!File.Exists(path))
						using (File.Create(path)) { }
					using (var writer = File.AppendText(path))
					{
						var fullDateString = now.ToString("[yyyy-MM-dd hh:mm:ss]");
						writer.WriteLine($"{fullDateString} - {logText()}");
					}
				}
			}
			catch (Exception ex)
			{
				ErrorLog.WriteError(ex);
			}
			return Task.CompletedTask;
		}
	}
}
