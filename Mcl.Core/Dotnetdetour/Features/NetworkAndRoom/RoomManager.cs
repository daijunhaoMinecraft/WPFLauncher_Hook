using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
// ChangeIPWindow 仍保留历史 Features.GeneralHooks 命名空间。
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Core;
using Mcl.Core.Dotnetdetour.UI.Windows;
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
    private static string DescribePasswordState(object password)
    {
        var value = password?.ToString();
        return string.IsNullOrEmpty(value) || value == "0" ||
               string.Equals(value, bool.FalseString, StringComparison.OrdinalIgnoreCase)
            ? "无"
            : "已设置";
    }

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

            // 房间界面统一由 RoomManagementWindow(替换启动器房间窗口) 承担。

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
密码: {DescribePasswordState(result.entity.password)}
资源ID: {result.entity.res_id}
房间ID: {result.entity.entity_id}
最大人数: {result.entity.max_count}
允许保存: {result.entity.allow_save}
可见性: {visibilityDescription}
房主ID: {result.entity.owner_id}
房主 xuid: {WpfConfig.SharedUidCipher.IntToHex(WpfConfig.SharedUidCipher.Encrypt(UidHelper.ToMobileUid(uint.Parse(result.entity.owner_id))))}
房主名称: {ownerName}
存档ID: {result.entity.save_id}
存档大小: {result.entity.save_size} bytes
版本号: {result.entity.version}
游戏状态: {result.entity.game_status}
当前人数: {result.entity.cur_num}
------------------------------------------------";

            // 日志文件记录
            PluginLog.Debug("Network", roomDetails);
            
            // 控制台高亮输出
            Console.ForegroundColor = ConsoleColor.Green;
            PluginLog.Debug("Network", roomDetails);
            Console.ResetColor();

            WpfConfig.JoinOrCreateTime = X19Tools.TimestampHelper.GetCurrentTimestampMilliseconds();
            PluginLog.Debug("Network", $"[RoomManage] 创建房间时间: {WpfConfig.JoinOrCreateTime}");

            WpfConfig.RoomPlayerList.Clear();
            var BuildPostGetPlayerInfo = new { entity_ids = new List<string> { result.entity.owner_id } };
            
            var GetPlayerInfoResult = JObject.Parse(X19Http.Post("/user/query/search-by-ids", JsonConvert.SerializeObject(BuildPostGetPlayerInfo)));
            foreach (var PlayerInfo in GetPlayerInfoResult["entities"])
            {
                PlayerInfo["JoinRoomTime"] = WpfConfig.JoinOrCreateTime;
                WpfConfig.RoomPlayerList.Add(JObject.Parse(PlayerInfo.ToString()));
            }

            // 发送WebSocket通知
            if (WpfConfig.EnableWebServer)
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
            PluginLog.Error("Network", errorMsg);
            
            Console.ForegroundColor = ConsoleColor.Red;
            PluginLog.Debug("Network", errorMsg);
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
        PluginLog.Debug("Network", $"[RoomManage] 正在创建房间, ResID:{ResID}, 密码:{DescribePasswordState(Password)}");
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

        // age.c 同时被“加入房间”(aum.f) 和“房间状态轮询”(jp.z, 重置存档后每 30 秒) 使用,
        // 只有真正加入房间时才允许改写本地房间状态, 否则旧房间的迟滞轮询会清空/覆盖当前房间。
        var isJoinFlow = IsJoinFlow(roomId);
        var Get_Room_Info = JoinRoom_Original(roomId);
        if (Get_Room_Info?.code == 0 && Get_Room_Info.entity != null)
        {
            if (!isJoinFlow)
            {
                ApplyRoomInfoRefresh(Get_Room_Info);
                return Get_Room_Info;
            }

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
密码: {DescribePasswordState(Get_Room_Info.entity.password)}
资源ID: {Get_Room_Info.entity.res_id}
房间ID: {Get_Room_Info.entity.entity_id}
最大人数: {Get_Room_Info.entity.max_count}
允许保存: {Get_Room_Info.entity.allow_save}
可见性: {visibilityDescription}
房主ID: {Get_Room_Info.entity.owner_id}
房主 xuid: {WpfConfig.SharedUidCipher.IntToHex(WpfConfig.SharedUidCipher.Encrypt(UidHelper.ToMobileUid(uint.Parse(Get_Room_Info.entity.owner_id))))}
房主名称: {ownerName}
版本号: {Get_Room_Info.entity.version}
游戏状态: {Get_Room_Info.entity.game_status}
当前人数: {Get_Room_Info.entity.cur_num}
------------------------------------------------";

            PluginLog.Debug("Network", joinDetails);
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            PluginLog.Debug("Network", joinDetails);
            Console.ResetColor();

            WpfConfig.RoomInfo = Get_Room_Info;
            if (WpfConfig.RoomInfo.entity.fids == null) WpfConfig.RoomInfo.entity.fids = new List<string>();
            if (!WpfConfig.RoomInfo.entity.fids.Contains(azf<arg>.Instance.User.Id))
                WpfConfig.RoomInfo.entity.fids.Add(azf<arg>.Instance.User.Id);
            WpfConfig.JoinOrCreateTime = X19Tools.TimestampHelper.GetCurrentTimestampMilliseconds();
            
            if (WpfConfig.EnableWebServer)
            {
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "RoomManage", status = "JoinRoom", data = WpfConfig.RoomInfo }));
            }
        }
        else
        {
            var currentRoomId = WpfConfig.RoomInfo?.entity?.entity_id;
            if (!string.IsNullOrEmpty(currentRoomId) && string.Equals(currentRoomId, roomId, StringComparison.Ordinal))
            {
                // 当前所在房间查询失败: 视为房间已失效。
                PluginLog.Error("Network", $"[RoomInfo] 获取房间信息失败: {Get_Room_Info?.message}");
                WpfConfig.RoomInfo = null;
            }
            else
            {
                // 旧房间的迟滞轮询/其他房间查询, 保留本地房间状态。
                PluginLog.Debug("Network",
                    $"[RoomInfo] 非当前房间查询失败, 保留本地房间状态, roomId={roomId}, message={Get_Room_Info?.message}");
            }
        }

        return Get_Room_Info;
    }

    /// <summary>判断本次 age.c 调用是“加入房间”还是“房间信息刷新/迟滞轮询”。</summary>
    private static bool IsJoinFlow(string roomId)
    {
        var currentRoomId = WpfConfig.RoomInfo?.entity?.entity_id;
        if (string.IsNullOrEmpty(currentRoomId)) return true;
        if (string.Equals(currentRoomId, roomId, StringComparison.Ordinal)) return false;

        // 本地还记录着其他房间且宿主房间窗口仍存在时, 视为旧房间的迟滞轮询。
        return RoomWindowReplacement.GetHostRoomView() == null;
    }

    /// <summary>刷新当前房间信息: 只同步服务器返回的房间属性, 不重复执行加入房间流程。</summary>
    private static void ApplyRoomInfoRefresh(EntityResponse<LobbyGameRoomEntity> roomInfo)
    {
        var current = WpfConfig.RoomInfo?.entity;
        var entity = roomInfo.entity;
        if (current == null ||
            !string.Equals(current.entity_id, entity.entity_id, StringComparison.Ordinal))
        {
            PluginLog.Debug("Network", $"[RoomInfo] 忽略非当前房间的信息刷新, roomId={entity.entity_id}");
            return;
        }

        // age.c 的响应不带成员列表, 保留本地房间成员。
        if (entity.fids == null || entity.fids.Count == 0) entity.fids = current.fids ?? new List<string>();
        if (entity.cur_num == 0) entity.cur_num = (uint)entity.fids.Count;

        WpfConfig.RoomInfo = roomInfo;
        RoomManagementWindow.NotifyRoomInfoChanged(null);
    }

    #endregion

    #region KickEvent

    private const int RejoinRetryDelayMs = 1000;
    private const int RejoinMaxAttempts = 90;
    private static int rejoinRunning;

    [OriginalMethod]
    private new void SendErrToWpf(abx packet)
    {
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Network.ChatService.ahl", "c", "SendErrToWpf")]
    private new void ReJoinRoom(abx packet)
    {
        // 宿主在 UI 线程调用被踢处理器, 这里只负责发起后台重连, 绝不能阻塞 Dispatcher
        // (否则会触发宿主看门狗 "CheckMCLBlock!" 并卡住启动器界面)。
        if (Interlocked.Exchange(ref rejoinRunning, 1) == 1)
        {
            PluginLog.Debug("Network", "Auto rejoin already running; duplicate kick packet ignored.");
            return;
        }

        PluginLog.Warn("Network", "你已被房主踢出房间, 正在后台重新加入房间...");
        Task.Run(() => ReJoinRoomInBackground(packet));
    }

    private void ReJoinRoomInBackground(abx packet)
    {
        try
        {
            for (var attempt = 1; attempt <= RejoinMaxAttempts; attempt++)
            {
                var entity = WpfConfig.RoomInfo?.entity;
                if (entity == null || string.IsNullOrEmpty(entity.entity_id))
                {
                    PluginLog.Warn("Network", "Room info is unavailable; auto rejoin aborted.");
                    break;
                }

                var response = TryEnterRoom(entity.entity_id, WpfConfig.Password);
                if (response == null)
                {
                    Thread.Sleep(RejoinRetryDelayMs);
                    continue;
                }

                var code = response["code"]?.ToObject<int>() ?? -1;
                if (code == 0)
                {
                    PluginLog.Info("Network", $"Auto rejoin succeeded (attempt {attempt}).");
                    return;
                }

                if (code == 12022)
                {
                    if (attempt == 1 || attempt % 10 == 0)
                    {
                        PluginLog.Debug("Network",
                            $"Auto rejoin rate limited ({response["message"]}), retry {attempt}/{RejoinMaxAttempts} in {RejoinRetryDelayMs}ms.");
                    }

                    Thread.Sleep(RejoinRetryDelayMs);
                    continue;
                }

                PluginLog.Error("Network", $"Auto rejoin failed: {response["message"]}");
                break;
            }

            // 重连失败: 交回宿主原处理器(提示被踢并关闭房间窗口), 必须在 UI 线程调用。
            RunOnUiThread(() => SendErrToWpf(packet));
        }
        catch (Exception exception)
        {
            PluginLog.Error("Network", exception, "Auto rejoin failed with an exception.");
            RunOnUiThread(() => SendErrToWpf(packet));
        }
        finally
        {
            Interlocked.Exchange(ref rejoinRunning, 0);
        }
    }

    private static JObject TryEnterRoom(string roomId, string password)
    {
        try
        {
            return JObject.Parse(X19Http.Post("/online-lobby-room-enter",
                JsonConvert.SerializeObject(new
                {
                    room_id = roomId,
                    password = password ?? string.Empty,
                    check_visibilily = true
                })));
        }
        catch (Exception exception)
        {
            PluginLog.Debug("Network", $"Rejoin request failed: {exception.Message}");
            return null;
        }
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
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
        WpfConfig.HasSelectedServerAddress = false;
        var Get_FlagBool = GetRoomIpOriginal(config, window);
        if (Get_FlagBool)
        {
            string ipDetails = $@"
-----------------[房间IP信息]-----------------
IP: {config.CppGameCfg.room_info.ip}
Port: {config.CppGameCfg.room_info.port}
----------------------------------------------";
            
            PluginLog.Debug("Network", ipDetails);
            
            Console.ForegroundColor = ConsoleColor.Magenta;
            PluginLog.Debug("Network", ipDetails);
            Console.ResetColor();

            if (WpfConfig.UseCustomServerAddress)
            {
                ThreadHelperSTATask.Run(() =>
                {
                    var window = new ChangeIPWindow(config);
                    window.ShowDialog();
                });
            }

            if (WpfConfig.EnableWebServer)
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
        
        PluginLog.Debug("Network", "-----------------[房间成员列表]-----------------");

        int sum = 0;
        foreach (var GetMemberInfo in result.entities)
        {
            var Get_Member_Rank = GetMemberInfo.ident == 1 ? "房主" : "成员";
            string playerName = Get_Player_Info["entities"][sum]?["name"]?.ToString() ?? "未知";
            string lineInfo = $"UID: {GetMemberInfo.member_id} | xuid: {WpfConfig.SharedUidCipher.IntToHex(WpfConfig.SharedUidCipher.Encrypt(UidHelper.ToMobileUid(GetMemberInfo.member_id)))} | 名称: {playerName} | 权限: {Get_Member_Rank}";
            
            loggerSb.AppendLine(lineInfo);
            
            // 为每个玩家分配不同的颜色，排版更好看
            Console.ForegroundColor = (ConsoleColor)(sum % 14 + 1);
            PluginLog.Debug("Network", lineInfo);
            
            sum += 1;
        }
        Console.ResetColor();
        PluginLog.Debug("Network", "----------------------------------------------");
        loggerSb.AppendLine("----------------------------------------------");

        PluginLog.Debug("Network", loggerSb.ToString());

        // 更新房间信息
        WpfConfig.RoomInfo.entity.fids = newPlayerList;
        WpfConfig.RoomInfo.entity.cur_num = (uint)newPlayerList.Count;

        WpfConfig.RoomPlayerList.Clear();
        foreach (var PlayerInfo in Get_Player_Info["entities"])
        {
            PlayerInfo["JoinRoomTime"] = 0;
            WpfConfig.RoomPlayerList.Add(JObject.Parse(PlayerInfo.ToString()));
        }

        RoomManagementWindow.NotifyRoomPlayersChanged();

        // WebSocket 通知
        if (WpfConfig.EnableWebServer)
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
        string passText = DescribePasswordState(password);
        
        string reqDetails = $@"
-----------------[进入房间请求]-----------------
房间状态: {statusText}
返回代码: {result.code}
系统消息: {result.message}
房间ID: {roomId}
房间密码: {passText}
隐藏状态: {(statusVisibility ? "是" : "否")}
----------------------------------------------";

        PluginLog.Debug("Network", reqDetails);

        if (result.code == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            PluginLog.Debug("Network", reqDetails);
            Console.ResetColor();
            WpfConfig.Password = password;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            PluginLog.Debug("Network", reqDetails);
            Console.ResetColor();

            if (result.code == 12003) WpfConfig.JoinRetryCount++;
            if (WpfConfig.JoinRetryCount >= 2)
            {
                var loadConfigResult = uz.q("警告: 已连续尝试加入房间2次均为无法重复进入房间, 是否退出此前进入过的房间?", "", "是", "否");
                if (loadConfigResult == MessageBoxResult.OK)
                {
                    var bGetExitRoomResult = ExitRoom.AutoExitRoom();
                    var sMessage = bGetExitRoomResult ? "成功退出房间(请重新点击加入房间)" : "退出房间失败,详细请见控制台";
                    uz.n(sMessage);
                }
                WpfConfig.JoinRetryCount = 0; // 无论点是还是否都重置
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
        // 离开房间时关闭 MCL 房间管理窗口
        Application.Current.Dispatcher.Invoke(() =>
        {
            RoomManagementWindow.CloseAll();
        });

        if (WpfConfig.EnableWebServer)
        {
            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "RoomManage", status = "Leave", data = new { roomId } }));
        }

        PluginLog.Debug("Network", $"Room left by the local user, roomId={roomId}");

        WpfConfig.RoomInfo = null;
        WpfConfig.Password = "";
        WpfConfig.RoomPlayerList.Clear();
        WpfConfig.JoinOrCreateTime = 0;
        
        LeftOriginal(roomId);
    }

    #endregion
}
