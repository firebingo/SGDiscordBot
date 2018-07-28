using MySql.Data.MySqlClient;
using SGMessageBot.Config;
using System;
using System.Data;
using System.Data.SQLite;
using SGMessageBot.Helpers;

namespace SGMessageBot.DataBase
{
	public static class DataLayerShortcut
	{
		public static DBConfig DBConfig { get; private set; }
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
				var connection = new MySql.Data.MySqlClient.MySqlConnection(DBConfig.connectionString);
				connection.Open();
				connection.Close();
				connection.Dispose();
			}
			catch (MySqlException e)
			{
				ErrorLog.WriteError(e);
				if (e.InnerException.Message.ToUpperInvariant().Contains("UNKNOWN DATABASE"))
					schemaExists = false;

				result.Success = false;
				result.Message = e.Message;
				return result;
			}
			result.Success = true;
			return result;
		}

		public static BaseResult createDataBase()
		{
			DatebaseCreate dataCreator = new DatebaseCreate();
			var result = new BaseResult();
			result.Success = true;
			if (!schemaExists) {
				result = dataCreator.createDatabase();
			}
			if(!result.Success)
				return result;
			else
			{
				result = dataCreator.buildDatabase();
				if (!result.Success)
					return result;
			}
			schemaExists = true;
			result.Success = true;
			return result;
		}

		public static void ExecuteReader<T>(Action<IDataReader, T> workFunction, T otherdata, string query, params MySqlParameter[] parameters)
		{
			var connection = new MySqlConnection(DBConfig.connectionString);
			connection.Open();
			if (connection.State == ConnectionState.Open)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				MySqlDataReader reader = null;
				if (parameters != null)
					DataHelper.addParams(ref cmd, parameters);

				reader = cmd.ExecuteReader();

				while (reader.Read())
					workFunction(reader, otherdata);

				reader.Close();
				cmd.Dispose();
			}
			connection.Close();
			connection.Dispose();
		}

		public static string ExecuteNonQuery(string query, params MySqlParameter[] parameters)
		{
			var connection = new MySqlConnection(DBConfig.connectionString);
			try
			{
				connection.Open();
				if (connection.State == ConnectionState.Open)
				{
					MySqlCommand cmd = new MySqlCommand(query, connection);
					cmd.CommandType = CommandType.Text;
					if (parameters != null)
						DataHelper.addParams(ref cmd, parameters);

					cmd.ExecuteNonQuery();
					cmd.Dispose();
				}
				connection.Close();
				connection.Dispose();
				return "";
			}
			catch (Exception e)
			{
				ErrorLog.WriteError(e);
				connection.Close();
				connection.Dispose();
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
			conn.Close();
			conn.Dispose();
		}

		public static int? ExecuteScalarInt(string query, params MySqlParameter[] parameters)
		{
			int? result = null;
			var connection = new MySqlConnection(DBConfig.connectionString);
			connection.Open();
			if (connection.State == ConnectionState.Open)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.CommandType = CommandType.Text;
				if (parameters != null)
					DataHelper.addParams(ref cmd, parameters);

				object scalar = cmd.ExecuteScalar();
				try
				{
					result = Convert.ToInt32(scalar);
				}
				catch (Exception e)
				{
					ErrorLog.WriteError(e);
					cmd.Dispose();
					connection.Close();
					connection.Dispose();
					return result;
				}
				cmd.Dispose();
			}
			connection.Close();
			connection.Dispose();
			return result;
		}

		public static uint? ExecuteScalarUInt(string query, params MySqlParameter[] parameters)
		{
			uint? result = null;
			var connection = new MySqlConnection(DBConfig.connectionString);
			connection.Open();
			if (connection.State == ConnectionState.Open)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.CommandType = CommandType.Text;
				if (parameters != null)
					DataHelper.addParams(ref cmd, parameters);

				object scalar = cmd.ExecuteScalar();
				try
				{
					result = Convert.ToUInt32(scalar);
				}
				catch (Exception e)
				{
					ErrorLog.WriteError(e);
					cmd.Dispose();
					connection.Close();
					connection.Dispose();
					return result;
				}
				cmd.Dispose();
			}
			connection.Close();
			connection.Dispose();
			return result;
		}

		public static BaseResult openLiteConnection(ulong serverId)
		{
			var result = new BaseResult();
			try
			{
				//if(!DBConfig.config.nadekoDbPath.ContainsKey(serverId))
				//{
				//	result.success = false;
				//	result.message = "No db path specified for this server.";
				//	return result;
				//}
				//if (DBConfig.config.nadekoDbPath == null || DBConfig.config.nadekoDbPath[serverId] == string.Empty)
				//{
				//	result.success = false;
				//	result.message = "No path specified for NadekoBot Database.";
				//}
				//if (!File.Exists(DBConfig.config.nadekoDbPath[serverId]))
				//{
				//	result.success = false;
				//	result.message = "No NadekoBot Database at given path.";
				//}
				//liteDBConn = new SQLiteConnection($"Data Source={DBConfig.config.nadekoDbPath[serverId]};Version=3;");
				//liteDBConn.Open();
			}
			catch (SQLiteException e)
			{
				ErrorLog.WriteError(e);
				result.Success = false;
				result.Message = e.Message;
			}
			hasNadeko = true;
			result.Success = true;
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
				ErrorLog.WriteError(e);
				result.Success = false;
				result.Message = e.Message;
			}
			result.Success = true;
			return result;
		}

		public static int? executeScalarLite(ulong serverId, string query, params SQLiteParameter[] parameters)
		{
			int? result = null;
			var isOpen = openLiteConnection(serverId);
			if(!isOpen.Success)
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
				catch (Exception e)
				{
					ErrorLog.WriteError(e);
					command.Dispose();
					closeLiteConnection();
					return result;
				}
				command.Dispose();
			}
			catch(SQLiteException e)
			{
				ErrorLog.WriteError(e);
				closeLiteConnection();
				return result = null;
			}
			return result;
		}
	}
}
