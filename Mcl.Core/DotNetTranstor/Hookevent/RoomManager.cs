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
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using System.Windows;
using System.Windows.Threading;
using Mcl.Core.DotNetTranstor.Tools;
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
using WPFLauncher.Model.Game;
using WPFLauncher.Network;
using WPFLauncher.SQLite;

namespace DotNetTranstor.Hookevent
{
	//去除网易存档加密功能
	internal class RoomManager : IMethodHook
	{
		#region CreateRoom
		[OriginalMethod]
		private void CreateRoom_Original(EntityResponse<LobbyGameRoomEntity> result)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.ViewModel.LobbyGame.jo", "d", "CreateRoom_Original")]
		private void CreateRoom(EntityResponse<LobbyGameRoomEntity> result)
		{
			Path_Bool.RoomInfo = result;
			if (result.code == 0)
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				
				// 初始化房主信息
				if (result.entity.fids == null)
				{
					result.entity.fids = new List<string>();
				}
				
				// 确保房主在玩家列表中
				string ownerId = result.entity.owner_id;
				if (!result.entity.fids.Contains(ownerId))
				{
					result.entity.fids.Add(ownerId);
				}

				// 显示房间信息窗口
				Application.Current.Dispatcher.Invoke(() =>
				{
					// 检查是否已存在相同房间信息的窗口，如果存在则关闭
					foreach (Window w in Application.Current.Windows)
					{
						if (w is RoomInfoWindow existingWindow && existingWindow.roomInfoResponse.entity.entity_id == result.entity.entity_id)
						{
							existingWindow.Close();
							break;
						}
					}
					
					// 创建并显示新窗口
					var window = new RoomInfoWindow(result);
					window.Show();
				});

				Console.WriteLine("RoomInfo:");
				Console.WriteLine("-----------------------------------------------------------");
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"房间号: {result.entity.room_name}");

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"是否有密码: {result.entity.password}");

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"资源 ID: {result.entity.res_id}");
				Console.WriteLine($"房间 RoomID: {result.entity.entity_id}");
				
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.WriteLine($"最大人数: {result.entity.max_count}");

				Console.ForegroundColor = ConsoleColor.Magenta;
				Console.WriteLine($"是否允许保存: {result.entity.allow_save}");

				Console.ForegroundColor = ConsoleColor.Cyan;
				string visibilityDescription = result.entity.visibility.GetType()
					.GetField(result.entity.visibility.ToString())
					.GetCustomAttributes(typeof(DescriptionAttribute), false)
					.FirstOrDefault() is DescriptionAttribute descriptionAttribute ? descriptionAttribute.Description : result.entity.visibility.ToString();
				Console.WriteLine($"房间可见性: {visibilityDescription}");

				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine($"房主 ID: {result.entity.owner_id}");
				JObject ownerInfo = X19Http.Get_Player_Info(result.entity.owner_id);
				Console.WriteLine("房主名称:" + ownerInfo["entity"]["name"]);

				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine($"存档 ID: {result.entity.save_id}");

				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine($"存档大小: {result.entity.save_size} bytes");

				Console.ForegroundColor = ConsoleColor.DarkGreen;
				Console.WriteLine($"版本号: {result.entity.version}");

				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.WriteLine($"游戏状态: {result.entity.game_status}");

				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				Console.WriteLine($"当前玩家人数: {result.entity.cur_num}");

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
				BuildPostGetPlayerInfo.entity_ids.Add(result.entity.owner_id);
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
							currentUserId = aze<arg>.Instance.User.Id,
							ownerId = Path_Bool.RoomInfo.entity.owner_id
						}
					}));
				}
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"[RoomInfo]创建房间失败,错误码: {result.code}, 错误信息: {result.message}");
				Console.ForegroundColor = ConsoleColor.White;
				Path_Bool.RoomInfo = null;
			}
			CreateRoom_Original(result);
		}
		
		[OriginalMethod]
		public static void CreateRoomOriginalRequest_Original(string RoomName, string ResID, uint Visivility, uint MaxCount, string SaveID, string Password, Action<EntityResponse<LobbyGameRoomEntity>> RequestAction)
		{
		}
		
		[HookMethod("WPFLauncher.Network.Protocol.LobbyGame.age","a","CreateRoomOriginalRequest_Original")]
		public static void CreateRoomOriginalRequest(string RoomName, string ResID, uint Visivility, uint MaxCount, string SaveID, string Password, Action<EntityResponse<LobbyGameRoomEntity>> RequestAction)
		{
			DebugPrint.LogDebug($"[RoomManage] 当前正在创建房间,房间ResID:{ResID},密码:{Password}");
			Path_Bool.Password = Password;
			CreateRoomOriginalRequest_Original(RoomName, ResID, Visivility, MaxCount, SaveID, Password, RequestAction);
		}
		#endregion

		#region JoinRoom

		[OriginalMethod]
		public static EntityResponse<LobbyGameRoomEntity> JoinRoom_Original(string roomId)
		{
			return null;
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.Protocol.LobbyGame.age", "c", "JoinRoom_Original")]
		public static EntityResponse<LobbyGameRoomEntity> JoinRoom(string roomId)
		{
			if (roomId.Length < 8)
			{
				// 错误的房间号
				return new EntityResponse<LobbyGameRoomEntity>
				{
					code = 1,
					message = "找不到房间"
				};
			}
			EntityResponse<LobbyGameRoomEntity> Get_Room_Info = JoinRoom_Original(roomId);
			if (Get_Room_Info.code == 0)
			{
				// 显示房间信息窗口
				Application.Current.Dispatcher.Invoke(() =>
				{
					// 检查是否已存在相同房间信息的窗口，如果存在则关闭
					foreach (Window w in Application.Current.Windows)
					{
						if (w is RoomInfoWindow existingWindow && existingWindow.roomInfoResponse.entity.entity_id == Get_Room_Info.entity.entity_id)
						{
							existingWindow.Close();
							break;
						}
					}
					
					// 创建并显示新窗口
					var window = new RoomInfoWindow(Get_Room_Info);
					window.Show();
				});
				JObject Get_Owner_Info = X19Http.Get_Player_Info(Get_Room_Info.entity.owner_id);
				Console.ForegroundColor = ConsoleColor.Cyan;
				DebugPrint.LogDebug_NoColorSelect("[RoomInfo]成功获取到房间信息");
				Console.WriteLine("-----------------------------------------------------------");

				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"房间号: {Get_Room_Info.entity.room_name}");

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"是否有密码: {Get_Room_Info.entity.password}");

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"资源 ID: {Get_Room_Info.entity.res_id}");
				Console.WriteLine($"房间 RoomID: {Get_Room_Info.entity.entity_id}");

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
				Path_Bool.RoomInfo.entity.fids.Add(aze<arg>.Instance.User.Id);
				Path_Bool.JoinOrCreateTime = X19Http.TimestampHelper.GetCurrentTimestampMilliseconds();
				Console.WriteLine($"[RoomManage] 加入房间时间:{Path_Bool.JoinOrCreateTime}");
				if (Path_Bool.IsStartWebSocket)
				{
					WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "RoomManage",status = "JoinRoom",data = Path_Bool.RoomInfo }));
				}
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

		#endregion

		#region KickEvent

		[OriginalMethod]
		private new void SendErrToWpf(abx packet)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.ChatService.ahl", "c", "SendErrToWpf")]
		private new void ReJoinRoom(abx packet)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("[RoomInfo]你已被房主踢出房间,正在重新加入房间");
			while (true)
			{
				JObject Get_RoomEnter_Info = JObject.Parse(X19Http.RequestX19Api("/online-lobby-room-enter",
					JsonConvert.SerializeObject(new
					{
						room_id = Path_Bool.RoomInfo.entity.entity_id, password = Path_Bool.Password,
						check_visibilily = true
					})));
				if (Get_RoomEnter_Info["code"].ToObject<int>() == 0)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine("[RoomInfo]成功加入房间!");
					break;
				}
				else if (Get_RoomEnter_Info["code"].ToObject<int>() == 12022)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"[RoomERROR]加入房间失败:{Get_RoomEnter_Info["message"]},等待0.8秒后再次加入房间");
					Thread.Sleep(200);
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"[RoomERROR]加入房间失败:{Get_RoomEnter_Info["message"]}");
					SendErrToWpf(packet);
					break;
				}
			}
			Console.ForegroundColor = ConsoleColor.White;
		}

		#endregion

		#region GetRoomIP

		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
        [OriginalMethod]
        public bool GetRoomIpOriginal(akv config, BaseWindow window)
        {
            return true;
        }

        // Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
        [CompilerGenerated]
        [HookMethod("WPFLauncher.Manager.Game.aum", "d", "GetRoomIpOriginal")]
        // Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
        public bool GetRoomIp(akv config, BaseWindow window)
        {
            Path_Bool.IsSelectedIP = false;
            bool Get_FlagBool = GetRoomIpOriginal(config, window);
            if (Get_FlagBool)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[RoomInfo][IPConfig]房间IP地址:");
                Console.WriteLine("-----------------------------------------------------------");
                Console.WriteLine($"IP: {config.CppGameCfg.room_info.ip}");
                Console.WriteLine($"Port: {config.CppGameCfg.room_info.port}");
                Console.WriteLine("-----------------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;

                // 检查是否满足特定条件，如果满足则显示IP更改界面
                if (Path_Bool.IsCustomIP)
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                    {
                        using (var changeIPForm = new ChangeIPForm(config))
                        {
                            changeIPForm.ShowDialog();
                        }
                    });
                }
                
                if (Path_Bool.IsStartWebSocket)
                {
                    var settings = new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // 忽略循环
                        NullValueHandling = NullValueHandling.Ignore,        // 可选：不输出 null 字段
                        Formatting = Formatting.None                          // 可选：减少体积
                    };
                    
                    WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                        { type = "RoomManage", status = "GetRoomCppGame", data = config.CppGameCfg }, settings));
                }
            }

            return Get_FlagBool;
            // }
            // else
            // // {
            // 	Console.ForegroundColor = ConsoleColor.Red;
            // 	Console.WriteLine("[RoomInfo][IPConfig]获取房间IP地址失败!");
            // 	Console.ForegroundColor = ConsoleColor.White;
            // 	return false;
            // }
        }

		#endregion

		#region GetRoomMemberInfo

		[OriginalMethod]
		private void GetMemberInfoOriginal(EntityListResponse<LobbyRoomMemberInfoEntity> result)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.ViewModel.LobbyGame.jo", "v", "GetMemberInfoOriginal")]
		private void v(EntityListResponse<LobbyRoomMemberInfoEntity> result)
		{
			if (Path_Bool.RoomInfo?.entity == null) return;

			// 获取当前房主ID
			string ownerId = Path_Bool.RoomInfo.entity.owner_id;
			
			// 创建新的玩家列表
			var newPlayerList = new List<string>();
			foreach (var GetMemberInfo in result.entities)
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
			foreach (var AllMemberInfo in result.entities)
			{
				if (AllMemberInfo.member_id == aze<arg>.Instance.User.UserID)
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
				result.entities.Add(new WPFLauncher.Network.Protocol.LobbyGame.LobbyRoomMemberInfoEntity(){member_id = aze<arg>.Instance.User.UserID,ident = HaveOwner ? 0 : 1});
			}
			// 获取玩家信息
			JObject Get_Player_Info = X19Http.Get_Players_Info(newPlayerList);
			Console.ForegroundColor = ConsoleColor.Cyan;
			DebugPrint.LogDebug_NoColorSelect("[RoomInfo]获取房间成员信息成功,以下是成员信息:");
			DebugPrint.LogDebug_NoColorSelect("-----------------------------------------------------------");

			int sum = 0;
			foreach (var GetMemberInfo in result.entities)
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
				foreach (var GetMemberInfo in result.entities)
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

			GetMemberInfoOriginal(result);
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

		#endregion

		#region GetUserInputPassword

		[OriginalMethod]
		public static EntityResponse<EntityBase> SendJoinRoomRequest(string roomId, string password, bool statusVisibility)
		{
			return null;
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.Protocol.LobbyGame.age", "f", "SendJoinRoomRequest")]
		public static EntityResponse<EntityBase> f(string roomId, string password, bool statusVisibility)
		{
			EntityResponse<EntityBase> result = SendJoinRoomRequest(roomId, password, statusVisibility);
			if (result.code == 0)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("[房间信息][进入房间]房间详细信息:");
				Console.WriteLine("-----------------------------------------------------------");
				
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"房间状态: 成功进入");
				Console.WriteLine($"返回代码: {result.code}");
				Console.WriteLine($"系统消息: {result.message}");
				Console.WriteLine($"房间ID: {roomId}");
				Console.WriteLine($"房间密码: {(string.IsNullOrEmpty(password) ? "无密码" : password)}");
				Console.WriteLine($"隐藏状态: {(statusVisibility ? "是" : "否")}");
				
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("-----------------------------------------------------------");
				Console.ForegroundColor = ConsoleColor.White;
				Path_Bool.Password = password;
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[房间信息][进入房间]房间详细信息:");
				Console.WriteLine("-----------------------------------------------------------");
				Console.WriteLine($"房间状态: 失败");
				Console.WriteLine($"返回代码: {result.code}");
				Console.WriteLine($"系统消息: {result.message}");
				Console.WriteLine($"房间ID: {roomId}");
				Console.WriteLine($"房间密码: {(string.IsNullOrEmpty(password) ? "无密码" : password)}");
				Console.WriteLine($"隐藏状态: {(statusVisibility ? "是" : "否")}");
				Console.WriteLine("-----------------------------------------------------------");
				Console.ResetColor();
				if (result.code == 12003)
				{
					Path_Bool.JoinFailRetry++;
				}
				if (Path_Bool.JoinFailRetry == 2)
				{
					MessageBoxResult loadConfigResult = uz.q("警告:已连续尝试加入房间2次均为无法重复进入房间,是否退出此前进入过的房间?", "", "是", "否", "");
					if (loadConfigResult == MessageBoxResult.OK)
					{
						bool bGetExitRoomResult = ExitRoom.autoExitRoom();
						string sMessage = bGetExitRoomResult ? "成功退出房间(请重新点击加入房间)" : "退出房间失败,详细请见控制台";
						uz.n(sMessage, "");
						Path_Bool.JoinFailRetry = 0;
					}
					else
					{
						Path_Bool.JoinFailRetry = 0;
					}
				}
			}
			return result;
		}

		#endregion

		#region UserExitRoom
		
		[OriginalMethod]
		public static void LeftOriginal(string roomId)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.Protocol.LobbyGame.age", "Left", "LeftOriginal")]
		public static void Left(string roomId)
		{
			if (Path_Bool.AlwaysSaveWorld && (Path_Bool.RoomInfo.entity.allow_save || Path_Bool.RoomInfo.entity.owner_id == aze<arg>.Instance.User.Id.ToString()))
			{
				MessageBoxResult messageBoxResult = uz.q("是否保存存档至云服务端?", "", "确定", "不保存", "");
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
									DebugPrint.LogDebug_NoColorSelect("[Backup]存档保存失败:" + Get_Backup_Return["message"].ToString());
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
							//uu.n("存档保存失败!\n" + Get_Backup_Return["message"], "");
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
			}

			// 关闭RoomInfoWindow窗口
			Application.Current.Dispatcher.Invoke(() =>
			{
				// 检查是否已存在相同房间信息的窗口，如果存在则关闭
				foreach (Window w in Application.Current.Windows)
				{
					if (w is RoomInfoWindow existingWindow && existingWindow.roomInfoResponse.entity.entity_id == roomId)
					{
						existingWindow.Close();
						break;
					}
				}
			});

			if (Path_Bool.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "RoomManage", status = "Leave", data = new {roomId = roomId} }));
			}
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("[RoomManage]你已离开房间,房间ID:" + roomId);
			Console.ForegroundColor = ConsoleColor.White;
			Path_Bool.RoomInfo = null;
			Path_Bool.Password = "";
			Path_Bool.RoomPlayerList.Clear();
			Path_Bool.JoinOrCreateTime = 0;
			LeftOriginal(roomId);
		}

		#endregion

		#region RemoveSaveCloudNameLimits
		// 去除联机大厅保存存档时的长度限制

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Util.ur", "a", null)]
		public static float GetStringLength(string text)
		{
			return 4f;
		}

		#endregion
	}
}

