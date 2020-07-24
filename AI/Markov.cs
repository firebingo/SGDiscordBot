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
using SGMessageBot.Helpers;
using SGMessageBot.DiscordBot;

namespace SGMessageBot.AI
{
	public class Markov
	{
		private List<string> AICorpusKeys = null;
		private static readonly SemaphoreSlim sem = new SemaphoreSlim(1, 1);
		private static readonly int blockSize = 500000;

		private async Task BuildCorpus(bool forceRebuild = false)
		{
			try
			{
				var metaData = await DatebaseCreate.GetMetaData();
				var sql = string.Empty;
				var minDate = DateTime.MinValue;
				if (forceRebuild || !metaData.Success || !metaData.metaData.lastCorpusDate.HasValue)
				{
					sql = "TRUNCATE TABLE messageCorpus";
					await DataLayerShortcut.ExecuteNonQuery(sql);
					sql = "SELECT COUNT(*) FROM messages WHERE isDeleted = false";
				}
				else
				{
					minDate = metaData.metaData.lastCorpusDate.Value;
					sql = "SELECT COUNT(*) FROM messages WHERE isDeleted = false AND mesTime > @date";
				}

				var totalMesCount = await DataLayerShortcut.ExecuteScalarInt(sql, new MySqlParameter("@date", minDate));
				var currentSkip = 0;

				do
				{
					await ProcessMessageBlock(null, currentSkip, blockSize, minDate);
					currentSkip += blockSize;
				} while (currentSkip < totalMesCount);

				if (!string.IsNullOrWhiteSpace(SGMessageBot.BotConfig.BotInfo.DiscordConfig.aiCorpusExtraPath))
				{
					var extraMessages = new List<MessageTextModel>();
					try
					{
						var lines = File.ReadAllLines(SGMessageBot.BotConfig.BotInfo.DiscordConfig.aiCorpusExtraPath);
						lines = lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
						for (var i = 0; i < lines.Length; ++i)
						{
							extraMessages.Add(new MessageTextModel(lines[i]));
						}
					}
					catch
					{
						throw;
					}

					await ProcessMessageBlock(extraMessages, 0, 0, minDate);
				}

				sql = "UPDATE metadata SET lastCorpusDate = @lastdate, updatedDate = @lastdate WHERE mkey = @mkey";
				await DataLayerShortcut.ExecuteNonQuery(sql, new MySqlParameter("@lastdate", DateTime.UtcNow), new MySqlParameter("@mkey", DatebaseCreate.mkey));
			}
			catch (Exception ex)
			{
				ErrorLog.WriteError(ex);
				throw ex;
			}
		}

		public async Task ProcessMessageBlock(List<MessageTextModel> messages, int skip, int limit, DateTime minDate)
		{
			var wordDict = new ConcurrentDictionary<string, ConcurrentBag<string>>();
			if (messages == null)
				messages = await BotCommandProcessor.LoadMessages(minDate, skip, limit);

			//https://stackoverflow.com/questions/5306729/how-do-markov-chain-chatbots-work
			Parallel.ForEach(messages, (message) =>
			{
				try
				{
					var split = message.MesText.Trim().Split(' ');
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
							if (key.Length >= 255)
								continue;
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
				}
				catch (Exception ex)
				{
					ErrorLog.WriteLog($"Exception on message in buildCorpus, Message: \"{message.MesText}\", Exception: {ex.Message}, Stack: {ex.StackTrace}");
					return;
				}
			});

			var sql = string.Empty;
			var insertValuesParam = new List<MySqlParameter>();
			var insertValuesSql = string.Empty;
			var insertI = 0;
			var addTasks = new List<Task>();
			var rows = new List<CorpusRowModel>();
			CorpusRowModel row = null;
			
			foreach (var word in wordDict)
			{
				rows.Clear();
				row = null;
				sql = "SELECT * FROM messageCorpus WHERE keyword = @key";
				await DataLayerShortcut.ExecuteReader(ReadCorpusRow, rows, sql, new MySqlParameter("@key", word.Key));
				row = rows.FirstOrDefault();
				if (row != null)
				{
					foreach (var val in row.Values)
					{
						word.Value.Add(val);
					}
					var values = word.Value.Where(x => x.Trim().Length != 0);
					sql = "UPDATE messageCorpus SET wordValues = @values WHERE keyword = @key";
					addTasks.Add(DataLayerShortcut.ExecuteNonQuery(sql, new MySqlParameter("@key", word.Key), new MySqlParameter("@values", string.Join("||", values))));
				}
				else
				{
					var values = string.Join("||", word.Value.Where(x => x.Trim().Length != 0));
					insertValuesParam.Add(new MySqlParameter($"@keyword{insertI}", word.Key));
					insertValuesParam.Add(new MySqlParameter($"@values{insertI}", values));
					if (insertI == 0)
						insertValuesSql = $"(@keyword{insertI}, @values{insertI})";
					else
						insertValuesSql += $",(@keyword{insertI}, @values{insertI})";
					//$"({MySqlHelper.EscapeString(word.Key)}, {MySqlHelper.EscapeString(values)})"
					if (insertI == 100)
					{
						sql = $"INSERT INTO messageCorpus (keyword, wordValues) VALUES {insertValuesSql}";
						addTasks.Add(DataLayerShortcut.ExecuteNonQuery(sql, insertValuesParam.ToArray()));
						insertValuesParam.Clear();
						insertValuesSql = string.Empty;
						insertI = 0;
					}
					else
					{
						insertI++;
					}
				}
				if (addTasks.Count > 50)
				{
					await Task.WhenAll(addTasks);
					addTasks.Clear();
				}
			}
		}

		public async Task RebuildCorpus(bool forceRebuild = false)
		{
			await sem.WaitAsync();
			try
			{
				await BuildCorpus(forceRebuild);
				await LoadCorpus();
			}
			catch (Exception ex)
			{
				ErrorLog.WriteError(ex);
				sem.Release();
				throw;
			}

			sem.Release();
		}

		private async Task LoadCorpus()
		{
			try
			{
				var keyList = new List<string>();
				var sql = "SELECT keyword FROM messageCorpus";
				await DataLayerShortcut.ExecuteReader(ReadCorpusRowKeys, keyList, sql);

				AICorpusKeys = keyList;
			}
			catch
			{
				throw;
			}
		}

		public async Task<string> GenerateMessage(string startWord)
		{
			try
			{
				await sem.WaitAsync();
				if (AICorpusKeys == null)
					await LoadCorpus();
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
				var startTime = DateTime.UtcNow;
				Random rand = new Random();

				var startWordKeys = new List<string>();
				if (startWord != string.Empty)
					startWordKeys = AICorpusKeys.Where(x => x.StartsWith(startWord)).ToList();
				if (startWord != string.Empty && startWordKeys.Count == 0)
					return $"Could not find a key starting with {startWord}";

				string startValue = string.Empty;
				if (startWordKeys.Count > 0)
					startValue = startWordKeys[rand.Next(startWordKeys.Count)];
				else
					startValue = AICorpusKeys[rand.Next(AICorpusKeys.Count)];
				result += startValue;
				string nextValue = startValue;
				var operate = true;
				var splitLength = 0;
				do
				{
					//If we have taken longer than 15 seconds just kill it because its probably in some weird loop.
					if ((DateTime.UtcNow - startTime).TotalSeconds > 15)
					{
						operate = false;
						break;
					}
					var idx = AICorpusKeys.IndexOf(nextValue);
					if (idx != -1)
					{
						var values = await GetRowForKey(nextValue);
						if (values != null && values.Values.Length > 0)
						{
							var toAdd = values.Values[rand.Next(values.Values.Length)];
							splitLength += 1 + toAdd.Length;
							result += $" {toAdd}";
							var split = result.Split(' ');
							nextValue = $"{split[^2]} {split[^1]}";
							if (splitLength > 1900)
							{
								result += $"|?|";
								splitLength = 0;
							}
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

		private async Task<CorpusRowModel> GetRowForKey(string key)
		{
			var sql = "SELECT * FROM messageCorpus WHERE keyword = @key";
			var values = new List<CorpusRowModel>();
			await DataLayerShortcut.ExecuteReader(ReadCorpusRow, values, sql, new MySqlParameter("@key", key));
			return values.FirstOrDefault();
		}

		private void ReadCorpusRow(IDataReader reader, List<CorpusRowModel> data)
		{
			var row = new CorpusRowModel
			{
				Key = reader.GetString(0),
				Values = reader.GetString(1)?.Split(new string[] { "||" }, StringSplitOptions.None) ?? new string[0]
			};
			data.Add(row);
		}

		private void ReadCorpusRowKeys(IDataReader reader, List<string> data)
		{
			data.Add(reader.GetString(0));
		}
	}

	public class CorpusRowModel
	{
		public string Key;
		public string[] Values;
	}
}
