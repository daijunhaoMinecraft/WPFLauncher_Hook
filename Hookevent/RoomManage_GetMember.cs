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
using Application = System.Windows.Application;
using MicrosoftTranslator.DotNetTranstor.Tools;

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
		[HookMethod("WPFLauncher.ViewModel.LobbyGame.jp", "v", "Get_Room")]
		private void v(EntityListResponse<LobbyRoomMemberInfoEntity> dqs)
		{
			if (Path_Bool.RoomInfo?.entity == null) return;

			// 获取当前房主ID
			string ownerId = Path_Bool.RoomInfo.entity.owner_id;
			
			// 创建新的玩家列表
			var newPlayerList = new List<string>();
			foreach (var GetMemberInfo in dqs.entities)
			{
				newPlayerList.Add(GetMemberInfo.member_id.ToString());
			}

			// 确保房主在列表中
			if (!newPlayerList.Contains(ownerId))
			{
				newPlayerList.Add(ownerId);
			}
			
			// 确保当前用户在列表中
			bool UserInList = false;
			bool HaveOwner = false;
			foreach (var AllMemberInfo in dqs.entities)
			{
				if (AllMemberInfo.member_id == azd<arf>.Instance.User.UserID)
				{
					UserInList = true;
				}

				if (AllMemberInfo.ident == 1)
				{
					HaveOwner = true;
				}
			}

			if (!UserInList)
			{
				dqs.entities.Add(new WPFLauncher.Network.Protocol.LobbyGame.LobbyRoomMemberInfoEntity(){member_id = azd<arf>.Instance.User.UserID,ident = HaveOwner ? 0 : 1});
			}
			// 获取玩家信息
			JObject Get_Player_Info = X19Http.Get_Players_Info(newPlayerList);
			Console.ForegroundColor = ConsoleColor.Cyan;
			DebugPrint.LogDebug_NoColorSelect("[RoomInfo]获取房间成员信息成功,以下是成员信息:");
			DebugPrint.LogDebug_NoColorSelect("-----------------------------------------------------------");

			int sum = 0;
			foreach (var GetMemberInfo in dqs.entities)
			{
				Console.ForegroundColor = (ConsoleColor)((sum % 14) + 1);
				string Get_Member_Rank = GetMemberInfo.ident == 1 ? "房主" : "成员";
				DebugPrint.LogDebug_NoColorSelect("[RoomInfo]房间成员uid:" + GetMemberInfo.member_id.ToString() + 
								" 玩家名称:" + Get_Player_Info["entities"][sum]["name"] + 
								" 房间内的权限情况:" + Get_Member_Rank);
				sum += 1;
			}

			Console.ForegroundColor = ConsoleColor.Cyan;
			DebugPrint.LogDebug_NoColorSelect("-----------------------------------------------------------");
			Console.ForegroundColor = ConsoleColor.White;

			// 更新房间信息
			Path_Bool.RoomInfo.entity.fids = newPlayerList;
			Path_Bool.RoomInfo.entity.cur_num = (uint)newPlayerList.Count;
			
			Path_Bool.RoomPlayerList.Clear();
			foreach (var PlayerInfo in Get_Player_Info["entities"])
			{
				PlayerInfo["JoinRoomTime"] = 0;
				Path_Bool.RoomPlayerList.Add(JObject.Parse(PlayerInfo.ToString()));
			}
			
			// 更新RoomInfoWindow
			Application.Current.Dispatcher.Invoke(() =>
			{
				var roomInfoWindow = GetRoomInfoWindow();
				if (roomInfoWindow != null)
				{
					roomInfoWindow.UpdatePlayersList(newPlayerList);
				}
			});

			// 发送WebSocket通知
			if (Path_Bool.IsStartWebSocket)
			{
				var playerInfoList = new JArray();
				int infoIndex = 0;
				foreach (var GetMemberInfo in dqs.entities)
				{
					string memberRole = GetMemberInfo.ident == 1 ? "房主" : "成员";
					string memberName = Get_Player_Info["entities"][infoIndex]["name"]?.ToString() ?? "未知玩家";
					string memberAvatar = Get_Player_Info["entities"][infoIndex]["avatar_image_url"]?.ToString() ?? "";
					string memberSignature = Get_Player_Info["entities"][infoIndex]["signature"]?.ToString() ?? "";
					
					playerInfoList.Add(JToken.FromObject(new
					{
						userId = GetMemberInfo.member_id.ToString(),
						playerName = memberName,
						avatarUrl = memberAvatar,
						role = memberRole,
						ident = GetMemberInfo.ident,
						signature = memberSignature
					}));
					infoIndex++;
				}

				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
				{
					type = "RoomManage",
					status = "UpdatePlayers",
					data = new
					{
						players = playerInfoList,
						roomInfo = Path_Bool.RoomInfo?.entity
					}
				}));
			}

			Get_Room(dqs);
		}

		private static RoomInfoWindow GetRoomInfoWindow()
		{
			foreach (Window window in Application.Current.Windows)
			{
				if (window is RoomInfoWindow roomInfoWindow)
				{
					return roomInfoWindow;
				}
			}
			return null;
		}
	}
}
