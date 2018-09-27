using MySql.Data.MySqlClient;
using SGMessageBot.Config;
using System;
using System.Data;
using SGMessageBot.Helpers;
using System.Threading.Tasks;
using System.Data.Common;

namespace SGMessageBot.DataBase
{
	public static class DataLayerShortcut
	{
		public static DBConfig DBConfig { get; private set; }
		public static bool SchemaExists { get; private set; } = true;
		public static bool HasNadeko { get; private set; } = false;

		public static BaseResult LoadConfig()
		{
			DBConfig = new DBConfig();
			var result = DBConfig.LoadDBConfig();
			return result;
		}

		public static BaseResult TestConnection()
		{
			var result = new BaseResult();
			try
			{
				var connection = new MySqlConnection(DBConfig.ConnectionString);
				connection.Open();
				connection.Close();
				connection.Dispose();
			}
			catch (MySqlException e)
			{
				ErrorLog.WriteError(e);
				if (e.InnerException != null && e.InnerException.Message.ToUpperInvariant().Contains("UNKNOWN DATABASE"))
					SchemaExists = false;

				result.Success = false;
				result.Message = e.Message;
				return result;
			}
			result.Success = true;
			return result;
		}

		public static async Task<BaseResult> CreateDataBase()
		{
			DatebaseCreate dataCreator = new DatebaseCreate();
			var result = new BaseResult
			{
				Success = true
			};
			if (!SchemaExists) {
				result = await dataCreator.CreateDatabase();
			}
			if(!result.Success)
				return result;
			else
			{
				result = await dataCreator.BuildDatabase();
				if (!result.Success)
					return result;
			}
			SchemaExists = true;
			result.Success = true;
			return result;
		}

		public static async Task ExecuteReader<T>(Action<IDataReader, T> workFunction, T otherdata, string query, params MySqlParameter[] parameters)
		{
			var connection = new MySqlConnection(DBConfig.ConnectionString);
			connection.Open();
			if (connection.State == ConnectionState.Open)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				DbDataReader reader = null;
				if (parameters != null)
					DataHelper.AddParams(ref cmd, parameters);

				reader = await cmd.ExecuteReaderAsync();

				while (reader.Read())
					workFunction(reader, otherdata);

				reader.Close();
				cmd.Dispose();
			}
			connection.Close();
			connection.Dispose();
		}

		public static async Task<string> ExecuteNonQuery(string query, params MySqlParameter[] parameters)
		{
			var connection = new MySqlConnection(DBConfig.ConnectionString);
			try
			{
				connection.Open();
				if (connection.State == ConnectionState.Open)
				{
					MySqlCommand cmd = new MySqlCommand(query, connection)
					{
						CommandType = CommandType.Text
					};
					if (parameters != null)
						DataHelper.AddParams(ref cmd, parameters);

					await cmd.ExecuteNonQueryAsync();
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

		public static async Task ExecuteSpecialNonQuery(string query, string connection, params MySqlParameter[] parameters)
		{
			var conn = new MySqlConnection(connection);
			conn.Open();
			MySqlCommand cmd = new MySqlCommand(query, conn)
			{
				CommandType = CommandType.Text
			};
			if (parameters != null)
				DataHelper.AddParams(ref cmd, parameters);

			await cmd.ExecuteNonQueryAsync();
			cmd.Dispose();
			conn.Close();
			conn.Dispose();
		}

		public static async Task<int?> ExecuteScalarInt(string query, params MySqlParameter[] parameters)
		{
			int? result = null;
			var connection = new MySqlConnection(DBConfig.ConnectionString);
			connection.Open();
			if (connection.State == ConnectionState.Open)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection)
				{
					CommandType = CommandType.Text
				};
				if (parameters != null)
					DataHelper.AddParams(ref cmd, parameters);

				object scalar = await cmd.ExecuteScalarAsync();
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

		public static async Task<uint?> ExecuteScalarUInt(string query, params MySqlParameter[] parameters)
		{
			uint? result = null;
			var connection = new MySqlConnection(DBConfig.ConnectionString);
			connection.Open();
			if (connection.State == ConnectionState.Open)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection)
				{
					CommandType = CommandType.Text
				};
				if (parameters != null)
					DataHelper.AddParams(ref cmd, parameters);

				object scalar = await cmd.ExecuteScalarAsync();
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
	}
}
