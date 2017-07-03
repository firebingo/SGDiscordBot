using System;
using System.IO;

namespace SGMessageBot.Helpers
{
	public static class ErrorLog
	{
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
}
