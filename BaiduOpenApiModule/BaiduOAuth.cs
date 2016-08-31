using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using Newtonsoft.Json;

namespace BaiduOpenApiModule
{
	public class BaiduOAuth
	{
		private string accessToken;

		public void GetAccessToken(string userId, string secretKey) {
			WebClient client = new WebClient ();
			string targetUrl = String.Format(
				"https://openapi.baidu.com/oauth/2.0/token?grant_type=client_credentials&client_id={0}&client_secret={1}",
				WebUtility.UrlEncode(userId),
				WebUtility.UrlEncode(secretKey));
			string responseText = client.DownloadString (targetUrl);
			Dictionary<string,string> responseData = JsonConvert.DeserializeObject<Dictionary<string,string>> (responseText);
			accessToken = responseData ["access_token"];
		}
	}
}

