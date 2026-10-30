using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Windows;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using Mcl.Core.NeteaseProtocol;
using Mcl.Core.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Network;
using WPFLauncher.Network.TransService;
using Application = System.Windows.Application;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

internal class ChatConnectionPacket : IMethodHook
{
    public static class BackupEventManager
    {
        // 定义备份完成事件 (参数: backupId, isSuccess)
        public static event Action<int, bool> OnBackupCompleted;

        public static void TriggerBackupCompleted(int backupId, bool isSuccess)
        {
            OnBackupCompleted?.Invoke(backupId, isSuccess);
        }
    }

    [OriginalMethod]
    public static void ProcessPacket(abx packet)
    {
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Network.abu", "b", "ProcessPacket")]
    public static void ProcessPacketHook(abx packet)
    {
        // message.a: jsonMessage
        // message.b: moduleId
        // message.c: commandId
        // message.d: Unknown
        // message.e: sequence Number
        var message = packet.a;
        var moduleId = packet.b;
        var commandId = packet.c;
        var sequenceId = packet.e;
        PluginLog.Debug("Network", 
            $"[ChatConnection] Received a message: {message} , moduleId: {moduleId} , commandId: {commandId} , sequence Number: {sequenceId}");
        var jsonMessage = new JObject();

        try
        {
            jsonMessage = JObject.Parse(message);
        }
        catch (Exception)
        {
        }

        if (string.IsNullOrEmpty(message)) return;

        ProcessPacket(packet);

        Task.Run(async () =>
        {
            // 使用并行任务处理不同类型的消息
            var processingTasks = new List<Task>();

            try
            {
                // 处理联机大厅相关事件
                if (moduleId == ModuleId.LobbyGameRoomManager)
                {
                    if (commandId == 1) processingTasks.Add(Task.Run(() => HandleGameStatus(jsonMessage)));

                    if (commandId == 2) processingTasks.Add(Task.Run(() => HandlePlayerOperation(jsonMessage)));

                    if (commandId == 3 && WpfConfig.RoomInfo != null)
                    {
                        // 自动重新加入房间由 RoomManager.ReJoinRoom(宿主被踢处理器) 在后台线程统一处理,
                        // 这里只做日志与 WebSocket 通知, 避免两条循环同时请求导致 12022 限流。
                        PluginLog.Warn("Network", "你已被房主踢出房间");
                        if (WpfConfig.EnableWebServer)
                            await Task.Run(() => WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                                { type = "ChatConnectionPacket", status = "kick", data = message })));
                    }

                    if (commandId == 4) processingTasks.Add(Task.Run(() => HandleRoomInfoChange(jsonMessage)));

                    if (commandId == 5)
                    {
                        // 注意：日志里是 "backup_id":"1"，你的原代码写成了 "back_id"，这里修正为 backup_id
                        int backupId = jsonMessage["backup_id"].ToObject<int>();
                        // success 可能是数字 1/0，也可能是 true/false，这里做个兼容判断
                        bool isSuccess = jsonMessage["success"].Type == JTokenType.Integer 
                            ? jsonMessage["success"].ToObject<int>() == 1 
                            : jsonMessage["success"].ToObject<bool>();

                        PluginLog.Debug("Network", $"联机大厅云存档备份Id: {backupId} , 是否成功: {isSuccess}");
    
                        // 触发全局事件
                        BackupEventManager.TriggerBackupCompleted(backupId, isSuccess);
                    }
                }

                else if (moduleId == ModuleId.FriendLogout)
                {
                    if (commandId == 1) processingTasks.Add(Task.Run(() => HandlePlayerStatus(jsonMessage)));
                    if (commandId == 7) processingTasks.Add(Task.Run(() => HandleFriendsList(jsonMessage)));
                }

                else if (moduleId == ModuleId.FriendStatus)
                {
                    if (commandId == 16)
                        processingTasks.Add(Task.Run(() => HandleStatusJson(jsonMessage)));

                    if (commandId == 17) processingTasks.Add(Task.Run(() => HandlePlayerComment(jsonMessage)));
                }

                else if (moduleId == ModuleId.Chat)
                {
                    if (commandId == 1)
                    {
                        if (message.Contains("player_chatver_id") && message.Contains("err"))
                            WpfConfig.LastChatResponse = message;
                        else
                            processingTasks.Add(Task.Run(() => HandlePlayerChat(jsonMessage)));
                    }
                }
                else
                {
                    processingTasks.Add(Task.Run(() =>
                    {
                        PluginLog.Debug("Network", $"接收到未分类的数据包: {message}");
                        if (WpfConfig.EnableWebServer)
                            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                                { type = "ChatConnectionPacket", data = jsonMessage }));
                    }));
                }

                // 等待所有处理任务完成
                await Task.WhenAll(processingTasks);

                // 更新接收列表
                try
                {
                    // 使用线程安全的方式添加到接收列表
                    lock (WpfConfig.ReceivedMessages)
                    {
                        WpfConfig.ReceivedMessages.Add(jsonMessage);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error("Network", $"更新接收列表失败: {ex.Message}");
                }
            }
            catch (Exception e)
            {
                await Task.Run(() => PluginLog.Error("Network", $"处理数据包时发生错误: {e}"));
            }
        });
    }

    private static void HandlePlayerChat(JObject jsonData)
    {
        //{"tgt":600015788,"msgtp":0,"uid":784912429,"words":"哈喽,我是踉跄玉帝踢足球","time":1737434926}
        //tgt = 目标ID
        //msgtp = 消息类型
        //msgtp = 0 正常消息
        //msgtp = 1 系统消息(如表情、图片等)
        //msgtp = 2 玩家加入房间
        //msgtp = 3 玩家离开房间
        //msgtp = 4 玩家被踢出房间
        //msgtp = 5 玩家权限变更(管理员、房主等)
        //uid = 发送者ID
        //words = 消息内容
        //time = 发送时间
        // 处理玩家聊天
        var tgt = jsonData["tgt"].ToObject<string>();
        var uid = jsonData["uid"].ToObject<string>();
        var words = jsonData["words"].ToObject<string>();
        var playerInfo = X19Http.GetPlayerInfo(uid);
        var playerName = playerInfo["entity"]["name"].ToObject<string>();
        var tgtPlayerInfo = X19Http.GetPlayerInfo(tgt);
        var tgtPlayerName = tgtPlayerInfo["entity"]["name"].ToObject<string>();
        PluginLog.DebugContent("Network", "Chat", $"<{playerName}(UserId: {uid})>: {words} => {tgtPlayerName}(UserId: {tgt})");
    }

    private static void HandlePlayerComment(JObject jsonData)
    {
        //{"comment":"踉跄玉帝踢足球","message":"","fid":784912429}
        // 添加好友请求
        var comment = jsonData["comment"].ToObject<string>();
        var message = jsonData["message"].ToObject<string>();
        var fid = jsonData["fid"].ToObject<string>();
        PluginLog.DebugContent("Network", "Friend request", $"comment={comment} message={message} friendId={fid}");
    }

    private static void HandleStatusJson(JObject jsonData)
    {
        //{"status_json":"{\"status\":1,\"hint\":\"\"}","uid":647998524}
        //status 1:在线 2:忙碌 3:离开 0:离线
        // 处理状态JSON
        var playerInfo = X19Http.GetPlayerInfo(jsonData["uid"].ToObject<string>());
        var playerName = playerInfo["entity"]["name"].ToObject<string>();
        if (jsonData["status_json"].ToString() != "")
        {
            var statusJson = JObject.Parse(jsonData["status_json"].ToString());
            var status = statusJson["status"].ToObject<int>();
            var hint = statusJson["hint"].ToObject<string>();
            var statusString = status == 1 ? "在线" : status == 2 ? "忙碌" : status == 3 ? "离开" : "离线";
            if (!string.IsNullOrEmpty(hint))
                try
                {
                    var hintJson = JObject.Parse(hint);
                    var gameName = hintJson["game_name"]?.ToString() ?? "";
                    var gameType = hintJson["game_type"]?.ToString() ?? "";
                    var gameId = hintJson["game_id"]?.ToString() ?? "";
                    var hostId = hintJson["host_id"]?.ToString() ?? "";
                    PluginLog.DebugContent("Network", "Player status", 
                        $"status={statusString} player={playerName} game={gameName} type={gameType} gameId={gameId} hostId={hostId}");
                }
                catch
                {
                    PluginLog.DebugContent("Network", "Player status", $"status={statusString} player={playerName} hint={hint}");
                }
            else
                PluginLog.DebugContent("Network", "Player status", $"status={statusString} player={playerName}");
        }
    }

    private static void HandleOnlinePcpe(JObject jsonData)
    {
        //{"online_pcpe":0,"status_json":"","uid":693704387}
        //online_pcpe = 0 表示在线 1 表示离线
        // 处理在线PCPE
        var onlinePcpe = jsonData["online_pcpe"].ToObject<int>();
        var stringOnlinePcpe = onlinePcpe == 1 ? "在线" : "离线";
        var uid = jsonData["uid"].ToObject<string>();
        var playerInfo = X19Http.GetPlayerInfo(uid);
        var playerName = playerInfo["entity"]["name"].ToObject<string>();
        if (jsonData["status_json"].ToString() != "")
        {
            var statusJson = JObject.Parse(jsonData["status_json"].ToString());
            var status = statusJson["status"].ToObject<string>();
            var hint = statusJson["hint"].ToObject<string>();
            PluginLog.Debug("Network", 
                $"在线PCPE: {stringOnlinePcpe} UID: {uid} xuid: {WpfConfig.SharedUidCipher.IntToHex(WpfConfig.SharedUidCipher.Encrypt(UidHelper.ToMobileUid(uint.Parse(uid))))} 玩家名: {playerName} 状态: {status} 提示: {hint}");
        }
        else
        {
            PluginLog.Debug("Network", $"在线PCPE: {stringOnlinePcpe} UID:{uid} xuid: {WpfConfig.SharedUidCipher.IntToHex(WpfConfig.SharedUidCipher.Encrypt(UidHelper.ToMobileUid(uint.Parse(uid))))} 玩家名: {playerName}");
        }
    }

    private static void HandlePlayerStatus(JObject jsonData)
    {
        //{"status":1,"sn":113864481832960,"uid":408935901,"hint":""}
        //{"status_json":"{\"status\":1,\"hint\":\"{\\\"game_name\\\":\\\"248302\\\",\\\"game_type\\\":4,\\\"game_id\\\":\\\"248302\\\",\\\"host_id\\\":\\\"615064403\\\"}\"}","uid":615064403}
        // 处理玩家状态
        var status = jsonData["status"].ToObject<int>();
        var statusString = status == 1 ? "在线" : "离线";
        var uid = jsonData["uid"].ToObject<string>();
        var hint = jsonData["hint"].ToObject<string>();
        var playerInfo = X19Http.GetPlayerInfo(uid);
        var playerName = playerInfo["entity"]["name"].ToObject<string>();
        PluginLog.Debug("Network", $"玩家状态: {statusString} UID:{uid} xuid: {WpfConfig.SharedUidCipher.IntToHex(WpfConfig.SharedUidCipher.Encrypt(UidHelper.ToMobileUid(uint.Parse(uid))))} 玩家名: {playerName} 提示: {hint}");
        FriendStatusStore.SetPresence(uid, status != 0);

        azf<arg>.Instance.ClusterList.h();
    }

    private static void HandleFriendsList(JObject jsonData)
    {
        //{"friends":[{"tLogout":1737357714,"nickname":"Da笨龙","uid":68854266},{"tLogout":1737254124,"nickname":"爱哭的戴俊豪","uid":84472828},{"tLogout":1708437593,"nickname":"必胜客炸了","uid":293717844},{"tLogout":1730607873,"nickname":"幻樱桃花剑","uid":337042223},{"tLogout":1737429945,"nickname":"EGGYLAN_","uid":378746661},...]}
        // 处理好友列表
        var friends = jsonData["friends"].ToObject<JArray>();
        foreach (JObject friend in friends)
        {
            var uid = friend["uid"].ToObject<string>();
            var nickname = friend["nickname"].ToObject<string>();
            PluginLog.Debug("Network", $"好友: {nickname} UID:{uid} xuid: {WpfConfig.SharedUidCipher.IntToHex(WpfConfig.SharedUidCipher.Encrypt(UidHelper.ToMobileUid(uint.Parse(uid))))}");
        }
    }

    private static void HandleGameStatus(JObject jsonData)
    {
        try
        {
            if (WpfConfig.RoomInfo?.entity == null)
            {
                PluginLog.Error("Network", "RoomInfo或entity为空，无法更新游戏状态");
                return;
            }

            var gameStatus = jsonData["game_status"].ToObject<int>();
            WpfConfig.RoomInfo.entity.game_status = gameStatus;
            WpfConfig.RoomInfo.entity.cur_num = (uint)(WpfConfig.RoomInfo.entity.fids?.Count ?? 0);

            var statusMessage = gameStatus == 1
                ? "游戏状态发生改变,当前可正常启动游戏(服务器在线状态)"
                : "游戏状态发生改变,当前不可正常启动游戏(服务器离线/不可用状态)";

            PluginLog.Debug("Network", $"{statusMessage}");

            // 同步 MCL 房间管理窗口的开始/进入游戏按钮
            RoomManagementWindow.NotifyRoomStateChanged();

            if (WpfConfig.EnableWebServer)
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                {
                    type = "ChatConnectionPacket",
                    status = gameStatus == 1 ? "online" : "offline",
                    data = jsonData,
                    game_status = gameStatus,
                    message = statusMessage,
                    roomInfo = WpfConfig.RoomInfo.entity,
                    currentPlayers = WpfConfig.RoomInfo.entity.fids,
                    playerCount = WpfConfig.RoomInfo.entity.cur_num
                }));
        }
        catch (Exception ex)
        {
            PluginLog.Error("Network", $"处理游戏状态时发生错误: {ex.Message}");
        }
    }

    private static void HandlePlayerOperation(JObject jsonData)
    {
        try
        {
            var operation = jsonData["op"].ToString();
            var userId = jsonData["uid"].ToObject<string>();

            if (operation == "in")
            {
                var playerInfo = X19Http.GetPlayerInfo(userId);
                playerInfo["JoinRoomTime"] = X19Tools.TimestampHelper.GetCurrentTimestampMilliseconds();
                WpfConfig.RoomPlayerList.Add(JObject.Parse(playerInfo["entity"].ToString()));
                HandlePlayerJoin(jsonData, userId);
            }
            else if (operation == "out")
            {
                var playerInfo = X19Http.GetPlayerInfo(userId);
                // 使用安全的方式查找并移除玩家
                var playerToRemove = WpfConfig.RoomPlayerList.FirstOrDefault(userProfileEntity =>
                    userProfileEntity["entity_id"].ToString() == playerInfo["entity"]["entity_id"].ToString());

                if (playerToRemove != null) WpfConfig.RoomPlayerList.Remove(playerToRemove);

                //WpfConfig.RoomPlayerList.Remove(JsonConvert.DeserializeObject<acl.UserProfileEntity>(playerInfo["entity"].ToString()));
                HandlePlayerLeave(userId, playerInfo["entity"]["name"].ToString());
            }

            // 同步 MCL 房间管理窗口的玩家列表
            RoomManagementWindow.NotifyRoomPlayersChanged();
        }
        catch (Exception ex)
        {
            PluginLog.Error("Network", $"处理玩家操作时发生错误: {ex.Message}");
        }
    }

    private static void HandlePlayerJoin(JObject jsonData, string userId)
    {
        try
        {
            if (WpfConfig.RoomInfo?.entity?.fids == null)
            {
                PluginLog.Error("Network", "RoomInfo或fids列表为空");
                return;
            }

            WpfConfig.RoomInfo.entity.fids.Add(userId);
            WpfConfig.RoomInfo.entity.cur_num = (uint)WpfConfig.RoomInfo.entity.fids.Count;

            if (WpfConfig.EnableRoomBlacklist && WpfConfig.RoomInfo.entity.owner_id == azf<arg>.Instance.User.Id)
            {
                HandleBlacklistCheck(userId);
            }
            else
            {
                var playerInfo = X19Http.GetPlayerInfo(userId);
                PluginLog.Warn("Network", $"[RoomInfo]玩家 {playerInfo["entity"]["name"]} UID: {userId} xuid: {WpfConfig.SharedUidCipher.IntToHex(WpfConfig.SharedUidCipher.Encrypt(UidHelper.ToMobileUid(uint.Parse(userId))))} 加入了房间");
                if (WpfConfig.EnableWebServer)
                    WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                    {
                        type = "ChatConnectionPacket",
                        status = "join",
                        playerName = playerInfo["entity"]["name"],
                        userId,
                        playerInfo,
                        currentPlayers = WpfConfig.RoomInfo.entity.fids,
                        playerCount = WpfConfig.RoomInfo.entity.cur_num
                    }));
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error("Network", $"处理玩家加入时发生错误: {ex.Message}");
        }
    }

    private static void HandlePlayerLeave(string userId, string playerName)
    {
        try
        {
            if (WpfConfig.RoomInfo?.entity?.fids == null)
            {
                PluginLog.Error("Network", "RoomInfo或fids列表为空");
                return;
            }

            WpfConfig.RoomInfo.entity.fids.Remove(userId);
            WpfConfig.RoomInfo.entity.cur_num = (uint)WpfConfig.RoomInfo.entity.fids.Count;

            PluginLog.Warn("Network", $"玩家 {playerName} UID: {userId} xuid: {WpfConfig.SharedUidCipher.IntToHex(WpfConfig.SharedUidCipher.Encrypt(UidHelper.ToMobileUid(uint.Parse(userId))))} 退出了房间");

            if (WpfConfig.EnableWebServer)
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                {
                    type = "ChatConnectionPacket",
                    status = "leave",
                    playerName,
                    userId,
                    currentPlayers = WpfConfig.RoomInfo.entity.fids,
                    playerCount = WpfConfig.RoomInfo.entity.cur_num
                }));
        }
        catch (Exception ex)
        {
            PluginLog.Error("Network", $"处理玩家离开时发生错误: {ex.Message}");
        }
    }

    private static void HandleBlacklistCheck(string userId)
    {
        try
        {
            var blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
            var blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");
            var blacklistFilePath_regex = Path.Combine(blacklistFolderPath, "RegexBlackList.json");

            // 确保目录和文件存在
            EnsureBlacklistFileExists(blacklistFolderPath, blacklistFilePath);

            // 读取和更新黑名单
            UpdateBlacklist(blacklistFilePath);

            UpdateBlacklist_Regex(blacklistFilePath_regex);

            // 检查玩家是否在黑名单中
            if (WpfConfig.RoomBlacklist.Contains(userId))
            {
                HandleBlacklistedPlayer(userId);
                if (WpfConfig.EnableWebServer)
                    WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                    {
                        type = "ChatConnectionPacket",
                        status = "kick",
                        userId,
                        currentPlayers = WpfConfig.RoomInfo.entity.fids,
                        playerCount = WpfConfig.RoomInfo.entity.cur_num
                    }));
            }
            else
            {
                var playerInfo = X19Http.GetPlayerInfo(userId);
                //Get_Player_Info Result:
                /*
                 * {
                        "code": 0,
                        "message": "正常返回",
                        "details": "",
                        "entities": [
                        {
                        "entity_id": "693704387",
                        "name": "HHSOOS",
                        "avatar_image_url": "https://x19.fp.ps.netease.com/file/6620fc3b5127eaa11460dcb1rHFyzwN005",
                        "frame_id": "https://x19.fp.ps.netease.com/file/64ae7241fc3e6eaa6a2de3cflOHWJ2at05",
                        "msg_background_id": "",
                        "chat_bubble_id": 0,
                        "cur_decorate": [
                        55,
                        57,
                        56,
                        51
                        ],
                        "gender": "m",
                        "register_time": 0,
                        "login_time": 0,
                        "logout_time": 0,
                        "signature": "不会的指令来找我(ˇˍˇ) 想～"
                        }
                        ],
                        "total": 1
                        }
                 */
                PluginLog.Warn("Network", $"[RoomInfo]玩家 {playerInfo["entity"]["name"]} UID: {userId} xuid: {WpfConfig.SharedUidCipher.IntToHex(WpfConfig.SharedUidCipher.Encrypt(UidHelper.ToMobileUid(uint.Parse(userId))))} 加入了房间");
                // 检查正则表达式黑名单
                CheckRegexBlacklist(userId, playerInfo["entity"]["name"].ToString(), blacklistFilePath);
                if (WpfConfig.EnableWebServer)
                    WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                    {
                        type = "ChatConnectionPacket",
                        status = "join",
                        playerName = playerInfo["entity"]["name"],
                        userId,
                        playerInfo,
                        currentPlayers = WpfConfig.RoomInfo.entity.fids,
                        playerCount = WpfConfig.RoomInfo.entity.cur_num
                    }));
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error("Network", $"处理黑名单检查时发生错误: {ex.Message}");
        }
    }

    private static void EnsureBlacklistFileExists(string folderPath, string filePath)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            PluginLog.Debug("Network", $"创建房间黑名单文件夹: {folderPath}");
        }

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "[]");
            PluginLog.Debug("Network", $"创建房间黑名单文件: {filePath}", ConsoleColor.Green);
        }
    }

    private static void UpdateBlacklist(string filePath)
    {
        try
        {
            var jsonContent = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(jsonContent))
            {
                WpfConfig.RoomBlacklist = new List<string>();
                PluginLog.Warn("Network", "房间黑名单文件为空,自动替换成空列表");
            }
            else
            {
                WpfConfig.RoomBlacklist = JArray.Parse(jsonContent).Select(x => x.ToString()).ToList();
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error("Network", $"读取房间黑名单时发生错误: {ex.Message}");
            WpfConfig.RoomBlacklist = new List<string>();
        }
    }

    private static void UpdateBlacklist_Regex(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                PluginLog.Warn("Network", "未检测到房间黑名单文件,自动创建文件");
                File.WriteAllText(filePath, "[]");
            }

            var jsonContent = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(jsonContent))
            {
                WpfConfig.RegexBlacklist = new List<string>();
                PluginLog.Warn("Network", "房间黑名单文件为空,自动替换成空列表");
            }
            else
            {
                WpfConfig.RegexBlacklist = JArray.Parse(jsonContent).Select(x => x.ToString()).ToList();
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error("Network", $"读取房间黑名单时发生错误: {ex.Message}");
            WpfConfig.RegexBlacklist = new List<string>();
        }
    }

    private static void HandleBlacklistedPlayer(string userId)
    {
        if (IsCurrentUserRoomOwner())
            KickPlayer(userId, userId, "在黑名单内");
        else
            PluginLog.Error("Network", $"[RoomInfo]玩家 {userId} 在黑名单内,但不是房主,无法踢出房间");
    }

    private static bool IsCurrentUserRoomOwner()
    {
        return WpfConfig.RoomInfo?.entity?.owner_id == azf<arg>.Instance.User.Id;
    }

    private static void KickPlayer(string userId, string playerName, string reason)
    {
        try
        {
            JObject kickResult;
            do
            {
                var requestData = JsonConvert.SerializeObject(new
                {
                    room_id = WpfConfig.RoomInfo.entity.entity_id,
                    user_id = userId
                });

                kickResult = JObject.Parse(X19Http.Post("/online-lobby-member-kick", requestData));

                if (kickResult["code"].ToObject<int>() == 0)
                {
                    PluginLog.Warn("Network", $"[RoomInfo]玩家 {playerName} {reason},已自动踢出房间");

                    if (WpfConfig.EnableWebServer)
                        WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                        {
                            type = "ChatConnectionPacket",
                            status = "kick",
                            playerName,
                            reason
                        }));
                    break;
                }

                PluginLog.Error("Network", $"[RoomInfo]玩家 {playerName} {reason},踢出失败,正在重试...");
            } while (true);
        }
        catch (Exception ex)
        {
            PluginLog.Error("Network", $"踢出玩家时发生错误: {ex.Message}");
        }
    }

    private static void CheckRegexBlacklist(string userId, string playerName, string blacklistFilePath)
    {
        if (WpfConfig.RegexBlacklist == null || !WpfConfig.RegexBlacklist.Any()) return;

        foreach (var regex in WpfConfig.RegexBlacklist)
            try
            {
                if (Regex.IsMatch(playerName, regex))
                {
                    // 添加到黑名单
                    WpfConfig.RoomBlacklist.Add(userId);
                    File.WriteAllText(blacklistFilePath, JsonConvert.SerializeObject(WpfConfig.RoomBlacklist));
                    PluginLog.Warn("Network", $"[RoomInfo]玩家 {playerName} 匹配正则表达式 {regex}，已添加到黑名单");

                    // 踢出玩家
                    if (IsCurrentUserRoomOwner()) KickPlayer(userId, playerName, "匹配正则表达式黑名单");
                    break;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("Network", $"处理正则表达式 {regex} 时发生错误: {ex.Message}");
            }
    }

    private static void HandleRoomInfoChange(JObject newRoomInfo)
    {
        try
        {
            if (WpfConfig.RoomInfo?.entity == null)
            {
                PluginLog.Error("Network", "RoomInfo或entity为空，无法更新房间信息");
                return;
            }

            // 先与旧信息对比, 再把新值写入本地实体。
            var changes = DescribeRoomInfoChanges(newRoomInfo);
            if (changes.Count > 0)
            {
                // 标题带上变化字段名, 避免同文本被 PluginLog 的重复抑制吃掉。
                var labels = string.Join("、", changes.Select(change => change.Split(':')[0].Trim()));
                PluginLog.Warn("Network", $"房间信息已更改: {labels}");
                foreach (var change in changes) PluginLog.Warn("Network", $"  {change}");
            }
            else
            {
                PluginLog.Debug("Network", "房间信息推送未包含可比较的变化");
            }

            // 更新房间信息
            UpdateRoomInfo(newRoomInfo);

            // 同步 MCL 房间管理窗口的控件与变化列表
            RoomManagementWindow.NotifyRoomInfoChanged(changes);

            // 发送WebSocket通知
            if (WpfConfig.EnableWebServer)
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                {
                    type = "ChatConnectionPacket",
                    status = "room_update",
                    roomInfo = newRoomInfo,
                    currentPlayers = WpfConfig.RoomInfo.entity.fids,
                    playerCount = WpfConfig.RoomInfo.entity.cur_num
                }));

        }
        catch (Exception ex)
        {
            PluginLog.Error("Network", $"处理房间信息变化时发生错误: {ex.Message}");
        }
    }

    /// <summary>把房间推送与当前本地房间信息逐项对比, 返回“旧值 -> 新值”的变化列表。</summary>
    private static List<string> DescribeRoomInfoChanges(JObject newRoomInfo)
    {
        var changes = new List<string>();
        var entity = WpfConfig.RoomInfo?.entity;
        if (entity == null) return changes;

        var newRoomId = newRoomInfo["entity_id"]?.ToObject<string>();
        if (!string.IsNullOrEmpty(newRoomId) && entity.entity_id != newRoomId)
            changes.Add($"房间ID: {entity.entity_id} -> {newRoomId}");

        var newOwnerId = newRoomInfo["owner_id"]?.ToObject<string>();
        if (!string.IsNullOrEmpty(newOwnerId) && entity.owner_id != newOwnerId)
            changes.Add($"房主: {DescribePlayer(entity.owner_id)} -> {DescribePlayer(newOwnerId)}");

        var newRoomName = newRoomInfo["room_name"]?.ToObject<string>();
        if (!string.IsNullOrEmpty(newRoomName) && entity.room_name != newRoomName)
            changes.Add($"房间号: {entity.room_name} -> {newRoomName}");

        if (newRoomInfo["save_size"] != null)
        {
            var newSaveSize = newRoomInfo["save_size"].ToObject<ulong>();
            if (entity.save_size != newSaveSize)
                changes.Add($"存档大小: {entity.save_size} -> {newSaveSize} bytes");
        }

        if (newRoomInfo["password"] != null)
        {
            var newPasswordStatus = newRoomInfo["password"].ToObject<int>() != 0;
            if (entity.password != newPasswordStatus)
                changes.Add($"密码保护: {YesNo(entity.password)} -> {YesNo(newPasswordStatus)}");
        }

        if (newRoomInfo["allow_save"] != null)
        {
            var newAllowSave = newRoomInfo["allow_save"].ToObject<int>() != 0;
            if (entity.allow_save != newAllowSave)
                changes.Add($"允许保存: {YesNo(entity.allow_save)} -> {YesNo(newAllowSave)}");
        }

        if (newRoomInfo["visibility"] != null)
        {
            var newVisibility = newRoomInfo["visibility"].ToObject<RoomVisibleStatus>();
            if (entity.visibility != newVisibility)
                changes.Add($"可见性: {DescribeVisibilityValue(entity.visibility)} -> {DescribeVisibilityValue(newVisibility)}");
        }

        return changes;
    }

    private static string YesNo(bool value) => value ? "是" : "否";

    private static string DescribePlayer(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return "未知";
        try
        {
            var name = X19Http.GetPlayerInfo(userId)?["entity"]?["name"]?.ToString();
            return string.IsNullOrEmpty(name) ? userId : $"{name}({userId})";
        }
        catch (Exception ex)
        {
            PluginLog.Debug("Network", $"读取玩家名称失败: {ex.Message}");
            return userId;
        }
    }

    private static string DescribeVisibilityValue(RoomVisibleStatus visibility)
    {
        try
        {
            return visibility.GetType()
                .GetField(visibility.ToString())
                ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() is DescriptionAttribute descriptionAttribute
                ? descriptionAttribute.Description
                : visibility.ToString();
        }
        catch (Exception)
        {
            return visibility.ToString();
        }
    }

    private static void UpdateRoomInfo(JObject newRoomInfo)
    {
        var entity = WpfConfig.RoomInfo.entity;
        entity.entity_id = newRoomInfo["entity_id"].ToObject<string>();
        entity.owner_id = newRoomInfo["owner_id"].ToObject<string>();
        entity.room_name = newRoomInfo["room_name"].ToObject<string>();
        entity.save_size = newRoomInfo["save_size"].ToObject<uint>();
        entity.password = newRoomInfo["password"].ToObject<int>() != 0;
        entity.allow_save = newRoomInfo["allow_save"].ToObject<int>() != 0;
        entity.visibility = newRoomInfo["visibility"].ToObject<RoomVisibleStatus>();
    }

    //发生消息拦截
    [OriginalMethod]
    public bool SendMessage(byte ServerID, object[] Data, object ifg = null, bool ErrorLog = true)
    {
        return true;
    }

    [HookMethod("WPFLauncher.Network.Service.ada", "d", "SendMessage")]
    public bool SendMessage_Hook(byte ServerID, object[] Data, object ifg = null, bool ErrorLog = true)
    {
        var ResultBool = SendMessage(ServerID, Data, ifg, ErrorLog);

        PluginLog.Debug("Network", "====================");

        PluginLog.Debug("Network", $"服务器ID: {ServerID}");
        PluginLog.Debug("Network", $"数据内容: {JsonConvert.SerializeObject(Data, Formatting.Indented)}");
        PluginLog.Debug("Network", $"接口信息: {ifg?.ToString() ?? "null"}");
        PluginLog.Error("Network", $"错误日志: {ErrorLog}");
        PluginLog.Debug("Network", $"结果: {ResultBool}");

        PluginLog.Debug("Network", "====================");

        return ResultBool;
    }

    public class ModuleId
    {
        public static readonly byte HandleLogin = 1;
        public static readonly byte Chat = 5;
        public static readonly byte FriendLogout = 6;
        public static readonly byte Unkown1 = 16;
        public static readonly byte FriendStatus = 39;
        public static readonly byte ClientStatus = 128;
        public static readonly byte NetworkService = 144;
        public static readonly byte LobbyGameRoomManager = 40;
        public static readonly byte FriendRelation = 27;
    }
}
