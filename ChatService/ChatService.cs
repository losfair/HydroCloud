using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading;

using Newtonsoft.Json;
using MySql.Data.MySqlClient;

using RequestDispatcher;
using DataProviderModule;

namespace ChatService {
	public class ChatServiceEntrance {
		private static HttpRequestDispatcher dp = new HttpRequestDispatcher();

		private static void testHandler(HttpListenerContext ctx,StreamReader reader,StreamWriter writer) {
			string outputText = String.Format ("Hello world!\nRemote endpoint: {0}\nUser agent: {1}\n",
				ctx.Request.RemoteEndPoint.ToString(),
				ctx.Request.UserAgent);
			ctx.Response.Headers.Set ("Content-Type", "text/plain;charset=utf-8");
			writer.WriteLine(outputText);
		}

		private static void registerHandlers() {
			dp.RegisterPath ("/test/",testHandler);
			dp.RegisterPath ("/turingBot/",ServiceHandlers.OnTuringBotRequest);
			dp.RegisterPath ("/getGroupIntro/", ServiceHandlers.OnGetGroupIntro);
			dp.RegisterPath ("/getAllGroupNames/", ServiceHandlers.OnGetAllGroupNames);
		}

		private static void clientHandler(object ctx_obj) {
			HttpListenerContext ctx = ctx_obj as HttpListenerContext;
			StreamReader reader = new StreamReader(ctx.Request.InputStream);
			StreamWriter writer = new StreamWriter(ctx.Response.OutputStream);

			ctx.Response.Headers.Set("Server","HydroCloud Web Service v2.0");
			ctx.Response.Headers.Set("Content-Type","text/html;charset=utf-8");

			dp.DispatchRequest(ctx,reader,writer);

			try {
				reader.Close();
				writer.Close();
			} catch(IOException) {
				Console.WriteLine("IOException while writing data");
			}

			Console.WriteLine("Client done.");
		}

		public static void Main(string[] args) {
			string cfgPath = args [0];
			string cfgData = (new StreamReader((new FileStream (cfgPath, FileMode.Open)))).ReadToEnd().Trim();
			Dictionary<string,string> cfg = JsonConvert.DeserializeObject<Dictionary<string,string>> (cfgData);

			try {
				Console.WriteLine("Loading config");
				Console.Write("turingbot_key... ");
				Console.Out.Flush();
				ServiceHandlers.turingBotApi.SetSecretKey(cfg["turingbot_key"]);
				Console.WriteLine("OK");
				Console.Write("mysql_connection... ");
				Console.Out.Flush();
				ServiceHandlers.db = new DataProvider(cfg["mysql_connection"]);
				Console.WriteLine("OK");
			} catch(KeyNotFoundException e) {
				Console.WriteLine (e.Message);
				Environment.Exit (-1);
			}

			registerHandlers();
			ServiceHandlers.PrepareStatements ();

			HttpListener listener = new HttpListener();
			listener.Prefixes.Add("http://127.0.0.1:6083/");
			listener.Start();

			while(true) {
				Console.WriteLine("Waiting for connection");
				HttpListenerContext ctx = listener.GetContext();
				Console.WriteLine("New client.");
				ThreadPool.QueueUserWorkItem(clientHandler,ctx);
			}
		}
	}
}
