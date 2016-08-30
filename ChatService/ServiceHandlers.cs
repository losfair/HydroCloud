using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;

using MySql.Data.MySqlClient;

using DataProviderModule;

namespace ChatService
{
	public class ServiceHandlers
	{
		public static TuringBot turingBotApi = new TuringBot();
		public static DataProvider db;

		private const string msgPrefix = "HCBot";

		private static Dictionary<string,MySqlCommand> PreparedCommands = new Dictionary<string, MySqlCommand>();

		private static string RequestPreprocess (HttpListenerContext ctx,StreamReader reader,StreamWriter writer) {
			string reqStr = reader.ReadToEnd().Trim();
			string requestType = ctx.Request.QueryString["type"];
			bool isPrivateChat = false;

			if (requestType == "private")
				isPrivateChat = true;

			if (!isPrivateChat && !reqStr.StartsWith (msgPrefix))
				throw new InvalidOperationException ("Bad request");

			if (reqStr.Length < 1)
				reqStr = ctx.Request.QueryString["msg"];

			if (reqStr.Length < 1)
				throw new InvalidOperationException ("Bad request");

			string reqMsg;
			if(isPrivateChat) reqMsg = reqStr.Trim();
			else reqMsg=reqStr.Substring(msgPrefix.Length).Trim();

			if (reqMsg.Length < 1) {
				throw new InvalidOperationException ("Bad request");
			}

			return reqMsg;
		}

		public static void OnGetGroupIntro(HttpListenerContext ctx,StreamReader reader,StreamWriter writer) {
			string reqMsg = "";
			try {
				reqMsg = RequestPreprocess (ctx, reader, writer);
			} catch(InvalidOperationException e) {
				writer.Write (e.Message);
				return;
			}

			string targetGroupName = "%" + reqMsg + "%";

			try {
				PreparedCommands ["GetGroupIntroByName"].Parameters["@name"].Value = targetGroupName;
			} catch(ArgumentException) {
				PreparedCommands ["GetGroupIntroByName"].Parameters.AddWithValue ("@name", targetGroupName);
			}

			MySqlDataReader dataReader = PreparedCommands ["GetGroupIntroByName"].ExecuteReader ();
			if (!dataReader.Read ()) {
				writer.Write ("未找到。");
				goto finishing;
			}
			string targetContent;
			try {
				targetContent = (string)dataReader ["content"];
			} catch(Exception) {
				goto finishing;
			}
			writer.Write (targetContent);

			finishing:
			dataReader.Close ();
		}

		public static void OnGetAllGroupNames(HttpListenerContext ctx,StreamReader reader,StreamWriter writer) {
			MySqlDataReader dataReader = PreparedCommands ["GetAllGroupNames"].ExecuteReader ();

			string resultStr = "";

			for (int i = 1; dataReader.Read (); i++) {
				resultStr += String.Format ("[{0}] {1}\n", i, dataReader ["name"]);
			}

			dataReader.Close ();

			if (resultStr == "") {
				writer.Write ("未找到。");
				return;
			}

			writer.Write (resultStr.Trim ());
		}

		public static void OnTuringBotRequest(HttpListenerContext ctx,StreamReader reader,StreamWriter writer) {
			string userId = ctx.Request.QueryString["userId"];

			string reqMsg = "";

			try {
				reqMsg = RequestPreprocess(ctx,reader,writer);
			} catch(InvalidOperationException e) {
				writer.Write (e.Message);
				return;
			}

			Console.WriteLine("[onTuringBotRequest] New message: "+reqMsg);
			writer.Write(turingBotApi.Request(reqMsg,userId));
		}

		public static void PrepareStatements() {
			PreparedCommands ["GetGroupIntroByName"] = db.PrepareCommand("SELECT content FROM intro_to_ntzx_groups_2016 WHERE name LIKE @name AND is_showed=1 ORDER BY id DESC");
			PreparedCommands ["GetAllGroupNames"] = db.PrepareCommand("SELECT name FROM intro_to_ntzx_groups_2016 WHERE is_showed=1 ORDER BY id ASC");
		}
	}
}

