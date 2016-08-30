using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Net;

using Newtonsoft.Json;

using RequestDispatcher;

namespace ChainLoader
{
	class MainClass
	{
		private static Dictionary<string,string> serviceList;
		private static Dictionary<string,Process> runningList = new Dictionary<string, Process>();
		private static string servicePrefix;

		public static void KillAllServices() {
			foreach (KeyValuePair<string,Process> p in runningList) {
				if (!p.Value.HasExited) {
					p.Value.Kill ();
				}
			}
		}

		public static void OnTerminateServices(HttpListenerContext ctx, StreamReader reader, StreamWriter writer) {
			KillAllServices ();
			writer.WriteLine ("Services terminated.");
			writer.Close ();
			reader.Close ();
			Console.WriteLine ("Termination requested via HTTP API. All services are terminated.");
			Environment.Exit (0);
		}

		public static void Main (string[] args)
		{
			string cfgData = (new StreamReader (new FileStream ("/etc/hydrocloud/chainloader.json", FileMode.Open))).ReadToEnd().Trim();
			serviceList = JsonConvert.DeserializeObject<Dictionary<string,string>> (cfgData);

			servicePrefix = serviceList ["Prefix"];

			foreach (KeyValuePair<string,string> p in serviceList) {
				if (p.Key == "Prefix")
					continue;
				
				string[] cmdParts = p.Value.Split (new char[]{' '}, 2);
				string cmdArgs = "";
				if (cmdParts.Length > 1)
					cmdArgs = cmdParts [1];
				
				runningList[p.Key] = Process.Start (servicePrefix + cmdParts[0], cmdArgs);
				Console.WriteLine (String.Format ("Service {0} started with PID {1}", p.Key, runningList [p.Key].Id));
			}

			HttpRequestDispatcher dp = new HttpRequestDispatcher ();
			dp.RegisterPath ("/terminateServices/", OnTerminateServices);

			HttpListener listener = new HttpListener ();
			listener.Prefixes.Add ("http://127.0.0.1:6080/");
			listener.Start ();

			while (true) {
				HttpListenerContext newCtx = listener.GetContext ();
				StreamReader newReader = new StreamReader (newCtx.Request.InputStream);
				StreamWriter newWriter = new StreamWriter (newCtx.Response.OutputStream);
				dp.DispatchRequest (newCtx, newReader, newWriter);
				newReader.Close ();
				newWriter.Close ();
			}
		}
	}
}
