using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot.Bot
{
	public static class OtherFunctions
	{
		//This should probably be in a data file somehow instead of being compiled, but oh well, its temporary.
		private static Dictionary<string, DateTime> times = new Dictionary<string, DateTime>();

		public static void loadTimes()
		{
			if (File.Exists("Data/SGTimes.json"))
			{
				var tempTimes = JsonConvert.DeserializeObject<Dictionary<string, long>>(File.ReadAllText("Data/SGTimes.json"));
				times.Clear();
				foreach(var t in tempTimes)
				{
					times.Add(t.Key, new DateTime(t.Value, DateTimeKind.Utc));
				}
			}
		}

		public static string SGRewatchNext()
		{
			var now = DateTime.UtcNow;
			loadTimes();
			var next = times.FirstOrDefault(x => x.Value > now);
			if (!next.Equals(default(KeyValuePair<string, DateTime>)))
			{
				var data = next.Key.Split('|');
				var nextTime = next.Value - now;
				return $"Next showing is Symphogear {data[1]}{(data[1] != "" ? " " : "") }- Episode {data[0]} in: {nextTime.ToString("h\\hm\\m")}.";
			}
			return "Rewatch has ended.";
		}
	}
}
