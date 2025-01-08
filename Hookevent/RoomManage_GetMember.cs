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
using Newtonsoft.Json.Linq;
using WPFLauncher;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Model;
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.View.WebPage;
using WPFLauncher.ViewModel.Share;
using MessageBox = System.Windows.MessageBox;
using static InlineIL.IL.Emit;

namespace DotNetTranstor.Hookevent
{
	//去除网易存档加密功能
	internal class RoomManage_GetMember : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		private void Get_Room(EntityListResponse<LobbyRoomMemberInfoEntity> dqs)
		{
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.ViewModel.LobbyGame.jm", "v", "Get_Room")]
		private void v(EntityListResponse<LobbyRoomMemberInfoEntity> dqs)
		{
			Path_Bool.RoomInfo.entity.fids.Clear();
			List<string> Get_Player_Uid_List = new List<string>();
			foreach (var GetMemberInfo in dqs.entities)
			{
				Get_Player_Uid_List.Add(GetMemberInfo.member_id.ToString());
			}
			int sum = 0;
			JObject Get_Player_Info = X19Http.Get_Players_Info(Get_Player_Uid_List);
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("[RoomInfo]获取房间成员信息成功,以下是成员信息:");
			Console.WriteLine("-----------------------------------------------------------");

			foreach (var GetMemberInfo in dqs.entities)
			{
				Path_Bool.RoomInfo.entity.fids.Add(GetMemberInfo.member_id.ToString());

				Console.ForegroundColor = (ConsoleColor)((sum % 14) + 1); // 循环使用不同的颜色
				string Get_Member_Rank = GetMemberInfo.ident == 1 ? "房主" : "成员";
				Console.WriteLine("[RoomInfo]房间成员uid:" + GetMemberInfo.member_id.ToString() + " 玩家名称:" + Get_Player_Info["entities"][sum]["name"] + " 房间内的权限情况:" +　Get_Member_Rank);
				sum += 1;
			}
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("-----------------------------------------------------------");
			Console.ForegroundColor = ConsoleColor.White;
			Path_Bool.RoomInfo.entity.cur_num = (uint)dqs.entities.Count;
			Get_Room(dqs);
		}
	}
}
