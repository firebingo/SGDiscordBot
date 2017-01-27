using MySql.Data.MySqlClient;
using SGMessageBot.Config;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace SGMessageBot.DataBase
{
	public static class DataLayerShortcut
	{
		public static DBConfig DBConfig { get; private set; }
		private static MySqlConnection DBConn;
		public static bool schemaExists { get; private set; } = true;
		private static SQLiteConnection liteDBConn;
		public static bool hasNadeko { get; private set; } = false;

		public static BaseResult loadConfig()
		{
			DBConfig = new DBConfig();
			var result = DBConfig.loadDBConfig();
			return result;
		}

		public static BaseResult testConnection()
		{
			var result = new BaseResult();
			try
			{
				DBConn = new MySql.Data.MySqlClient.MySqlConnection(DBConfig.connectionString);
				DBConn.Open();
			}
			catch (MySqlException e)
			{
				if (e.InnerException.Message.ToUpperInvariant().Contains("UNKNOWN DATABASE"))
					schemaExists = false;

				result.success = false;
				result.message = e.Message;
				return result;
			}
			result.success = true;
			return result;
		}

		public static BaseResult createDataBase()
		{
			DatebaseCreate dataCreator = new DatebaseCreate();
			var result = new BaseResult();
			result.success = true;
			if (!schemaExists) {
				result = dataCreator.createDatabase();
			}
			if(!result.success)
				return result;
			else
			{
				result = dataCreator.buildDatabase();
				if (!result.success)
					return result;
			}
			schemaExists = true;
			result.success = true;
			return result;
		}

		public static void ExecuteReader<T>(Action<IDataReader, T> workFunction, T otherdata, string query, params MySqlParameter[] parameters)
		{
			if (DBConn != null || DBConn.State != ConnectionState.Open)
			{
				DBConn = new MySqlConnection(DBConfig.connectionString);
				DBConn.Open();
			}
			MySqlCommand cmd = new MySqlCommand(query, DBConn);		
			MySqlDataReader reader = null;
			if (parameters != null)
				DataHelper.addParams(ref cmd, parameters);

			reader = cmd.ExecuteReader();

			while (reader.Read())
				workFunction(reader, otherdata);

			reader.Close();
			cmd.Dispose();
		}

		public static string ExecuteNonQuery(string query, params MySqlParameter[] parameters)
		{
			try
			{
				if (DBConn != null || DBConn.State != ConnectionState.Open)
				{
					DBConn = new MySqlConnection(DBConfig.connectionString);
					DBConn.Open();
				}
				MySqlCommand cmd = new MySqlCommand(query, DBConn);
				cmd.CommandType = CommandType.Text;
				if (parameters != null)
					DataHelper.addParams(ref cmd, parameters);

				cmd.ExecuteNonQuery();
				cmd.Dispose();
				return "";
			}
			catch (Exception e)
			{
				return $"Exception: {e.Message}, Query: {query}";
			}
		}

		public static void ExecuteSpecialNonQuery(string query, string connection, params MySqlParameter[] parameters)
		{
			var conn = new MySqlConnection(connection);
			conn.Open();
			MySqlCommand cmd = new MySqlCommand(query, conn);
			cmd.CommandType = CommandType.Text;
			if (parameters != null)
				DataHelper.addParams(ref cmd, parameters);

			cmd.ExecuteNonQuery();
			cmd.Dispose();
		}

		public static int? ExecuteScalar(string query, params MySqlParameter[] parameters)
		{
			int? result = null;
			if (DBConn != null || DBConn.State != ConnectionState.Open)
			{
				DBConn = new MySqlConnection(DBConfig.connectionString);
				DBConn.Open();
			}
			MySqlCommand cmd = new MySqlCommand(query, DBConn);
			cmd.CommandType = CommandType.Text;
			if (parameters != null)
				DataHelper.addParams(ref cmd, parameters);

			object scalar = cmd.ExecuteScalar();
			try
			{
				result = Convert.ToInt32(scalar);
			}
			catch
			{
				cmd.Dispose();
				return result;
			}
			cmd.Dispose();
			return result;
		}

		public static void closeConnection()
		{
			if (DBConn != null || DBConn.State != ConnectionState.Closed)
			{
				DBConn.Close();
			}
		}

		public static BaseResult openLiteConnection(ulong serverId)
		{
			var result = new BaseResult();
			try
			{
				if(!DBConfig.config.nadekoDbPath.ContainsKey(serverId))
				{
					result.success = false;
					result.message = "No db path specified for this server.";
					return result;
				}
				if (DBConfig.config.nadekoDbPath == null || DBConfig.config.nadekoDbPath[serverId] == string.Empty)
				{
					result.success = false;
					result.message = "No path specified for NadekoBot Database.";
				}
				if (!File.Exists(DBConfig.config.nadekoDbPath[serverId]))
				{
					result.success = false;
					result.message = "No NadekoBot Database at given path.";
				}
				liteDBConn = new SQLiteConnection($"Data Source={DBConfig.config.nadekoDbPath[serverId]};Version=3;");
				liteDBConn.Open();
			}
			catch (SQLiteException e)
			{
				result.success = false;
				result.message = e.Message;
			}
			hasNadeko = true;
			result.success = true;
			return result;
		}

		public static BaseResult closeLiteConnection()
		{
			var result = new BaseResult();
			try
			{
				if (liteDBConn != null)
				{
					liteDBConn.Close();
					liteDBConn = null;
				}
			}
			catch (SQLiteException e)
			{
				result.success = false;
				result.message = e.Message;
			}
			result.success = true;
			return result;
		}

		public static int? executeScalarLite(ulong serverId, string query, params SQLiteParameter[] parameters)
		{
			int? result = null;
			var isOpen = openLiteConnection(serverId);
			if(!isOpen.success)
			{
				return result;
			}
			try
			{
				SQLiteCommand command = new SQLiteCommand(query, liteDBConn);
				if (parameters != null)
					DataHelper.addLiteParams(ref command, parameters);
				command.CommandType = CommandType.Text;
				object scalar = command.ExecuteScalar(CommandBehavior.CloseConnection);
				try
				{
					result = Convert.ToInt32(scalar);
				}
				catch
				{
					command.Dispose();
					closeLiteConnection();
					return result;
				}
				command.Dispose();
			}
			catch(SQLiteException e)
			{
				closeLiteConnection();
				return result = null;
			}
			return result;
		}
	}
}
