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

using Application = System.Windows.Application;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
	//去除网易存档加密功能
	internal class RoomManage : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		private void Get_Room(EntityResponse<LobbyGameRoomEntity> dpv)
		{
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.ViewModel.LobbyGame.jo", "d", "Get_Room")]
		// Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
		private void d(EntityResponse<LobbyGameRoomEntity> dpv)
		{
			Path_Bool.RoomInfo = dpv;
			if (dpv.code == 0)
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				
				// 初始化房主信息
				if (dpv.entity.fids == null)
				{
					dpv.entity.fids = new List<string>();
				}
				
				// 确保房主在玩家列表中
				string ownerId = dpv.entity.owner_id;
				if (!dpv.entity.fids.Contains(ownerId))
				{
					dpv.entity.fids.Add(ownerId);
				}

				// 显示房间信息窗口
				Application.Current.Dispatcher.Invoke(() =>
				{
					var window = new RoomInfoWindow(dpv);
					window.Show();
				});
				Console.WriteLine("RoomInfo:");
				Console.WriteLine("-----------------------------------------------------------");
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"房间号: {dpv.entity.room_name}");

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"是否有密码: {dpv.entity.password}");

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"资源 ID: {dpv.entity.res_id}");
				Console.WriteLine($"房间 RoomID: {dpv.entity.entity_id}");
				
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.WriteLine($"最大人数: {dpv.entity.max_count}");

				Console.ForegroundColor = ConsoleColor.Magenta;
				Console.WriteLine($"是否允许保存: {dpv.entity.allow_save}");

				Console.ForegroundColor = ConsoleColor.Cyan;
				string visibilityDescription = dpv.entity.visibility.GetType()
					.GetField(dpv.entity.visibility.ToString())
					.GetCustomAttributes(typeof(DescriptionAttribute), false)
					.FirstOrDefault() is DescriptionAttribute descriptionAttribute ? descriptionAttribute.Description : dpv.entity.visibility.ToString();
				Console.WriteLine($"房间可见性: {visibilityDescription}");

				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine($"房主 ID: {dpv.entity.owner_id}");
				JObject ownerInfo = X19Http.Get_Player_Info(dpv.entity.owner_id);
				Console.WriteLine("房主名称:" + ownerInfo["entity"]["name"]);

				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine($"存档 ID: {dpv.entity.save_id}");

				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine($"存档大小: {dpv.entity.save_size} bytes");

				Console.ForegroundColor = ConsoleColor.DarkGreen;
				Console.WriteLine($"版本号: {dpv.entity.version}");

				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.WriteLine($"游戏状态: {dpv.entity.game_status}");

				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				Console.WriteLine($"当前玩家人数: {dpv.entity.cur_num}");

				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine("-----------------------------------------------------------");
				Console.ForegroundColor = ConsoleColor.White;

				// 确保HTML目录存在
				// string htmlDir = Path.Combine(Directory.GetCurrentDirectory(), "DotNetTranstor", "HTML");
				// if (!Directory.Exists(htmlDir))
				// {
				// 	try
				// 	{
				// 		Directory.CreateDirectory(htmlDir);
				// 		Console.WriteLine("[Roommanage] 创建HTML目录成功: " + htmlDir);
				// 	}
				// 	catch (Exception ex)
				// 	{
				// 		Console.WriteLine("[Roommanage] 创建HTML目录失败: " + ex.Message);
				// 	}
				// }
				Path_Bool.JoinOrCreateTime = X19Http.TimestampHelper.GetCurrentTimestampMilliseconds();
				Console.WriteLine($"[RoomManage] 创建房间时间:{Path_Bool.JoinOrCreateTime}");
				Path_Bool.RoomPlayerList.Clear();
				var BuildPostGetPlayerInfo = new
				{
					entity_ids = new List<string>()
				};
				BuildPostGetPlayerInfo.entity_ids.Add(dpv.entity.owner_id);
				// 获取玩家信息
				JObject GetPlayerInfoResult = JObject.Parse(X19Http.RequestX19Api("/user/query/search-by-ids",JsonConvert.SerializeObject(BuildPostGetPlayerInfo)));
				foreach (var PlayerInfo in GetPlayerInfoResult["entities"])
				{
					PlayerInfo["JoinRoomTime"] = Path_Bool.JoinOrCreateTime;
					Path_Bool.RoomPlayerList.Add(JObject.Parse(PlayerInfo.ToString()));
				}
				// 发送WebSocket通知
				if (Path_Bool.IsStartWebSocket)
				{
					WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
					{ 
						type = "RoomManage",
						status = "CreateRoom",
						data = new
						{
							entity = Path_Bool.RoomInfo.entity,
							isOwner = true,
							currentUserId = azd<arf>.Instance.User.Id,
							ownerId = Path_Bool.RoomInfo.entity.owner_id
						}
					}));
				}
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"[RoomInfo]创建房间失败,错误码: {dpv.code}, 错误信息: {dpv.message}");
				Console.ForegroundColor = ConsoleColor.White;
				Path_Bool.RoomInfo = null;
			}
			Get_Room(dpv);
		}
		
		[OriginalMethod]
		public static void CreateRoomOriginalRequest_Original(string RoomName, string ResID, uint Visivility, uint MaxCount, string SaveID, string Password, Action<EntityResponse<LobbyGameRoomEntity>> RequestAction)
		{
		}
		
		[HookMethod("WPFLauncher.Network.Protocol.LobbyGame.agd","a","CreateRoomOriginalRequest_Original")]
		public static void CreateRoomOriginalRequest(string RoomName, string ResID, uint Visivility, uint MaxCount, string SaveID, string Password, Action<EntityResponse<LobbyGameRoomEntity>> RequestAction)
		{
			DebugPrint.LogDebug($"[RoomManage] 当前正在创建房间,房间ResID:{ResID},密码:{Password}");
			Path_Bool.Password = Password;
			CreateRoomOriginalRequest_Original(RoomName, ResID, Visivility, MaxCount, SaveID, Password, RequestAction);
		}
	}
}

