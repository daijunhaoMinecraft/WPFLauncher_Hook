using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
using Newtonsoft.Json.Linq;
using WPFLauncher;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Model;
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.ViewModel.Share;
using MessageBox = System.Windows.MessageBox;
using static InlineIL.IL.Emit;

namespace DotNetTranstor.Hookevent
{
	//去除网易存档加密功能
	internal class RoomManage_GetRoomEnterPassword : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		public static EntityResponse<EntityBase> Get_Room(string jns, string jnt, bool jnu)
		{
			return null;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.Protocol.LobbyGame.afx", "f", "Get_Room")]
		public static EntityResponse<EntityBase> f(string jns, string jnt, bool jnu)
		{
			EntityResponse<EntityBase> result = Get_Room(jns, jnt, jnu);
			if (result.code == 0)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("[房间信息][进入房间]房间详细信息:");
				Console.WriteLine("-----------------------------------------------------------");
				
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"房间状态: 成功进入");
				Console.WriteLine($"返回代码: {result.code}");
				Console.WriteLine($"系统消息: {result.message}");
				Console.WriteLine($"房间ID: {jns}");
				Console.WriteLine($"房间密码: {(string.IsNullOrEmpty(jnt) ? "无密码" : jnt)}");
				Console.WriteLine($"隐藏状态: {(jnu ? "是" : "否")}");
				
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("-----------------------------------------------------------");
				Console.ForegroundColor = ConsoleColor.White;
				Path_Bool.Password = jnt;
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[房间信息][进入房间]房间详细信息:");
				Console.WriteLine("-----------------------------------------------------------");
				Console.WriteLine($"房间状态: 失败");
				Console.WriteLine($"返回代码: {result.code}");
				Console.WriteLine($"系统消息: {result.message}");
				Console.WriteLine($"房间ID: {jns}");
				Console.WriteLine($"房间密码: {(string.IsNullOrEmpty(jnt) ? "无密码" : jnt)}");
				Console.WriteLine($"隐藏状态: {(jnu ? "是" : "否")}");
				Console.WriteLine("-----------------------------------------------------------");
				Console.ResetColor();
			}
			return result;
		}
	}
}
