using System;
using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace DataProviderModule
{
	public class DataProvider
	{
		private MySqlConnection dbConn = new MySqlConnection();

		public MySqlCommand PrepareCommand(string text) {
			MySqlCommand cmd = new MySqlCommand ();
			cmd.Connection = dbConn;
			cmd.CommandText = text;
			cmd.Prepare ();
			return cmd;
		}
		public DataProvider (string connString)
		{
			dbConn.ConnectionString = connString;
			dbConn.Open ();
		}
	}
}

