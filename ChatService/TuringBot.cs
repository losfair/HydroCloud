using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using Newtonsoft.Json;

namespace ChatService {
	public class TuringBot {
		private string SecretKey = "";

		public void SetSecretKey(string key) {
			SecretKey = key;
		}

		public string Request(string reqStr, string userId) {
			try {
				string reqArgs = String.Format("key={0}&info={1}&userid={2}",SecretKey,WebUtility.UrlEncode(reqStr),WebUtility.UrlEncode(userId));
				WebClient client = new WebClient();
				client.Headers.Add("Content-Type","application/x-www-form-urlencoded");
				string responseData = Encoding.UTF8.GetString(client.UploadData("http://www.tuling123.com/openapi/api","POST",Encoding.UTF8.GetBytes(reqArgs)));
				Dictionary<string,string> responseObject = JsonConvert.DeserializeObject<Dictionary<string,string>>(responseData);
				return responseObject["text"];
			} catch(Exception) {
				return "";
			}
		}
	}
}
