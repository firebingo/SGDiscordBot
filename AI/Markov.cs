using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace SGMessageBot.AI
{
	public class Markov
	{
		private const string dataDirectory = @"Data\AICorpus.json";
		private static Dictionary<string, string[]> AICorpus;

		private async Task buildCorpus()
		{
			try
			{
				var messages = await loadMessages();
				var wordDict = new ConcurrentDictionary<string, ConcurrentBag<string>>();
				Parallel.ForEach(messages, (message) =>
				{
					var split = message.mesText.Trim().Split(' ');
					var length = split.Length;
					if (length < 3)
						return;
					var i = 0;
					var operate = true;
					while (operate)
					{
						var key = string.Empty;
						var value = string.Empty;
						if (i + 2 < length)
						{
							key = $"{split[i]} {split[++i]}";
							value = split[i + 1];
							if (wordDict.ContainsKey(key))
							{
								wordDict[key].Add(value);
							}
							else
							{
								wordDict.AddOrUpdate(key, new ConcurrentBag<string>() { value }, (xKey, xExistingVal) =>
								{
									xExistingVal.Add(value);
									return xExistingVal;
								});
							}
						}
						else
						{
							operate = false;
							break;
						}
					}
				});
				AICorpus = wordDict.ToDictionary(x => x.Key, x => x.Value.ToArray());
				using (var file = new StreamWriter(File.Open(dataDirectory, FileMode.Create)))
				{
					foreach (var word in AICorpus)
					{
						file.WriteLine(JsonConvert.SerializeObject(word));
					}
					file.Close();
				}
			}
			catch
			{
				throw;
			}
		}

		public async Task rebuildCorpus()
		{
			if(File.Exists(dataDirectory))
			{
				File.Delete(dataDirectory);
			}
			await loadCorpus();
		}

		public async Task loadCorpus()
		{
			try
			{
				var wordDict = new Dictionary<string, string[]>();
				if (!File.Exists(dataDirectory))
				{
					await buildCorpus();
					return;
				}
				else
				{
					using (var file = new StreamReader(File.OpenRead(dataDirectory)))
					{
						var line = string.Empty;
						while ((line = file.ReadLine()) != null)
						{
							try
							{
								KeyValuePair<string, string[]> item = JsonConvert.DeserializeObject<KeyValuePair<string, string[]>>(line);
								wordDict.Add(item.Key, item.Value);
							}
							catch
							{
								continue;
							}
						}
						file.Close();
					}
					AICorpus = wordDict;
				}
			}
			catch
			{
				throw;
			}
		}

		public async Task<string> generateMessage()
		{
			try
			{
				var result = string.Empty;
				if (AICorpus == null)
					await loadCorpus();

				Random rand = new Random();
				List<string> keys = Enumerable.ToList(AICorpus.Keys);
				var startValue = keys[rand.Next(keys.Count)];
				result += startValue;
				string nextValue = startValue;
				var operate = true;
				do
				{
					if (AICorpus.ContainsKey(nextValue))
					{
						if (AICorpus[nextValue].Length > 0)
						{
							var toAdd = AICorpus[nextValue][(rand.Next(AICorpus[nextValue].Length))];
							result += $" {toAdd}";
							var split = result.Split(' ');
							nextValue = $"{split[split.Length - 2]} {split[split.Length - 1]}";
						}
						else
						{
							operate = false;
							break;
						}
					}
					else
					{
						operate = false;
						break;
					}
					

				} while (operate);
				return result;
			}
			catch
			{
				throw;
			}
		}

		private async Task<List<MessageModel>> loadMessages()
		{
			var result = new List<MessageModel>();

			var query = "SELECT txt FROM (SELECT COALESCE(mesText, editedMesText) AS txt FROM messages WHERE NOT isDeleted) x WHERE txt != ''";
			DataLayerShortcut.ExecuteReader<List<MessageModel>>(readMessages, result, query);

			return result;
		}

		private void readMessages(IDataReader reader, List<MessageModel> data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null)
			{
				var message = new MessageModel(reader.GetString(0));
				data.Add(message);
			}
		}
	}

	[Serializable]
	public struct MessageModel
	{
		private readonly string _mesText;
		public string mesText { get { return _mesText; } }

		public MessageModel(string t)
		{
			_mesText = t;
		}
	}
}
