using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace RequestDispatcher {
	public class HttpRequestDispatcher {
		public delegate void HandlerType(HttpListenerContext ctx,StreamReader reader,StreamWriter writer);
		private Dictionary<string,HandlerType> handlerMap = new Dictionary<string,HandlerType>();
		public void RegisterPath(string prefix,HandlerType targetFunc) {
			handlerMap[prefix] = targetFunc;
		}
		public void DispatchRequest(HttpListenerContext ctx,StreamReader reader,StreamWriter writer) {
			foreach(KeyValuePair<string,HandlerType> kv in handlerMap) {
				if(ctx.Request.RawUrl.StartsWith(kv.Key)) {
					try {
						kv.Value(ctx,reader,writer);
					} catch(Exception e) {
						writer.Write (e.Message);
					}
					return;
				}
			}
			ctx.Response.StatusCode = 404;
			writer.WriteLine ("Method not found");
		}
	}
}
