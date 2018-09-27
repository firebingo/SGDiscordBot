using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace SGMessageBot.DataBase
{
	public static class DataHelper
	{
		public static void AddParams(ref MySqlCommand cmd, MySqlParameter[] paramters)
		{
			foreach (var p in paramters)
			{
				if (p != null)
				{
					// Check for derived output value with no value assigned
					if ((p.Direction == ParameterDirection.InputOutput ||
						 p.Direction == ParameterDirection.Input) &&
						(p.Value == null))
					{
						p.Value = DBNull.Value;
					}
					cmd.Parameters.Add(p);
				}
			}
		}
	}
}
