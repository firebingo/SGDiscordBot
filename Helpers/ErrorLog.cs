using System;
using System.IO;

namespace SGMessageBot.Helpers
{
	public static class ErrorLog
	{
		private static readonly object locker = new object();
		private static readonly string folderLocation = string.Empty;
		static ErrorLog()
		{
			folderLocation = "Data/Logs";
			if (!Directory.Exists(folderLocation))
				Directory.CreateDirectory(folderLocation);
		}

		public static void WriteLog(string log)
		{
			try
			{
				lock (locker)
				{
					var now = DateTime.Now;
					var dateString = now.ToString("yyyyMMdd");
					var path = $"{folderLocation}/{dateString}_Errors.log";
					if (!File.Exists(path))
						using (File.Create(path)) { }
					using (var writer = File.AppendText(path))
					{
						var fullDateString = now.ToString("[yyyy-MM-dd hh:mm:ss]");
						writer.WriteLine($"{fullDateString} - {log}");
					}
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine($"Shits fucked yo\n(Failed to write to error log: {ex.Message})");
			}
		}

		public static void WriteError(Exception e)
		{
			try
			{
				lock (locker)
				{
					var now = DateTime.Now;
					var dateString = now.ToString("yyyyMMdd");
					var path = $"Data/Logs/{dateString}_Errors.log";
					if (!File.Exists(path))
						using (File.Create(path)) { }
					using (var writer = File.AppendText(path))
					{
						var fullDateString = now.ToString("[yyyy-MM-dd hh:mm:ss]");
						writer.WriteLine($"{fullDateString} - Message: {e.Message}, Stack Trace: {e.StackTrace}");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Shits fucked yo\n(Failed to write to error log: {ex.Message})");
			}
		}
	}
}
