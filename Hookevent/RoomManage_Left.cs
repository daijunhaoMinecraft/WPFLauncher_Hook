using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using System.Windows;
using InlineIL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Model;
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.Util.FileExplorer;
using WPFLauncher.View.WebPage.Handlers;
using WPFLauncher.ViewModel.Share;
using MessageBox = System.Windows.MessageBox;
using static InlineIL.IL.Emit;

namespace DotNetTranstor.Hookevent
{
	//去除网易存档加密功能
	internal class RoomManage_Left : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		public static void Get_Room(string roomId)
		{
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.Protocol.LobbyGame.afx", "Left", "Get_Room")]
		// Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
		public static void Left(string roomId)
		{
			MessageBoxResult messageBoxResult = us.q("是否保存存档至云服务端?", "", "确定", "不保存", "");
			if (messageBoxResult == MessageBoxResult.OK)
			{
				bool flag_End = false;
				while (true)
				{
					if (flag_End)
					{
						break;
					}
					JObject Get_Backup_Return = JObject.Parse(X19Http.RequestX19Api("/online-lobby-backup/create",
						JsonConvert.SerializeObject(new { backup_id = "1", name = "Server_Backup" })));
					Console.ForegroundColor = ConsoleColor.Cyan;
					if (Get_Backup_Return["code"].ToObject<int>() != 0)
					{
						switch (Get_Backup_Return["code"].ToObject<int>())
						{
							case 12023:
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("[Backup]存档保存失败:" + Get_Backup_Return["message"].ToString());
								Console.ForegroundColor = ConsoleColor.White;
								Thread.Sleep(2000);
								flag_End = false;
								break;
							case 12026:
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("[Backup]存档保存失败:" + Get_Backup_Return["message"].ToString());
								Console.ForegroundColor = ConsoleColor.White;
								Thread.Sleep(2000);
								flag_End = false;
								break;
							case 12000:
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("[Backup]存档保存失败:" + Get_Backup_Return["message"].ToString());
								Console.ForegroundColor = ConsoleColor.White;
								flag_End = true;
								break;
						}
						//us.n("存档保存失败!\n" + Get_Backup_Return["message"], "");
					}
					else
					{
						Console.WriteLine("[RoomInfo][Auto_Backup]存档保存情况:");
						Console.WriteLine("-----------------------------------------------------------");
						Console.WriteLine("存档名称:" + Get_Backup_Return["entity"]["name"].ToString());
						Console.WriteLine("存档ResID:" + Get_Backup_Return["entity"]["res_id"].ToString());
						Console.WriteLine("存档保存时间:" + X19Http.unix_timestamp_to(Get_Backup_Return["entity"]["timestamp"].ToObject<long>()));
						Console.WriteLine("存档过期时间:" + X19Http.unix_timestamp_to(Get_Backup_Return["entity"]["expire_time"].ToObject<long>()));
						Console.WriteLine("存档大小:" + Get_Backup_Return["entity"]["size"].ToString() + "字节");
						Console.WriteLine("备份存档返回值:" + Get_Backup_Return["code"].ToString());
						Console.WriteLine("备份存档返回消息:" + Get_Backup_Return["message"].ToString());
						Console.WriteLine("-----------------------------------------------------------");
						flag_End = true;
					}
				}
			};
			if (Path_Bool.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "RoomManage", status = "Leave", data = new {roomId = roomId} }));
			}
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("[RoomManage]你已离开房间,房间ID:" + roomId);
			Console.ForegroundColor = ConsoleColor.White;
			Path_Bool.RoomInfo = null;
			Path_Bool.Password = "";
			Get_Room(roomId);
		}
	}
}
