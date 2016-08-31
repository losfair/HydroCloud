using SmartQQLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QQBotService
{
	public class QQBot
    {
        public static SmartQQClient qc;
		public static long BotAdminUid = 0;
		public static string AdminAuthStr = "";
		public static Dictionary<long,long> groupIdMapping = new Dictionary<long, long>();

		public delegate void AdminActionType(string msgContent, long msgFromChat, long msgSendUin);
		public delegate void NormalActionType(string msgContent, long msgFromChat, long msgSendUin);

		public static Dictionary<string,AdminActionType> adminActionTable = new Dictionary<string, AdminActionType>();
		public static Dictionary<string,NormalActionType> normalActionTable = new Dictionary<string, NormalActionType>();

		private static void registerAdminActions() {
			adminActionTable ["/kick "] = ChatActions.KickGroupMember;
			adminActionTable ["/gid "] = ChatActions.AddGroupMapping;
			adminActionTable ["/shutup "] = ChatActions.ShutupGroupMember;
		}

		private static void registerNormalActions() {
//			normalActionTable ["/card "] = ChatActions.SetMyCard;
		}

		private static void initGroupList() {
			Console.WriteLine ("Loading group list");
			string groupListJson = qc.GetGroupList ();
			string groupListExtJson = qc.GetGroupListExt ();

//			Console.WriteLine ("Processing");

//			Console.WriteLine (groupListJson);
			JObject groupList = JsonConvert.DeserializeObject<JObject> (groupListJson);
			JObject groupListExt = JsonConvert.DeserializeObject<JObject> (groupListExtJson);

			JArray groupNameList = (JArray)((JObject)groupList.GetValue ("result")).GetValue ("gnamelist");
			JArray groupNameListExt = (JArray)groupListExt.GetValue ("join");

			Dictionary<string, long> nameMapping = new Dictionary<string, long>();

//			Console.WriteLine ("Mapping stage 1");

			foreach (JObject item in groupNameList) {
				string groupName = (string)item.GetValue ("name");
				long groupId = (long)item.GetValue ("gid");
				nameMapping [groupName] = groupId;
			}

//			Console.WriteLine ("Mapping stage 2");

			foreach (JObject item in groupNameListExt) {
//				Console.WriteLine ("In loop");
				long prop_gc = (long)item.GetValue ("gc");
				string prop_gn = WebUtility.HtmlDecode((string)item.GetValue ("gn"));
//				Console.WriteLine ("{0} {1}", prop_gc, prop_gn);
				try {
					groupIdMapping [nameMapping [prop_gn]] = prop_gc;
					Console.WriteLine("{0} -> {1}",nameMapping[prop_gn],prop_gc);
				} catch(Exception e) {
					Console.WriteLine (e.Message);
					continue;
				}
//				Console.WriteLine ("Loop done");
			}

			Console.WriteLine ("Group list loaded.");
		}

		private static void messageReceiver(object unused) {
			while (true) {
				Dictionary<string,object> msgData;
				try {
					msgData = JsonConvert.DeserializeObject<Dictionary<string,object>>(qc.Poll2 ());
				} catch(ArgumentException) {
					Thread.Sleep (1000);
					continue;
				}

				string msgContent = "";
				long msgFromChat = 0;
				long msgGroupCode = 0;
				long msgSendUin = 0;
				bool isPrivateChat = false;

				try {
					JObject result = (JObject)((JArray)msgData["result"])[0];
					msgContent = ((string)((JArray)((JObject)result.GetValue("value")).GetValue("content"))[1]).Trim();
					msgFromChat = (long)((JObject)result.GetValue("value")).GetValue("from_uin");
					try {
						msgSendUin = (long)((JObject)result.GetValue("value")).GetValue("send_uin");
					} catch(ArgumentNullException) {
						msgSendUin = msgFromChat;
					}
					try {
						msgGroupCode = (long)((JObject)result.GetValue("value")).GetValue("group_code");
					} catch(ArgumentNullException) {
						isPrivateChat = true;
					}
				} catch(InvalidCastException) {
					Console.WriteLine ("Unable to parse response: InvalidCastException");
					Thread.Sleep (1000);
					continue;
				} catch(Exception e) {
					Console.WriteLine ("Unexpected exception: {0}", e.Message);
				}

				if (msgContent == AdminAuthStr && BotAdminUid == 0 && isPrivateChat) {
					BotAdminUid = msgFromChat;
					Console.WriteLine ("Bot admin uid set to {0}.", BotAdminUid);
					Thread.Sleep (1000);
					continue;
				}

				bool isAdminCommand = false;

				if (!isPrivateChat && msgSendUin == BotAdminUid) {
					foreach (KeyValuePair<string,AdminActionType> itr in adminActionTable) {
						if (itr.Key != "" && msgContent.StartsWith (itr.Key)) {
							Console.WriteLine ("Executing administrative command: {0}", itr.Key);
							isAdminCommand = true;
							try {
								itr.Value (msgContent.Substring(itr.Key.Length).Trim(), msgFromChat, msgSendUin);
							} catch(Exception e) {
								Console.WriteLine ("Exception while executing administrative command: {0}", e.Message);
							}
							break;
						}
					}
				}

				if (!isPrivateChat && !isAdminCommand) {
				}

				Console.WriteLine (msgContent.Trim());

				Thread.Sleep (1000);
			}
		}

		public static void Main(string[] args)
        {
			registerAdminActions ();

            qc = new SmartQQClient();

            qc.BeginReLogin = () => {
                RunInMainthread(() => {
                    //QrCodePicture.Image = UserLogo;
					Console.WriteLine("Automatically logging in");
                });
            };
            qc.ReLoginFail = () => {
                RunInMainthread(() => {
                    //QrCodePicture.Image = UserLogo;
					Console.WriteLine("Unable to log in automatically, fetching QR code");
                });
            };

            qc.OnGetQRCodeImage = (image) => {
                RunInMainthread(() => {
					image.Save(new FileStream("qrcode.png",FileMode.OpenOrCreate),ImageFormat.Png);
					Console.WriteLine("QR code saved at qrcode.png. Waiting for scanning.");
                });
            };
            qc.OnVerifyImage = () => {
                RunInMainthread(() => {
                    //QrCodePicture.Image = UserLogo;
					Console.WriteLine("Waiting for authorization");
                });
            };
            qc.OnVerifySucess = () => {
                RunInMainthread(() => {
					Console.WriteLine("Login confirmed. Loading session.");
                });
            };

            qc.OnLoginSucess = () => {
                RunInMainthread(() => {
					Console.WriteLine("Logged in.");
					initGroupList();
					Random randSource = new Random();
					for(int i=0;i<8;i++) AdminAuthStr += ((char)('a'+randSource.Next()%26)).ToString();
					Console.WriteLine(String.Format("Auth string: {0}\nSend it via private chat to the bot.",AdminAuthStr));

					ThreadPool.QueueUserWorkItem(messageReceiver);
                });
            };


            RunAsync(() => {
				if (File.Exists(Environment.CurrentDirectory + "/user/user.ini"))
                {
					Console.WriteLine("Trying to relink");
                    qc.ReLink();

                }
                else
                {
                    qc.Login();
                    
                }
            });

            //MessageBox.Show(SmartQQLib.API.Cookies.ptwebqq);
			while(true) Thread.Sleep(100000);

        }

        static void RunAsync(Action action)
        {
            ((Action)(delegate () {
                action?.Invoke();
            })).BeginInvoke(null, null);
        }

        static void RunInMainthread(Action action)
        {
			action?.Invoke();
        }

    }
}
