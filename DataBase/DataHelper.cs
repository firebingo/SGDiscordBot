using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGMessageBot.DataBase
{
	public static class DataHelper
	{
		public static void addParams(ref MySqlCommand cmd, MySqlParameter[] paramters)
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
