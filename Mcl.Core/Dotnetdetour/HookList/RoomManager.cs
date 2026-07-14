using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Mcl.Core.Dotnetdetour.Tools;
using Mcl.Core.NeteaseProtocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Code;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Model.Game;
using WPFLauncher.Network;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.Util;
using Application = System.Windows.Application;

namespace Mcl.Core.Dotnetdetour.HookList;

//去除网易存档加密功能
internal class RoomManager : IMethodHook
{
    #region RemoveSaveCloudNameLimits

    // 去除联机大厅保存存档时的长度限制

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Util.ur", "a")]
    public static float GetStringLength(string text)
    {
        return 4f;
    }

    #endregion

    #region CreateRoom

    [OriginalMethod]
    private void CreateRoom_Original(EntityResponse<LobbyGameRoomEntity> result)
    {
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.ViewModel.LobbyGame.jo", "d", "CreateRoom_Original")]
    private void CreateRoom(EntityResponse<LobbyGameRoomEntity> result)
    {
        WpfConfig.RoomInfo = result;
        if (result.code == 0)
        {
            // 初始化房主信息
            if (result.entity.fids == null) result.entity.fids = new List<string>();

            // 确保房主在玩家列表中
            var ownerId = result.entity.owner_id;
            if (!result.entity.fids.Contains(ownerId)) result.entity.fids.Add(ownerId);

            // 显示房间信息窗口
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 检查是否已存在相同房间信息的窗口，如果存在则关闭
                foreach (System.Windows.Window w in Application.Current.Windows)
                    if (w is RoomInfoWindow existingWindow &&
                        existingWindow.roomInfoResponse.entity.entity_id == result.entity.entity_id)
                    {
                        existingWindow.Close();
                        break;
                    }

                // 创建并显示新窗口
                var window = new RoomInfoWindow(result);
                window.Show();
            });

            WpfConfig.DefaultLogger.Info("RoomInfo:");
            WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");

            WpfConfig.DefaultLogger.Info($"房间号: {result.entity.room_name}");


            WpfConfig.DefaultLogger.Info($"是否有密码: {result.entity.password}");


            WpfConfig.DefaultLogger.Info($"资源 ID: {result.entity.res_id}");
            WpfConfig.DefaultLogger.Info($"房间 RoomID: {result.entity.entity_id}");


            WpfConfig.DefaultLogger.Info($"最大人数: {result.entity.max_count}");


            WpfConfig.DefaultLogger.Info($"是否允许保存: {result.entity.allow_save}");


            var visibilityDescription = result.entity.visibility.GetType()
                .GetField(result.entity.visibility.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() is DescriptionAttribute descriptionAttribute
                ? descriptionAttribute.Description
                : result.entity.visibility.ToString();
            WpfConfig.DefaultLogger.Info($"房间可见性: {visibilityDescription}");


            WpfConfig.DefaultLogger.Info($"房主 ID: {result.entity.owner_id}");
            var ownerInfo = X19Http.GetPlayerInfo(result.entity.owner_id);
            WpfConfig.DefaultLogger.Info("房主名称:" + ownerInfo["entity"]["name"]);


            WpfConfig.DefaultLogger.Info($"存档 ID: {result.entity.save_id}");


            WpfConfig.DefaultLogger.Info($"存档大小: {result.entity.save_size} bytes");


            WpfConfig.DefaultLogger.Info($"版本号: {result.entity.version}");


            WpfConfig.DefaultLogger.Info($"游戏状态: {result.entity.game_status}");


            WpfConfig.DefaultLogger.Info($"当前玩家人数: {result.entity.cur_num}");


            WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");


            // 确保HTML目录存在
            // string htmlDir = Path.Combine(Directory.GetCurrentDirectory(), "DotNetTranstor", "HTML");
            // if (!Directory.Exists(htmlDir))
            // {
            // 	try
            // 	{
            // 		Directory.CreateDirectory(htmlDir);
            // 		WpfConfig.DefaultLogger.Info("[Roommanage] 创建HTML目录成功: " + htmlDir);
            // 	}
            // 	catch (Exception ex)
            // 	{
            // 		WpfConfig.DefaultLogger.Info("[Roommanage] 创建HTML目录失败: " + ex.Message);
            // 	}
            // }
            WpfConfig.JoinOrCreateTime = X19Tools.TimestampHelper.GetCurrentTimestampMilliseconds();
            WpfConfig.DefaultLogger.Info($"[RoomManage] 创建房间时间:{WpfConfig.JoinOrCreateTime}");
            WpfConfig.RoomPlayerList.Clear();
            var BuildPostGetPlayerInfo = new
            {
                entity_ids = new List<string>()
            };
            BuildPostGetPlayerInfo.entity_ids.Add(result.entity.owner_id);
            // 获取玩家信息
            var GetPlayerInfoResult = JObject.Parse(X19Http.Post("/user/query/search-by-ids",
                JsonConvert.SerializeObject(BuildPostGetPlayerInfo)));
            foreach (var PlayerInfo in GetPlayerInfoResult["entities"])
            {
                PlayerInfo["JoinRoomTime"] = WpfConfig.JoinOrCreateTime;
                WpfConfig.RoomPlayerList.Add(JObject.Parse(PlayerInfo.ToString()));
            }

            // 发送WebSocket通知
            if (WpfConfig.IsStartWebSocket)
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                {
                    type = "RoomManage",
                    status = "CreateRoom",
                    data = new
                    {
                        WpfConfig.RoomInfo.entity,
                        isOwner = true,
                        currentUserId = azf<arg>.Instance.User.Id,
                        ownerId = WpfConfig.RoomInfo.entity.owner_id
                    }
                }));
        }
        else
        {
            WpfConfig.DefaultLogger.Info($"[RoomInfo]创建房间失败,错误码: {result.code}, 错误信息: {result.message}");

            WpfConfig.RoomInfo = null;
        }

        CreateRoom_Original(result);
    }

    [OriginalMethod]
    public static void CreateRoomOriginalRequest_Original(string RoomName, string ResID, uint Visivility, uint MaxCount,
        string SaveID, string Password, Action<EntityResponse<LobbyGameRoomEntity>> RequestAction)
    {
    }

    [HookMethod("WPFLauncher.Network.Protocol.LobbyGame.age", "a", "CreateRoomOriginalRequest_Original")]
    public static void CreateRoomOriginalRequest(string RoomName, string ResID, uint Visivility, uint MaxCount,
        string SaveID, string Password, Action<EntityResponse<LobbyGameRoomEntity>> RequestAction)
    {
        WpfConfig.DefaultLogger.Info($"[RoomManage] 当前正在创建房间,房间ResID:{ResID},密码:{Password}");
        WpfConfig.Password = Password;
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
            // 错误的房间号
            return new EntityResponse<LobbyGameRoomEntity>
            {
                code = 1,
                message = "找不到房间"
            };
        var Get_Room_Info = JoinRoom_Original(roomId);
        if (Get_Room_Info.code == 0)
        {
            // 显示房间信息窗口
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 检查是否已存在相同房间信息的窗口，如果存在则关闭
                foreach (System.Windows.Window w in Application.Current.Windows)
                    if (w is RoomInfoWindow existingWindow && existingWindow.roomInfoResponse.entity.entity_id ==
                        Get_Room_Info.entity.entity_id)
                    {
                        existingWindow.Close();
                        break;
                    }

                // 创建并显示新窗口
                var window = new RoomInfoWindow(Get_Room_Info);
                window.Show();
            });
            var Get_Owner_Info = X19Http.GetPlayerInfo(Get_Room_Info.entity.owner_id);

            WpfConfig.DefaultLogger.Info("[RoomInfo]成功获取到房间信息");
            WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");


            WpfConfig.DefaultLogger.Info($"房间号: {Get_Room_Info.entity.room_name}");


            WpfConfig.DefaultLogger.Info($"是否有密码: {Get_Room_Info.entity.password}");


            WpfConfig.DefaultLogger.Info($"资源 ID: {Get_Room_Info.entity.res_id}");
            WpfConfig.DefaultLogger.Info($"房间 RoomID: {Get_Room_Info.entity.entity_id}");


            WpfConfig.DefaultLogger.Info($"最大人数: {Get_Room_Info.entity.max_count}");


            WpfConfig.DefaultLogger.Info($"是否允许保存: {Get_Room_Info.entity.allow_save}");


            var visibilityDescription = Get_Room_Info.entity.visibility.GetType()
                .GetField(Get_Room_Info.entity.visibility.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() is DescriptionAttribute descriptionAttribute
                ? descriptionAttribute.Description
                : Get_Room_Info.entity.visibility.ToString();
            WpfConfig.DefaultLogger.Info($"房间可见性: {visibilityDescription}"); // 可见性可能是一个枚举类型


            WpfConfig.DefaultLogger.Info($"房主 ID: {Get_Room_Info.entity.owner_id}");
            WpfConfig.DefaultLogger.Info("房主名称:" + Get_Owner_Info["entity"]["name"]);


            WpfConfig.DefaultLogger.Info($"版本号: {Get_Room_Info.entity.version}");


            WpfConfig.DefaultLogger.Info($"游戏状态: {Get_Room_Info.entity.game_status}");


            WpfConfig.DefaultLogger.Info($"当前玩家人数: {Get_Room_Info.entity.cur_num}");

            WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");

            WpfConfig.RoomInfo = Get_Room_Info;
            WpfConfig.RoomInfo.entity.fids.Add(azf<arg>.Instance.User.Id);
            WpfConfig.JoinOrCreateTime = X19Tools.TimestampHelper.GetCurrentTimestampMilliseconds();
            WpfConfig.DefaultLogger.Error($"[RoomManage] 加入房间时间:{WpfConfig.JoinOrCreateTime}");
            if (WpfConfig.IsStartWebSocket)
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                    { type = "RoomManage", status = "JoinRoom", data = WpfConfig.RoomInfo }));
        }
        else
        {
            WpfConfig.DefaultLogger.Error("[RoomInfo]获取房间信息失败:" + Get_Room_Info.message);
            WpfConfig.RoomInfo = null;
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
        WpfConfig.DefaultLogger.Warn("[RoomInfo]你已被房主踢出房间,正在重新加入房间");
        while (true)
        {
            var Get_RoomEnter_Info = JObject.Parse(X19Http.Post("/online-lobby-room-enter",
                JsonConvert.SerializeObject(new
                {
                    room_id = WpfConfig.RoomInfo.entity.entity_id, password = WpfConfig.Password,
                    check_visibilily = true
                })));
            if (Get_RoomEnter_Info["code"].ToObject<int>() == 0)
            {
                WpfConfig.DefaultLogger.Info("[RoomInfo]成功加入房间!");
                break;
            }

            if (Get_RoomEnter_Info["code"].ToObject<int>() == 12022)
            {
                WpfConfig.DefaultLogger.Error($"[RoomERROR]加入房间失败:{Get_RoomEnter_Info["message"]},等待0.8秒后再次加入房间");
                Thread.Sleep(200);
            }
            else
            {
                WpfConfig.DefaultLogger.Error($"[RoomERROR]加入房间失败:{Get_RoomEnter_Info["message"]}");
                SendErrToWpf(packet);
                break;
            }
        }
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
        WpfConfig.IsSelectedIP = false;
        var Get_FlagBool = GetRoomIpOriginal(config, window);
        if (Get_FlagBool)
        {
            WpfConfig.DefaultLogger.Info("[RoomInfo][IPConfig]房间IP地址:");
            WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");
            WpfConfig.DefaultLogger.Info($"IP: {config.CppGameCfg.room_info.ip}");
            WpfConfig.DefaultLogger.Info($"Port: {config.CppGameCfg.room_info.port}");
            WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");


            // 检查是否满足特定条件，如果满足则显示IP更改界面
            if (WpfConfig.IsCustomIP)
                Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                {
                    using (var changeIPForm = new ChangeIPForm(config))
                    {
                        changeIPForm.ShowDialog();
                    }
                });

            if (WpfConfig.IsStartWebSocket)
            {
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // 忽略循环
                    NullValueHandling = NullValueHandling.Ignore, // 可选：不输出 null 字段
                    Formatting = Formatting.None // 可选：减少体积
                };

                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                    { type = "RoomManage", status = "GetRoomCppGame", data = config.CppGameCfg }, settings));
            }
        }

        return Get_FlagBool;
        // }
        // else
        // // {
        // 	
        // 	WpfConfig.DefaultLogger.Info("[RoomInfo][IPConfig]获取房间IP地址失败!");
        // 	
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
    [HookMethod("WPFLauncher.ViewModel.LobbyGame.jp", "v", "GetMemberInfoOriginal")]
    private void v(EntityListResponse<LobbyRoomMemberInfoEntity> result)
    {
        if (WpfConfig.RoomInfo?.entity == null) return;

        // 获取当前房主ID
        var ownerId = WpfConfig.RoomInfo.entity.owner_id;

        // 创建新的玩家列表
        var newPlayerList = new List<string>();
        foreach (var GetMemberInfo in result.entities) newPlayerList.Add(GetMemberInfo.member_id.ToString());

        // 确保房主在列表中
        if (!newPlayerList.Contains(ownerId)) newPlayerList.Add(ownerId);

        // 确保当前用户在列表中
        var UserInList = false;
        var HaveOwner = false;
        foreach (var AllMemberInfo in result.entities)
        {
            if (AllMemberInfo.member_id == azf<arg>.Instance.User.UserID) UserInList = true;

            if (AllMemberInfo.ident == 1) HaveOwner = true;
        }

        if (!UserInList)
            result.entities.Add(new LobbyRoomMemberInfoEntity
                { member_id = azf<arg>.Instance.User.UserID, ident = HaveOwner ? 0 : 1 });
        // 获取玩家信息
        var Get_Player_Info = X19Http.GetPlayersInfo(newPlayerList);

        WpfConfig.DefaultLogger.Info("[RoomInfo]获取房间成员信息成功,以下是成员信息:");
        WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");

        var sum = 0;
        foreach (var GetMemberInfo in result.entities)
        {
            Console.ForegroundColor = (ConsoleColor)(sum % 14 + 1);
            var Get_Member_Rank = GetMemberInfo.ident == 1 ? "房主" : "成员";
            WpfConfig.DefaultLogger.Info("[RoomInfo]房间成员uid:" + GetMemberInfo.member_id +
                                         " 玩家名称:" + Get_Player_Info["entities"][sum]["name"] +
                                         " 房间内的权限情况:" + Get_Member_Rank);
            sum += 1;
        }


        WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");


        // 更新房间信息
        WpfConfig.RoomInfo.entity.fids = newPlayerList;
        WpfConfig.RoomInfo.entity.cur_num = (uint)newPlayerList.Count;

        WpfConfig.RoomPlayerList.Clear();
        foreach (var PlayerInfo in Get_Player_Info["entities"])
        {
            PlayerInfo["JoinRoomTime"] = 0;
            WpfConfig.RoomPlayerList.Add(JObject.Parse(PlayerInfo.ToString()));
        }

        // 更新RoomInfoWindow
        Application.Current.Dispatcher.Invoke(() =>
        {
            var roomInfoWindow = GetRoomInfoWindow();
            if (roomInfoWindow != null) roomInfoWindow.UpdatePlayersList(newPlayerList);
        });

        // 发送WebSocket通知
        if (WpfConfig.IsStartWebSocket)
        {
            var playerInfoList = new JArray();
            var infoIndex = 0;
            foreach (var GetMemberInfo in result.entities)
            {
                var memberRole = GetMemberInfo.ident == 1 ? "房主" : "成员";
                var memberName = Get_Player_Info["entities"][infoIndex]["name"]?.ToString() ?? "未知玩家";
                var memberAvatar = Get_Player_Info["entities"][infoIndex]["avatar_image_url"]?.ToString() ?? "";
                var memberSignature = Get_Player_Info["entities"][infoIndex]["signature"]?.ToString() ?? "";

                playerInfoList.Add(JToken.FromObject(new
                {
                    userId = GetMemberInfo.member_id.ToString(),
                    playerName = memberName,
                    avatarUrl = memberAvatar,
                    role = memberRole,
                    GetMemberInfo.ident,
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
                    roomInfo = WpfConfig.RoomInfo?.entity
                }
            }));
        }

        GetMemberInfoOriginal(result);
    }

    private static RoomInfoWindow GetRoomInfoWindow()
    {
        foreach (System.Windows.Window window in Application.Current.Windows)
            if (window is RoomInfoWindow roomInfoWindow)
                return roomInfoWindow;

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
        var result = SendJoinRoomRequest(roomId, password, statusVisibility);
        if (result.code == 0)
        {
            WpfConfig.DefaultLogger.Info("[房间信息][进入房间]房间详细信息:");
            WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");


            WpfConfig.DefaultLogger.Info("房间状态: 成功进入");
            WpfConfig.DefaultLogger.Info($"返回代码: {result.code}");
            WpfConfig.DefaultLogger.Info($"系统消息: {result.message}");
            WpfConfig.DefaultLogger.Info($"房间ID: {roomId}");
            WpfConfig.DefaultLogger.Info($"房间密码: {(string.IsNullOrEmpty(password) ? "无密码" : password)}");
            WpfConfig.DefaultLogger.Info($"隐藏状态: {(statusVisibility ? "是" : "否")}");


            WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");

            WpfConfig.Password = password;
        }
        else
        {
            WpfConfig.DefaultLogger.Info("[房间信息][进入房间]房间详细信息:");
            WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");
            WpfConfig.DefaultLogger.Info("房间状态: 失败");
            WpfConfig.DefaultLogger.Info($"返回代码: {result.code}");
            WpfConfig.DefaultLogger.Info($"系统消息: {result.message}");
            WpfConfig.DefaultLogger.Info($"房间ID: {roomId}");
            WpfConfig.DefaultLogger.Info($"房间密码: {(string.IsNullOrEmpty(password) ? "无密码" : password)}");
            WpfConfig.DefaultLogger.Info($"隐藏状态: {(statusVisibility ? "是" : "否")}");
            WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");
            Console.ResetColor();
            if (result.code == 12003) WpfConfig.JoinFailRetry++;
            if (WpfConfig.JoinFailRetry == 2)
            {
                var loadConfigResult = uz.q("警告:已连续尝试加入房间2次均为无法重复进入房间,是否退出此前进入过的房间?", "", "是", "否");
                if (loadConfigResult == MessageBoxResult.OK)
                {
                    var bGetExitRoomResult = ExitRoom.AutoExitRoom();
                    var sMessage = bGetExitRoomResult ? "成功退出房间(请重新点击加入房间)" : "退出房间失败,详细请见控制台";
                    uz.n(sMessage);
                    WpfConfig.JoinFailRetry = 0;
                }
                else
                {
                    WpfConfig.JoinFailRetry = 0;
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
        if (WpfConfig.AlwaysSaveWorld && (WpfConfig.RoomInfo.entity.allow_save ||
                                          WpfConfig.RoomInfo.entity.owner_id == azf<arg>.Instance.User.Id))
        {
            var messageBoxResult = uz.q("是否保存存档至云服务端?", "", "确定", "不保存");
            if (messageBoxResult == MessageBoxResult.OK)
            {
                var flag_End = false;
                while (true)
                {
                    if (flag_End) break;
                    var Get_Backup_Return = JObject.Parse(X19Http.Post("/online-lobby-backup/create",
                        JsonConvert.SerializeObject(new { backup_id = "1", name = "Server_Backup" })));

                    if (Get_Backup_Return["code"].ToObject<int>() != 0)
                    {
                        switch (Get_Backup_Return["code"].ToObject<int>())
                        {
                            case 12023:
                                WpfConfig.DefaultLogger.Info("[Backup]存档保存失败:" + Get_Backup_Return["message"]);
                                Thread.Sleep(2000);
                                flag_End = false;
                                break;
                            case 12026:
                                WpfConfig.DefaultLogger.Info("[Backup]存档保存失败:" + Get_Backup_Return["message"]);
                                Thread.Sleep(2000);
                                flag_End = false;
                                break;
                            case 12000:
                                WpfConfig.DefaultLogger.Info("[Backup]存档保存失败:" + Get_Backup_Return["message"]);
                                flag_End = true;
                                break;
                        }
                        //uu.n("存档保存失败!\n" + Get_Backup_Return["message"], "");
                    }
                    else
                    {
                        WpfConfig.DefaultLogger.Info("[RoomInfo][Auto_Backup]存档保存情况:");
                        WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");
                        WpfConfig.DefaultLogger.Info("存档名称:" + Get_Backup_Return["entity"]["name"]);
                        WpfConfig.DefaultLogger.Info("存档ResID:" + Get_Backup_Return["entity"]["res_id"]);
                        WpfConfig.DefaultLogger.Info("存档保存时间:" +
                                                     X19Tools.unix_timestamp_to(Get_Backup_Return["entity"]["timestamp"]
                                                         .ToObject<long>()));
                        WpfConfig.DefaultLogger.Info("存档过期时间:" +
                                                     X19Tools.unix_timestamp_to(
                                                         Get_Backup_Return["entity"]["expire_time"].ToObject<long>()));
                        WpfConfig.DefaultLogger.Info("存档大小:" + Get_Backup_Return["entity"]["size"] + "字节");
                        WpfConfig.DefaultLogger.Info("备份存档返回值:" + Get_Backup_Return["code"]);
                        WpfConfig.DefaultLogger.Info("备份存档返回消息:" + Get_Backup_Return["message"]);
                        WpfConfig.DefaultLogger.Info("-----------------------------------------------------------");
                        flag_End = true;
                    }
                }
            }

            ;
        }

        // 关闭RoomInfoWindow窗口
        Application.Current.Dispatcher.Invoke(() =>
        {
            // 检查是否已存在相同房间信息的窗口，如果存在则关闭
            foreach (System.Windows.Window w in Application.Current.Windows)
                if (w is RoomInfoWindow existingWindow && existingWindow.roomInfoResponse.entity.entity_id == roomId)
                {
                    existingWindow.Close();
                    break;
                }
        });

        if (WpfConfig.IsStartWebSocket)
            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                { type = "RoomManage", status = "Leave", data = new { roomId } }));

        WpfConfig.DefaultLogger.Info("[RoomManage]你已离开房间,房间ID:" + roomId);

        WpfConfig.RoomInfo = null;
        WpfConfig.Password = "";
        WpfConfig.RoomPlayerList.Clear();
        WpfConfig.JoinOrCreateTime = 0;
        LeftOriginal(roomId);
    }

    #endregion
}