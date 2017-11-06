using MySql.Data.MySqlClient;
using SGMessageBot.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading;

namespace SGMessageBot.AI
{
	public class Markov
	{
		private const string dataDirectory = @"Data\AICorpus.json";
		private Dictionary<string, string[]> AICorpus;
		private static SemaphoreSlim sem = new SemaphoreSlim(1, 1);
		private static object corpusLock = new object();

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
				lock (corpusLock)
				{
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
			}
			catch
			{
				throw;
			}
		}

		public async Task rebuildCorpus()
		{
			await sem.WaitAsync();
			try
			{
				if (File.Exists(dataDirectory))
					File.Delete(dataDirectory);
			}
			catch (Exception e)
			{
				sem.Release();
				throw;
			}
			
			await loadCorpus();
			sem.Release();
		}

		private async Task loadCorpus()
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
					lock (corpusLock)
					{
						AICorpus = wordDict;
					}
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
				await sem.WaitAsync();
				if (AICorpus == null)
				{
					await loadCorpus();
				}
			}
			catch
			{
				throw;
			}
			finally
			{
				sem.Release();
			}
			try
			{
				var result = string.Empty;
				lock (corpusLock)
				{
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
				}
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

			var query = "SELECT txt FROM (SELECT COALESCE(mesText, editedMesText) AS txt FROM messages WHERE NOT isDeleted AND userId != @botId) x WHERE txt != '' AND txt NOT LIKE '%@botId%'";
			DataLayerShortcut.ExecuteReader<List<MessageModel>>(readMessages, result, query, new MySqlParameter("@botId", SGMessageBot.botConfig.botInfo.botId));

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

		[Serializable]
		private struct MessageModel
		{
			private readonly string _mesText;
			public string mesText { get { return _mesText; } }

			public MessageModel(string t)
			{
				_mesText = t;
			}
		}
	}
}
