using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SGMessageBot.Bot
{
	/// <summary>
	/// This class is used for the process to run through the bots server list and add missing data to the database.
	/// </summary>
	public static class BotExamineServers
	{
		/// <summary>
		/// Updates a specific server in the database
		/// </summary>
		public static void updateDatabaseServer(Discord.Server server)
		{
			var queryString = @"INSERT INTO servers (serverID, ownerID, serverName, userCount, channelCount, roleCount, regionID, createdDate)
			VALUES (@serverID, @ownerID, @serverName, @userCount, @channelCount, @roleCount, @regionID, @createdDate))";
		}
	}
}
