using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using System.Windows;
using InlineIL;
using Newtonsoft.Json;
using WPFLauncher;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Model;
using WPFLauncher.Model.Game;
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.ViewModel.Share;
using MessageBox = System.Windows.MessageBox;
using static InlineIL.IL.Emit;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
	//去除网易存档长度问题
	internal class RoomManage_GetIP : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		public bool Get_Room(aku oab, BaseWindow oac)
		{
			return true;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.Manager.Game.aul", "d", "Get_Room")]
		// Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
		public bool d(aku oab, BaseWindow oac)
		{
			bool Get_FlagBool = Get_Room(oab, oac);
			if (Get_FlagBool)
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine("[RoomInfo][IPConfig]房间IP地址:");
				Console.WriteLine("-----------------------------------------------------------");
				Console.WriteLine($"IP: {oab.CppGameCfg.room_info.ip}");
				Console.WriteLine($"Port: {oab.CppGameCfg.room_info.port}");
				Console.WriteLine("-----------------------------------------------------------");
				Console.ForegroundColor = ConsoleColor.White;
				// if (Path_Bool.IsStartWebSocket)
				// {
				// 	WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "RoomManage",status = "GetRoomInfo",data = oab }));
				// }
				return true;
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[RoomInfo][IPConfig]获取房间IP地址失败!");
				Console.ForegroundColor = ConsoleColor.White;
				return false;
			}
		}
	}
}
