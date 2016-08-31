using System;
using System.Collections.Generic;

using SmartQQLib;

namespace QQBotService
{
	public class ChatActions
	{
		public static void KickGroupMember(string msgContent, long msgFromChat, long msgSendUin) {
			long targetGid = 0;
			try {
				targetGid = QQBot.groupIdMapping [msgFromChat];
			} catch(KeyNotFoundException) {
				Console.WriteLine ("Mapping for target group chat {0} not found.", msgFromChat);
				return;
			}

			long targetUid = long.Parse(msgContent);

			string result = QQBot.qc.KickGroupMember (targetGid,targetUid);
			Console.WriteLine ("Member {0} kicked from group {1}. API result: {2}", targetUid,targetGid,result);
		}

		public static void ShutupGroupMember(string msgContent, long msgFromChat, long msgSendUin) {
			long targetGid = 0;
			try {
				targetGid = QQBot.groupIdMapping [msgFromChat];
			} catch(KeyNotFoundException) {
				Console.WriteLine ("Mapping for target group chat {0} not found.", msgFromChat);
				return;
			}

			if (msgContent == "all") {
				string ret = QQBot.qc.ShutupGroup (targetGid);
				Console.WriteLine ("Group {0} shutted up. API result: {1}", targetGid, ret);
				return;
			} else if (msgContent == "disable") {
				string ret = QQBot.qc.ShutupGroupDisable (targetGid);
				Console.WriteLine ("Group {0} disabled shutting-up. API result: {1}", targetGid, ret);
				return;
			}

			long targetUid = long.Parse(msgContent);

			string result = QQBot.qc.ShutupGroupMember (targetGid,targetUid);
			Console.WriteLine ("Member {0} shutted up in group {1}. API result: {2}", targetUid,targetGid,result);
		}

		public static void AddGroupMapping(string msgContent, long msgFromChat, long msgSendUin) {
			QQBot.groupIdMapping [msgFromChat] = long.Parse(msgContent);
		}
	}
}

