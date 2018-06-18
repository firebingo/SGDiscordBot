using System;
using System.IO;

namespace SGMessageBot.Helpers
{
	public static class ErrorLog
	{
		private static object lockobj = new object();
		private static string folderLocation = String.Empty;
		static ErrorLog()
		{
			folderLocation = "Data/Logs";
			if (!Directory.Exists(folderLocation))
			{
				Directory.CreateDirectory(folderLocation);
			}
		}

		public static void writeLog(string log)
		{
			try
			{
				lock (lockobj)
				{
					var dateString = DateTime.Now.ToString("yyyyMMdd");
					var path = $"Data/Logs/{dateString}_Errors.log";
					if (!File.Exists(path))
						using (File.Create(path)) { }
					using (var writer = File.AppendText(path))
					{
						var fullDateString = DateTime.Now.ToString("[yyyy-MM-dd hh:MM:ss]");
						writer.WriteLine($"{fullDateString} - {log}");
					}
				}
			}
			catch(Exception e)
			{
				Console.WriteLine($"Shits fucked yo\n(Failed to write to error log: {e.Message})");
			}
		}

		public static void writeError(Exception e)
		{
			try
			{
				lock (lockobj)
				{
					var dateString = DateTime.Now.ToString("yyyyMMdd");
					var path = $"Data/Logs/{dateString}_Errors.log";
					if (!File.Exists(path))
						using (File.Create(path)) { }
					using (var writer = File.AppendText(path))
					{
						var fullDateString = DateTime.Now.ToString("[yyyy-MM-dd hh:MM:ss]");
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
