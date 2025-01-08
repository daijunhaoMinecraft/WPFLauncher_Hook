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
	internal class RoomManage_2 : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		public static EntityResponse<LobbyGameRoomEntity> Get_Room(string jng)
		{
			return null;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.Protocol.LobbyGame.afx", "c", "Get_Room")]
		// Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
		public static EntityResponse<LobbyGameRoomEntity> c(string jng)
		{
			EntityResponse<LobbyGameRoomEntity> Get_Room_Info = Get_Room(jng);
			if (Get_Room_Info.code == 0)
			{
				JObject Get_Owner_Info = X19Http.Get_Player_Info(Get_Room_Info.entity.owner_id);
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine("[RoomInfo]成功获取到房间信息");
				Console.WriteLine("-----------------------------------------------------------");

				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"房间号: {Get_Room_Info.entity.room_name}");

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"是否有密码: {Get_Room_Info.entity.password}");

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"资源 ID: {Get_Room_Info.entity.res_id}");

				Console.ForegroundColor = ConsoleColor.Blue;
				Console.WriteLine($"最大人数: {Get_Room_Info.entity.max_count}");

				Console.ForegroundColor = ConsoleColor.Magenta;
				Console.WriteLine($"是否允许保存: {Get_Room_Info.entity.allow_save}");

				Console.ForegroundColor = ConsoleColor.Cyan;
				
				string visibilityDescription = Get_Room_Info.entity.visibility.GetType()
					.GetField(Get_Room_Info.entity.visibility.ToString())
					.GetCustomAttributes(typeof(DescriptionAttribute), false)
					.FirstOrDefault() is DescriptionAttribute descriptionAttribute ? descriptionAttribute.Description : Get_Room_Info.entity.visibility.ToString();
				Console.WriteLine($"房间可见性: {visibilityDescription}");  // 可见性可能是一个枚举类型

				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine($"房主 ID: {Get_Room_Info.entity.owner_id}");
				Console.WriteLine("房主名称:"+Get_Owner_Info["entity"]["name"]);
				
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine($"版本号: {Get_Room_Info.entity.version}");

				Console.ForegroundColor = ConsoleColor.DarkGreen;
				Console.WriteLine($"游戏状态: {Get_Room_Info.entity.game_status}");

				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.WriteLine($"当前玩家人数: {Get_Room_Info.entity.cur_num}");
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine("-----------------------------------------------------------");
				Console.ForegroundColor = ConsoleColor.White;
				Path_Bool.RoomInfo = Get_Room_Info;
				Path_Bool.RoomInfo.entity.fids.Add(ayx<aqz>.Instance.User.Id);
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[RoomInfo]获取房间信息失败:"+Get_Room_Info.message);
				Console.ForegroundColor = ConsoleColor.White;
				Path_Bool.RoomInfo = null;
			}
			return Get_Room_Info;
		}
	}
}
