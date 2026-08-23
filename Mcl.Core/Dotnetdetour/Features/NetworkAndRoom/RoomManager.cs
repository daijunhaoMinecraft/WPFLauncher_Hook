using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Core;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using Mcl.Core.NeteaseProtocol;
using Mcl.Core.Tools;
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

namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

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
                foreach (System.Windows.Window w in Application.Current.Windows)
                {
                    if (w is RoomInfoWindow existingWindow && existingWindow.roomInfoResponse.entity.entity_id == result.entity.entity_id)
                    {
                        existingWindow.Close();
                        break;
                    }
                }

                if (WpfConfig.ShowRoomManagerWindow)
                {
                    var window = new RoomInfoWindow(result);
                    window.Show();
                }
            });

            // 获取房主名称及可见性
            var ownerInfo = X19Http.GetPlayerInfo(result.entity.owner_id);
            string ownerName = ownerInfo?["entity"]?["name"]?.ToString() ?? "未知";
            var visibilityDescription = result.entity.visibility.GetType()
                .GetField(result.entity.visibility.ToString())
                ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() is DescriptionAttribute descriptionAttribute
                ? descriptionAttribute.Description : result.entity.visibility.ToString();

            // 整合日志，减少 Logger IO 压力
            string roomDetails = $@"
-------------------[建房成功]-------------------
房间号: {result.entity.room_name}
密码: {(string.IsNullOrEmpty(result.entity.password.ToString()) ? "无" : result.entity.password)}
资源ID: {result.entity.res_id}
房间ID: {result.entity.entity_id}
最大人数: {result.entity.max_count}
允许保存: {result.entity.allow_save}
可见性: {visibilityDescription}
房主ID: {result.entity.owner_id}
房主 xuid: {WpfConfig.PublicSkip32Cipher.IntToHex(WpfConfig.PublicSkip32Cipher.Encrypt(UidHelper.ToMobileUid(uint.Parse(result.entity.owner_id))))}
房主名称: {ownerName}
存档ID: {result.entity.save_id}
存档大小: {result.entity.save_size} bytes
版本号: {result.entity.version}
游戏状态: {result.entity.game_status}
当前人数: {result.entity.cur_num}
------------------------------------------------";

            // 日志文件记录
            WpfConfig.DefaultLogger.Info(roomDetails);
            
            // 控制台高亮输出
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(roomDetails);
            Console.ResetColor();

            WpfConfig.JoinOrCreateTime = X19Tools.TimestampHelper.GetCurrentTimestampMilliseconds();
            WpfConfig.DefaultLogger.Info($"[RoomManage] 创建房间时间: {WpfConfig.JoinOrCreateTime}");

            WpfConfig.RoomPlayerList.Clear();
            var BuildPostGetPlayerInfo = new { entity_ids = new List<string> { result.entity.owner_id } };
            
            var GetPlayerInfoResult = JObject.Parse(X19Http.Post("/user/query/search-by-ids", JsonConvert.SerializeObject(BuildPostGetPlayerInfo)));
            foreach (var PlayerInfo in GetPlayerInfoResult["entities"])
            {
                PlayerInfo["JoinRoomTime"] = WpfConfig.JoinOrCreateTime;
                WpfConfig.RoomPlayerList.Add(JObject.Parse(PlayerInfo.ToString()));
            }

            // 发送WebSocket通知
            if (WpfConfig.IsStartWebSocket)
            {
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
        }
        else
        {
            string errorMsg = $"[RoomInfo] 创建房间失败, 错误码: {result.code}, 错误信息: {result.message}";
            WpfConfig.DefaultLogger.Error(errorMsg);
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorMsg);
            Console.ResetColor();
            
            WpfConfig.RoomInfo = null;
        }

        CreateRoom_Original(result);
    }

    [OriginalMethod]
    public static void CreateRoomOriginalRequest_Original(string RoomName, string ResID, uint Visivility, uint MaxCount, string SaveID, string Password, Action<EntityResponse<LobbyGameRoomEntity>> RequestAction)
    {
    }

    [HookMethod("WPFLauncher.Network.Protocol.LobbyGame.age", "a", "CreateRoomOriginalRequest_Original")]
    public static void CreateRoomOriginalRequest(string RoomName, string ResID, uint Visivility, uint MaxCount, string SaveID, string Password, Action<EntityResponse<LobbyGameRoomEntity>> RequestAction)
    {
        WpfConfig.DefaultLogger.Info($"[RoomManage] 正在创建房间, ResID:{ResID}, 密码:{Password}");
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
        {
            return new EntityResponse<LobbyGameRoomEntity> { code = 1, message = "找不到房间" };
        }

        var Get_Room_Info = JoinRoom_Original(roomId);
        if (Get_Room_Info.code == 0)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (System.Windows.Window w in Application.Current.Windows)
                {
                    if (w is RoomInfoWindow existingWindow && existingWindow.roomInfoResponse.entity.entity_id == Get_Room_Info.entity.entity_id)
                    {
                        existingWindow.Close();
                        break;
                    }
                }

                if (WpfConfig.ShowRoomManagerWindow)
                {
                    var window = new RoomInfoWindow(Get_Room_Info);
                    window.Show();
                }
            });

            var Get_Owner_Info = X19Http.GetPlayerInfo(Get_Room_Info.entity.owner_id);
            string ownerName = Get_Owner_Info?["entity"]?["name"]?.ToString() ?? "未知";

            var visibilityDescription = Get_Room_Info.entity.visibility.GetType()
                .GetField(Get_Room_Info.entity.visibility.ToString())
                ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() is DescriptionAttribute descriptionAttribute
                ? descriptionAttribute.Description : Get_Room_Info.entity.visibility.ToString();

            // 整合日志输出
            string joinDetails = $@"
-------------------[加入房间]-------------------
房间号: {Get_Room_Info.entity.room_name}
密码: {Get_Room_Info.entity.password}
资源ID: {Get_Room_Info.entity.res_id}
房间ID: {Get_Room_Info.entity.entity_id}
最大人数: {Get_Room_Info.entity.max_count}
允许保存: {Get_Room_Info.entity.allow_save}
可见性: {visibilityDescription}
房主ID: {Get_Room_Info.entity.owner_id}
房主 xuid: {WpfConfig.PublicSkip32Cipher.IntToHex(WpfConfig.PublicSkip32Cipher.Encrypt(UidHelper.ToMobileUid(uint.Parse(Get_Room_Info.entity.owner_id))))}
房主名称: {ownerName}
版本号: {Get_Room_Info.entity.version}
游戏状态: {Get_Room_Info.entity.game_status}
当前人数: {Get_Room_Info.entity.cur_num}
------------------------------------------------";

            WpfConfig.DefaultLogger.Info(joinDetails);
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(joinDetails);
            Console.ResetColor();

            WpfConfig.RoomInfo = Get_Room_Info;
            WpfConfig.RoomInfo.entity.fids.Add(azf<arg>.Instance.User.Id);
            WpfConfig.JoinOrCreateTime = X19Tools.TimestampHelper.GetCurrentTimestampMilliseconds();
            
            if (WpfConfig.IsStartWebSocket)
            {
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "RoomManage", status = "JoinRoom", data = WpfConfig.RoomInfo }));
            }
        }
        else
        {
            string errMsg = $"[RoomInfo] 获取房间信息失败: {Get_Room_Info.message}";
            WpfConfig.DefaultLogger.Error(errMsg);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errMsg);
            Console.ResetColor();
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
        string warnMsg = "[RoomInfo] 你已被房主踢出房间, 正在重新加入房间...";
        WpfConfig.DefaultLogger.Warn(warnMsg);
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(warnMsg);
        Console.ResetColor();

        while (true)
        {
            var Get_RoomEnter_Info = JObject.Parse(X19Http.Post("/online-lobby-room-enter",
                JsonConvert.SerializeObject(new
                {
                    room_id = WpfConfig.RoomInfo.entity.entity_id, 
                    password = WpfConfig.Password,
                    check_visibilily = true
                })));

            int code = Get_RoomEnter_Info["code"]?.ToObject<int>() ?? -1;

            if (code == 0)
            {
                WpfConfig.DefaultLogger.Info("[RoomInfo] 成功重新加入房间!");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[RoomInfo] 成功重新加入房间!");
                Console.ResetColor();
                break;
            }

            if (code == 12022)
            {
                string retryMsg = $"[RoomERROR] 加入失败: {Get_RoomEnter_Info["message"]}, 等待0.8秒后重试...";
                WpfConfig.DefaultLogger.Error(retryMsg);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(retryMsg);
                Console.ResetColor();
                Thread.Sleep(800); // 修复了原代码里注释写0.8秒实际休眠200ms的逻辑（或者可保持200）
            }
            else
            {
                string errMsg = $"[RoomERROR] 最终加入房间失败: {Get_RoomEnter_Info["message"]}";
                WpfConfig.DefaultLogger.Error(errMsg);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errMsg);
                Console.ResetColor();
                SendErrToWpf(packet);
                break;
            }
        }
    }

    #endregion

    #region GetRoomIP

    [OriginalMethod]
    public bool GetRoomIpOriginal(akv config, BaseWindow window)
    {
        return true;
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Manager.Game.aum", "d", "GetRoomIpOriginal")]
    public bool GetRoomIp(akv config, BaseWindow window)
    {
        WpfConfig.IsSelectedIP = false;
        var Get_FlagBool = GetRoomIpOriginal(config, window);
        if (Get_FlagBool)
        {
            string ipDetails = $@"
-----------------[房间IP信息]-----------------
IP: {config.CppGameCfg.room_info.ip}
Port: {config.CppGameCfg.room_info.port}
----------------------------------------------";
            
            WpfConfig.DefaultLogger.Info(ipDetails);
            
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(ipDetails);
            Console.ResetColor();

            if (WpfConfig.IsCustomIP)
            {
                ThreadHelperSTATask.Run(() =>
                {
                    var window = new ChangeIPWindow(config);
                    window.ShowDialog();
                });
            }

            if (WpfConfig.IsStartWebSocket)
            {
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.None
                };

                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                    { type = "RoomManage", status = "GetRoomCppGame", data = config.CppGameCfg }, settings));
            }
        }

        return Get_FlagBool;
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

        var ownerId = WpfConfig.RoomInfo.entity.owner_id;
        var newPlayerList = new List<string>();
        
        foreach (var GetMemberInfo in result.entities) 
            newPlayerList.Add(GetMemberInfo.member_id.ToString());

        if (!newPlayerList.Contains(ownerId)) 
            newPlayerList.Add(ownerId);

        var UserInList = false;
        var HaveOwner = false;
        foreach (var AllMemberInfo in result.entities)
        {
            if (AllMemberInfo.member_id == azf<arg>.Instance.User.UserID) UserInList = true;
            if (AllMemberInfo.ident == 1) HaveOwner = true;
        }

        if (!UserInList)
            result.entities.Add(new LobbyRoomMemberInfoEntity { member_id = azf<arg>.Instance.User.UserID, ident = HaveOwner ? 0 : 1 });
            
        var Get_Player_Info = X19Http.GetPlayersInfo(newPlayerList);

        // 合并日志输出并分配颜色
        StringBuilder loggerSb = new StringBuilder();
        loggerSb.AppendLine("-----------------[房间成员列表]-----------------");
        
        Console.WriteLine("-----------------[房间成员列表]-----------------");

        int sum = 0;
        foreach (var GetMemberInfo in result.entities)
        {
            var Get_Member_Rank = GetMemberInfo.ident == 1 ? "房主" : "成员";
            string playerName = Get_Player_Info["entities"][sum]?["name"]?.ToString() ?? "未知";
            string lineInfo = $"UID: {GetMemberInfo.member_id} | xuid: {WpfConfig.PublicSkip32Cipher.IntToHex(WpfConfig.PublicSkip32Cipher.Encrypt(UidHelper.ToMobileUid(GetMemberInfo.member_id)))} | 名称: {playerName} | 权限: {Get_Member_Rank}";
            
            loggerSb.AppendLine(lineInfo);
            
            // 为每个玩家分配不同的颜色，排版更好看
            Console.ForegroundColor = (ConsoleColor)(sum % 14 + 1);
            Console.WriteLine(lineInfo);
            
            sum += 1;
        }
        Console.ResetColor();
        Console.WriteLine("----------------------------------------------");
        loggerSb.AppendLine("----------------------------------------------");

        WpfConfig.DefaultLogger.Info(loggerSb.ToString());

        // 更新房间信息
        WpfConfig.RoomInfo.entity.fids = newPlayerList;
        WpfConfig.RoomInfo.entity.cur_num = (uint)newPlayerList.Count;

        WpfConfig.RoomPlayerList.Clear();
        foreach (var PlayerInfo in Get_Player_Info["entities"])
        {
            PlayerInfo["JoinRoomTime"] = 0;
            WpfConfig.RoomPlayerList.Add(JObject.Parse(PlayerInfo.ToString()));
        }

        // 更新窗口
        Application.Current.Dispatcher.Invoke(() =>
        {
            var roomInfoWindow = GetRoomInfoWindow();
            if (roomInfoWindow != null) roomInfoWindow.UpdatePlayersList(newPlayerList);
        });

        // WebSocket 通知
        if (WpfConfig.IsStartWebSocket)
        {
            var playerInfoList = new JArray();
            int infoIndex = 0;
            foreach (var GetMemberInfo in result.entities)
            {
                var memberRole = GetMemberInfo.ident == 1 ? "房主" : "成员";
                var memberName = Get_Player_Info["entities"][infoIndex]?["name"]?.ToString() ?? "未知玩家";
                var memberAvatar = Get_Player_Info["entities"][infoIndex]?["avatar_image_url"]?.ToString() ?? "";
                var memberSignature = Get_Player_Info["entities"][infoIndex]?["signature"]?.ToString() ?? "";

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
            if (window is RoomInfoWindow roomInfoWindow) return roomInfoWindow;
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
        
        string statusText = result.code == 0 ? "成功进入" : "进入失败";
        string passText = string.IsNullOrEmpty(password) ? "无密码" : password;
        
        string reqDetails = $@"
-----------------[进入房间请求]-----------------
房间状态: {statusText}
返回代码: {result.code}
系统消息: {result.message}
房间ID: {roomId}
房间密码: {passText}
隐藏状态: {(statusVisibility ? "是" : "否")}
----------------------------------------------";

        WpfConfig.DefaultLogger.Info(reqDetails);

        if (result.code == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(reqDetails);
            Console.ResetColor();
            WpfConfig.Password = password;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(reqDetails);
            Console.ResetColor();

            if (result.code == 12003) WpfConfig.JoinFailRetry++;
            if (WpfConfig.JoinFailRetry >= 2)
            {
                var loadConfigResult = uz.q("警告: 已连续尝试加入房间2次均为无法重复进入房间, 是否退出此前进入过的房间?", "", "是", "否");
                if (loadConfigResult == MessageBoxResult.OK)
                {
                    var bGetExitRoomResult = ExitRoom.AutoExitRoom();
                    var sMessage = bGetExitRoomResult ? "成功退出房间(请重新点击加入房间)" : "退出房间失败,详细请见控制台";
                    uz.n(sMessage);
                }
                WpfConfig.JoinFailRetry = 0; // 无论点是还是否都重置
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
        // 关闭RoomInfoWindow窗口
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (System.Windows.Window w in Application.Current.Windows)
            {
                if (w is RoomInfoWindow existingWindow && existingWindow.roomInfoResponse.entity.entity_id == roomId)
                {
                    existingWindow.Close();
                    break;
                }
            }
        });

        if (WpfConfig.IsStartWebSocket)
        {
            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "RoomManage", status = "Leave", data = new { roomId } }));
        }

        string leaveMsg = $"[RoomManage] 你已离开房间, 房间ID: {roomId}";
        WpfConfig.DefaultLogger.Info(leaveMsg);
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(leaveMsg);
        Console.ResetColor();

        WpfConfig.RoomInfo = null;
        WpfConfig.Password = "";
        WpfConfig.RoomPlayerList.Clear();
        WpfConfig.JoinOrCreateTime = 0;
        
        LeftOriginal(roomId);
    }

    #endregion
}