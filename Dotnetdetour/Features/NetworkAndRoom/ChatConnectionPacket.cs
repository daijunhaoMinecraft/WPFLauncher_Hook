using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.Model;
using Mcl.Core.Dotnetdetour.Tools;
using Mcl.Core.NeteaseProtocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Manager;
using WPFLauncher.Network;
using WPFLauncher.Network.TransService;
using WPFLauncher.Util;
using Application = System.Windows.Application;

namespace Mcl.Core.Dotnetdetour.HookList
{
    internal class ChatConnectionPacket : IMethodHook
    {
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
            string message = packet.a;
            byte moduleId = packet.b;
            byte commandId = packet.c;
            ushort sequenceId = packet.e;
            WpfConfig.DefaultLogger.Info($"[ChatConnection] Received a message: {message} , moduleId: {moduleId} , commandId: {commandId} , sequence Number: {sequenceId}");
            JObject jsonMessage = new JObject();
            
            try
            {
                jsonMessage = JObject.Parse(message);
            }
            catch (Exception) { }
            
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

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
                        if (commandId == 1)
                        {
                            processingTasks.Add(Task.Run(() => HandleGameStatus(jsonMessage)));
                        }

                        if (commandId == 2)
                        {
                            processingTasks.Add(Task.Run(() => HandlePlayerOperation(jsonMessage)));
                        }
                        
                        if (commandId == 3 && WpfConfig.RoomInfo != null)
                        {
                            WpfConfig.DefaultLogger.Warn("你已被房主踢出房间");
                            if (WpfConfig.IsStartWebSocket)
                            {
                                await Task.Run(() => WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "ChatConnectionPacket", status = "kick", data = message })));
                            }
                            WpfConfig.DefaultLogger.Info("正在重新加入房间...");
                            while (true)
                            {
                                JObject joinRoomResponse = JObject.Parse(X19Http.Post("/online-lobby-room-enter",
                                    JsonConvert.SerializeObject(new
                                    {
                                        room_id = WpfConfig.RoomInfo.entity.entity_id, password = WpfConfig.Password,
                                        check_visibilily = true
                                    })));
                                if (joinRoomResponse["code"].ToObject<int>() == 0)
                                {
                                    WpfConfig.DefaultLogger.Info("成功加入房间!");
                                    break;
                                }
                                else if (joinRoomResponse["code"].ToObject<int>() == 12022)
                                {
                                    WpfConfig.DefaultLogger.Error($"加入房间失败:{joinRoomResponse["message"]},等待0.8秒后再次加入房间");
                                    Thread.Sleep(200);
                                }
                                else
                                {
                                    WpfConfig.DefaultLogger.Info($"加入房间失败:{joinRoomResponse["message"]}");
                                    break;
                                }
                            }
                        }

                        if (commandId == 4)
                        {
                            processingTasks.Add(Task.Run(() => HandleRoomInfoChange(jsonMessage)));
                        }
                        
                        if (commandId == 5)
                        {
                            WpfConfig.DefaultLogger.Info($"联机大厅云存档备份Id: {jsonMessage["back_id"].ToString()} , 是否成功: {jsonMessage["success"].ToObject<bool>()}");
                        }
                    }
                    
                    else if (moduleId == ModuleId.FriendLogout)
                    {
                        if (commandId == 1)
                        {
                            processingTasks.Add(Task.Run(() => HandlePlayerStatus(jsonMessage)));
                        }
                        if (commandId == 7)
                        {
                            processingTasks.Add(Task.Run(() => HandleFriendsList(jsonMessage)));
                        }
                    }

                    else if (moduleId == ModuleId.FriendStatus)
                    {
                        if (commandId == 16)
                        {
                            // processingTasks.Add(Task.Run(() => HandleOnlinePcpe(jsonMessage)));
                            processingTasks.Add(Task.Run(() => HandleStatusJson(jsonMessage)));
                        }

                        if (commandId == 17)
                        {
                            processingTasks.Add(Task.Run(() => HandlePlayerComment(jsonMessage)));
                        }
                    }

                    else if (moduleId == ModuleId.Chat)
                    {
                        if (commandId == 1)
                        {
                            if (message.Contains("player_chatver_id") && message.Contains("err"))
                            {
                                WpfConfig.Get_Recv_String_ChatResult = message;
                            }
                            else
                            {
                                processingTasks.Add(Task.Run(() => HandlePlayerChat(jsonMessage)));
                            }
                        }
                    }
                    else
                    {
                        processingTasks.Add(Task.Run(() => 
                        {
                            WpfConfig.DefaultLogger.Debug($"接收到未分类的数据包: {message}");
                            if (WpfConfig.IsStartWebSocket)
                            {
                                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "ChatConnectionPacket", data = jsonMessage }));
                            }
                        }));
                    }

                    // 等待所有处理任务完成
                    await Task.WhenAll(processingTasks);

                    // 更新接收列表
                    try
                    {
                        // 使用线程安全的方式添加到接收列表
                        lock (WpfConfig.RecvList)
                        {
                            WpfConfig.RecvList.Add(jsonMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        WpfConfig.DefaultLogger.Error($"更新接收列表失败: {ex.Message}");
                    }
                }
                catch (Exception e)
                {
                    await Task.Run(() => WpfConfig.DefaultLogger.Error($"处理数据包时发生错误: {e}"));
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
            string tgt = jsonData["tgt"].ToObject<string>();
            string uid = jsonData["uid"].ToObject<string>();
            string words = jsonData["words"].ToObject<string>();
            JObject playerInfo = X19Http.GetPlayerInfo(uid);
            string playerName = playerInfo["entity"]["name"].ToObject<string>();
            JObject tgtPlayerInfo = X19Http.GetPlayerInfo(tgt);
            string tgtPlayerName = tgtPlayerInfo["entity"]["name"].ToObject<string>();
            WpfConfig.DefaultLogger.Info($"<{playerName}(UserId: {uid})>: {words} => {tgtPlayerName}(UserId: {tgt})");
        }

        private static void HandlePlayerComment(JObject jsonData)
        {
            //{"comment":"踉跄玉帝踢足球","message":"","fid":784912429}
            // 添加好友请求
            string comment = jsonData["comment"].ToObject<string>();
            string message = jsonData["message"].ToObject<string>();
            string fid = jsonData["fid"].ToObject<string>();
            WpfConfig.DefaultLogger.Info($"添加好友请求: {comment} 消息: {message} 好友ID: {fid}");
        }

        private static void HandleStatusJson(JObject jsonData)
        {
            //{"status_json":"{\"status\":1,\"hint\":\"\"}","uid":647998524}
            //status 1:在线 2:忙碌 3:离开 0:离线
            // 处理状态JSON
            JObject playerInfo = X19Http.GetPlayerInfo(jsonData["uid"].ToObject<string>());
            string playerName = playerInfo["entity"]["name"].ToObject<string>();
            if (jsonData["status_json"].ToString() != "")
            {
                JObject statusJson = JObject.Parse(jsonData["status_json"].ToString());
                int status = statusJson["status"].ToObject<int>();
                string hint = statusJson["hint"].ToObject<string>();
                string statusString = status == 1 ? "在线" : status == 2 ? "忙碌" : status == 3 ? "离开" : "离线";
                if (!string.IsNullOrEmpty(hint))
                {
                    try
                    {
                        JObject hintJson = JObject.Parse(hint);
                        string gameName = hintJson["game_name"]?.ToString() ?? "";
                        string gameType = hintJson["game_type"]?.ToString() ?? "";
                        string gameId = hintJson["game_id"]?.ToString() ?? "";
                        string hostId = hintJson["host_id"]?.ToString() ?? "";
                        WpfConfig.DefaultLogger.Info($"状态: {statusString} 玩家名: {playerName} 游戏名: {gameName} 游戏类型: {gameType} 游戏ID: {gameId} 房主ID: {hostId}");
                    }
                    catch
                    {
                        WpfConfig.DefaultLogger.Info($"状态: {statusString} 玩家名: {playerName} 提示: {hint}");
                    }
                }
                else
                {
                    WpfConfig.DefaultLogger.Info($"状态: {statusString} 玩家名: {playerName}");
                }
            }
        }

        private static void HandleOnlinePcpe(JObject jsonData)
        {
            //{"online_pcpe":0,"status_json":"","uid":693704387}
            //online_pcpe = 0 表示在线 1 表示离线
            // 处理在线PCPE
            int onlinePcpe = jsonData["online_pcpe"].ToObject<int>();
            string stringOnlinePcpe = onlinePcpe == 1 ? "在线" : "离线";
            string uid = jsonData["uid"].ToObject<string>();
            JObject playerInfo = X19Http.GetPlayerInfo(uid);
            string playerName = playerInfo["entity"]["name"].ToObject<string>();
            if (jsonData["status_json"].ToString() != "")
            {
                JObject statusJson = JObject.Parse(jsonData["status_json"].ToString());
                string status = statusJson["status"].ToObject<string>();
                string hint = statusJson["hint"].ToObject<string>();
                WpfConfig.DefaultLogger.Info($"在线PCPE: {stringOnlinePcpe} UID:{uid} 玩家名: {playerName} 状态: {status} 提示: {hint}");
            }
            else
            {
                WpfConfig.DefaultLogger.Info($"在线PCPE: {stringOnlinePcpe} UID:{uid} 玩家名: {playerName}");
            }
        }

        private static void HandlePlayerStatus(JObject jsonData)
        {
            //{"status":1,"sn":113864481832960,"uid":408935901,"hint":""}
            //{"status_json":"{\"status\":1,\"hint\":\"{\\\"game_name\\\":\\\"248302\\\",\\\"game_type\\\":4,\\\"game_id\\\":\\\"248302\\\",\\\"host_id\\\":\\\"615064403\\\"}\"}","uid":615064403}
            // 处理玩家状态
            int status = jsonData["status"].ToObject<int>();
            string statusString = status == 1 ? "在线" : "离线";
            string uid = jsonData["uid"].ToObject<string>();
            string hint = jsonData["hint"].ToObject<string>();
            JObject playerInfo = X19Http.GetPlayerInfo(uid);
            string playerName = playerInfo["entity"]["name"].ToObject<string>();
            WpfConfig.DefaultLogger.Info($"玩家状态: {statusString} UID:{uid} 玩家名: {playerName} 提示: {hint}");
            FriendStatus friendStatus = WpfConfig.ListFriendStatus.FirstOrDefault(x => x.UserId.ToString() == uid);
            if (friendStatus == null)
            {
                WpfConfig.ListFriendStatus.Add(new FriendStatus()
                {
                    Status = status,
                    UserId = uid
                });
            }
            else
            {
                friendStatus.Status = status;
                friendStatus.UserId = uid;
            }
            WPFLauncher.Common.azf<arg>.Instance.ClusterList.h();
        }

        private static void HandleFriendsList(JObject jsonData)
        {
            //{"friends":[{"tLogout":1737357714,"nickname":"Da笨龙","uid":68854266},{"tLogout":1737254124,"nickname":"爱哭的戴俊豪","uid":84472828},{"tLogout":1708437593,"nickname":"必胜客炸了","uid":293717844},{"tLogout":1730607873,"nickname":"幻樱桃花剑","uid":337042223},{"tLogout":1737429945,"nickname":"EGGYLAN_","uid":378746661},...]}
            // 处理好友列表
            JArray friends = jsonData["friends"].ToObject<JArray>();
            foreach (JObject friend in friends)
            {
                string uid = friend["uid"].ToObject<string>();
                string nickname = friend["nickname"].ToObject<string>();
                WpfConfig.DefaultLogger.Info($"好友: {nickname} UID:{uid}");
            }
        }

        private static void HandleGameStatus(JObject jsonData)
        {
            try
            {
                if (WpfConfig.RoomInfo?.entity == null)
                {
                    WpfConfig.DefaultLogger.Error("RoomInfo或entity为空，无法更新游戏状态");
                    return;
                }

                int gameStatus = jsonData["game_status"].ToObject<int>();
                WpfConfig.RoomInfo.entity.game_status = gameStatus;
                WpfConfig.RoomInfo.entity.cur_num = (uint)(WpfConfig.RoomInfo.entity.fids?.Count ?? 0);

                string statusMessage = gameStatus == 1 
                    ? "游戏状态发生改变,当前可正常启动游戏(服务器在线状态)"
                    : "游戏状态发生改变,当前不可正常启动游戏(服务器离线/不可用状态)";

                WpfConfig.DefaultLogger.Info($"{statusMessage}");

                // 更新RoomInfoWindow
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var roomInfoWindow = GetRoomInfoWindow();
                    if (roomInfoWindow != null)
                    {
                        roomInfoWindow.UpdateRoomInfoFromJson(jsonData);
                    }
                });

                if (WpfConfig.IsStartWebSocket)
                {
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
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"处理游戏状态时发生错误: {ex.Message}");
            }
        }

        private static void HandlePlayerOperation(JObject jsonData)
        {
            try
            {
                string operation = jsonData["op"].ToString();
                string userId = jsonData["uid"].ToObject<string>();
                
                if (operation == "in")
                {
                    JObject playerInfo = X19Http.GetPlayerInfo(userId);
                    playerInfo["JoinRoomTime"] = X19Tools.TimestampHelper.GetCurrentTimestampMilliseconds();
                    WpfConfig.RoomPlayerList.Add(JObject.Parse(playerInfo["entity"].ToString()));
                    HandlePlayerJoin(jsonData, userId);
                }
                else if (operation == "out")
                {
                    JObject playerInfo = X19Http.GetPlayerInfo(userId);
                    // 使用安全的方式查找并移除玩家
                    var playerToRemove = WpfConfig.RoomPlayerList.FirstOrDefault(userProfileEntity => 
                        userProfileEntity["entity_id"].ToString() == playerInfo["entity"]["entity_id"].ToString());
                    
                    if (playerToRemove != null)
                    {
                        WpfConfig.RoomPlayerList.Remove(playerToRemove);
                    }
                    
                    //WpfConfig.RoomPlayerList.Remove(JsonConvert.DeserializeObject<acl.UserProfileEntity>(playerInfo["entity"].ToString()));
                    HandlePlayerLeave(userId, playerInfo["entity"]["name"].ToString());
                }
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"处理玩家操作时发生错误: {ex.Message}");
            }
        }

        private static void HandlePlayerJoin(JObject jsonData, string userId)
        {
            try
            {
                if (WpfConfig.RoomInfo?.entity?.fids == null)
                {
                    WpfConfig.DefaultLogger.Error("RoomInfo或fids列表为空");
                    return;
                }

                WpfConfig.RoomInfo.entity.fids.Add(userId);
                WpfConfig.RoomInfo.entity.cur_num = (uint)WpfConfig.RoomInfo.entity.fids.Count;

                // 更新RoomInfoWindow
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var roomInfoWindow = GetRoomInfoWindow();
                    if (roomInfoWindow != null)
                    {
                        //roomInfoWindow.HandlePlayerJoin(userId);
                        roomInfoWindow.UpdatePlayersList(WpfConfig.RoomInfo.entity.fids);
                    }
                });

                if (WpfConfig.EnableRoomBlacklist && WpfConfig.RoomInfo.entity.owner_id == WPFLauncher.Common.azf<arg>.Instance.User.Id)
                {
                    HandleBlacklistCheck(userId);
                }
                else
                {
                    JObject playerInfo = X19Http.GetPlayerInfo(userId);
                    WpfConfig.DefaultLogger.Warn($"[RoomInfo]玩家 {playerInfo["entity"]["name"]} UID:{userId} 加入了房间");
                    if (WpfConfig.IsStartWebSocket)
                    {
                        WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
                        { 
                            type = "ChatConnectionPacket", 
                            status = "join", 
                            playerName = playerInfo["entity"]["name"],
                            userId = userId,
                            playerInfo = playerInfo,
                            currentPlayers = WpfConfig.RoomInfo.entity.fids,
                            playerCount = WpfConfig.RoomInfo.entity.cur_num
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"处理玩家加入时发生错误: {ex.Message}");
            }
        }

        private static void HandlePlayerLeave(string userId, string playerName)
        {
            try
            {
                if (WpfConfig.RoomInfo?.entity?.fids == null)
                {
                    WpfConfig.DefaultLogger.Error("RoomInfo或fids列表为空");
                    return;
                }

                WpfConfig.RoomInfo.entity.fids.Remove(userId);
                WpfConfig.RoomInfo.entity.cur_num = (uint)WpfConfig.RoomInfo.entity.fids.Count;

                // 更新RoomInfoWindow
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var roomInfoWindow = GetRoomInfoWindow();
                    if (roomInfoWindow != null)
                    {
                        roomInfoWindow.HandlePlayerLeave(userId);
                        roomInfoWindow.UpdatePlayersList(WpfConfig.RoomInfo.entity.fids);
                    }
                });

                WpfConfig.DefaultLogger.Warn($"玩家 {playerName} UID:{userId} 退出了房间");

                if (WpfConfig.IsStartWebSocket)
                {
                    WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
                    { 
                        type = "ChatConnectionPacket", 
                        status = "leave", 
                        playerName = playerName,
                        userId = userId,
                        currentPlayers = WpfConfig.RoomInfo.entity.fids,
                        playerCount = WpfConfig.RoomInfo.entity.cur_num
                    }));
                }
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"处理玩家离开时发生错误: {ex.Message}");
            }
        }

        private static void HandleBlacklistCheck(string userId)
        {
            try
            {
                string blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
                string blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");
                string blacklistFilePath_regex = Path.Combine(blacklistFolderPath, "RegexBlackList.json");

                // 确保目录和文件存在
                EnsureBlacklistFileExists(blacklistFolderPath, blacklistFilePath);

                // 读取和更新黑名单
                UpdateBlacklist(blacklistFilePath);
                
                UpdateBlacklist_Regex(blacklistFilePath_regex);

                // 检查玩家是否在黑名单中
                if (WpfConfig.RoomBlacklist.Contains(userId))
                {
                    HandleBlacklistedPlayer(userId);
                    if (WpfConfig.IsStartWebSocket)
                    {
                        WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
                        { 
                            type = "ChatConnectionPacket", 
                            status = "kick", 
                            userId = userId,
                            currentPlayers = WpfConfig.RoomInfo.entity.fids,
                            playerCount = WpfConfig.RoomInfo.entity.cur_num
                        }));
                    }
                }
                else
                {
                    JObject playerInfo = X19Http.GetPlayerInfo(userId);
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
                    WpfConfig.DefaultLogger.Warn($"[RoomInfo]玩家 {playerInfo["entity"]["name"]} UID:{userId} 加入了房间");
                    // 检查正则表达式黑名单
                    CheckRegexBlacklist(userId, playerInfo["entity"]["name"].ToString(), blacklistFilePath);
                    if (WpfConfig.IsStartWebSocket)
                    {
                        WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
                        { 
                            type = "ChatConnectionPacket", 
                            status = "join", 
                            playerName = playerInfo["entity"]["name"],
                            userId = userId,
                            playerInfo = playerInfo,
                            currentPlayers = WpfConfig.RoomInfo.entity.fids,
                            playerCount = WpfConfig.RoomInfo.entity.cur_num
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"处理黑名单检查时发生错误: {ex.Message}");
            }
        }

        private static void EnsureBlacklistFileExists(string folderPath, string filePath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                WpfConfig.DefaultLogger.Info($"创建房间黑名单文件夹: {folderPath}");
            }

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "[]");
                WpfConfig.DefaultLogger.Info($"创建房间黑名单文件: {filePath}", ConsoleColor.Green);
            }
        }

        private static void UpdateBlacklist(string filePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    WpfConfig.RoomBlacklist = new List<string>();
                    WpfConfig.DefaultLogger.Warn("房间黑名单文件为空,自动替换成空列表");
                }
                else
                {
                    WpfConfig.RoomBlacklist = JArray.Parse(jsonContent).Select(x => x.ToString()).ToList();
                }
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"读取房间黑名单时发生错误: {ex.Message}");
                WpfConfig.RoomBlacklist = new List<string>();
            }
        }
        
        private static void UpdateBlacklist_Regex(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    WpfConfig.DefaultLogger.Warn("未检测到房间黑名单文件,自动创建文件");
                    File.WriteAllText(filePath, "[]");
                }
                string jsonContent = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    WpfConfig.RegexBlacklist = new List<string>();
                    WpfConfig.DefaultLogger.Warn("房间黑名单文件为空,自动替换成空列表");
                }
                else
                {
                    WpfConfig.RegexBlacklist = JArray.Parse(jsonContent).Select(x => x.ToString()).ToList();
                }
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"读取房间黑名单时发生错误: {ex.Message}");
                WpfConfig.RegexBlacklist = new List<string>();
            }
        }
        
        private static void HandleBlacklistedPlayer(string userId)
        {
            if (IsCurrentUserRoomOwner())
            {
                KickPlayer(userId, userId, "在黑名单内");
            }
            else
            {
                WpfConfig.DefaultLogger.Error($"[RoomInfo]玩家 {userId} 在黑名单内,但不是房主,无法踢出房间");
            }
        }

        private static bool IsCurrentUserRoomOwner()
        {
            return WpfConfig.RoomInfo?.entity?.owner_id == WPFLauncher.Common.azf<arg>.Instance.User.Id;
        }

        private static void KickPlayer(string userId, string playerName, string reason)
        {
            try
            {
                JObject kickResult;
                do
                {
                    string requestData = JsonConvert.SerializeObject(new 
                    { 
                        room_id = WpfConfig.RoomInfo.entity.entity_id, 
                        user_id = userId 
                    });

                    kickResult = JObject.Parse(X19Http.Post("/online-lobby-member-kick", requestData));

                    if (kickResult["code"].ToObject<int>() == 0)
                    {
                        WpfConfig.DefaultLogger.Warn($"[RoomInfo]玩家 {playerName} {reason},已自动踢出房间");
                        
                        if (WpfConfig.IsStartWebSocket)
                        {
                            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
                            { 
                                type = "ChatConnectionPacket", 
                                status = "kick", 
                                playerName = playerName,
                                reason = reason
                            }));
                        }
                        break;
                    }
                    else
                    {
                        WpfConfig.DefaultLogger.Error($"[RoomInfo]玩家 {playerName} {reason},踢出失败,正在重试...");
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"踢出玩家时发生错误: {ex.Message}");
            }
        }

        private static void CheckRegexBlacklist(string userId, string playerName, string blacklistFilePath)
        {
            if (WpfConfig.RegexBlacklist == null || !WpfConfig.RegexBlacklist.Any())
            {
                return;
            }

            foreach (var regex in WpfConfig.RegexBlacklist)
            {
                try
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(playerName, regex))
                    {
                        // 添加到黑名单
                        WpfConfig.RoomBlacklist.Add(userId);
                        File.WriteAllText(blacklistFilePath, JsonConvert.SerializeObject(WpfConfig.RoomBlacklist));
                        WpfConfig.DefaultLogger.Warn($"[RoomInfo]玩家 {playerName} 匹配正则表达式 {regex}，已添加到黑名单");

                        // 踢出玩家
                        if (IsCurrentUserRoomOwner())
                        {
                            KickPlayer(userId, playerName, "匹配正则表达式黑名单");
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    WpfConfig.DefaultLogger.Error($"处理正则表达式 {regex} 时发生错误: {ex.Message}");
                }
            }
        }

        private static void HandleRoomInfoChange(JObject newRoomInfo)
        {
            try
            {
                if (WpfConfig.RoomInfo?.entity == null)
                {
                    WpfConfig.DefaultLogger.Error("RoomInfo或entity为空，无法更新房间信息");
                    return;
                }

                WpfConfig.DefaultLogger.Warn("房间信息已更改:");
                WpfConfig.DefaultLogger.Warn("-----------------------------------------------------------");

                // 更新RoomInfoWindow
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var roomInfoWindow = GetRoomInfoWindow();
                    if (roomInfoWindow != null)
                    {
                        roomInfoWindow.UpdateRoomInfoFromJson(newRoomInfo);
                    }
                });

                // 检查并记录所有变化
                LogRoomInfoChanges(newRoomInfo);

                // 更新房间信息
                UpdateRoomInfo(newRoomInfo);

                // 发送WebSocket通知
                if (WpfConfig.IsStartWebSocket)
                {
                    WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                    {
                        type = "ChatConnectionPacket",
                        status = "room_update",
                        roomInfo = newRoomInfo,
                        currentPlayers = WpfConfig.RoomInfo.entity.fids,
                        playerCount = WpfConfig.RoomInfo.entity.cur_num
                    }));
                }

                WpfConfig.DefaultLogger.Warn("-----------------------------------------------------------");
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"处理房间信息变化时发生错误: {ex.Message}");
            }
        }

        private static void LogRoomInfoChanges(JObject newRoomInfo)
        {
            var entity = WpfConfig.RoomInfo.entity;

            if (entity.entity_id != newRoomInfo["entity_id"].ToObject<string>())
            {
                WpfConfig.DefaultLogger.Info($"房间ID更改为: {newRoomInfo["entity_id"].ToObject<string>()}");
            }

            if (entity.owner_id != newRoomInfo["owner_id"].ToObject<string>())
            {
                LogOwnerChange(entity.owner_id, newRoomInfo["owner_id"].ToObject<string>());
            }

            if (entity.room_name != newRoomInfo["room_name"].ToObject<string>())
            {
                WpfConfig.DefaultLogger.Info($"房间名称更改为: {newRoomInfo["room_name"].ToObject<string>()}");
            }

            if (entity.save_size != newRoomInfo["save_size"].ToObject<uint>())
            {
                WpfConfig.DefaultLogger.Info($"保存大小更改为: {newRoomInfo["save_size"].ToObject<uint>()}");
            }

            bool newPasswordStatus = newRoomInfo["password"].ToObject<int>() != 0;
            if (entity.password != newPasswordStatus)
            {
                WpfConfig.DefaultLogger.Info($"密码保护更改为: {(newPasswordStatus ? "是" : "否")}");
            }

            bool newAllowSave = newRoomInfo["allow_save"].ToObject<int>() != 0;
            if (entity.allow_save != newAllowSave)
            {
                WpfConfig.DefaultLogger.Info($"允许保存更改为: {(newAllowSave ? "是" : "否")}");
            }

            var newVisibility = newRoomInfo["visibility"].ToObject<RoomVisibleStatus>();
            if (entity.visibility != newVisibility)
            {
                LogVisibilityChange(newVisibility);
            }
        }

        private static void LogOwnerChange(string oldOwnerId, string newOwnerId)
        {
            try
            {
                JObject oldOwnerInfo = X19Http.GetPlayerInfo(oldOwnerId);
                JObject newOwnerInfo = X19Http.GetPlayerInfo(newOwnerId);

                WpfConfig.DefaultLogger.Warn("当前房间内房主更改");
                WpfConfig.DefaultLogger.Warn($"原房主的UID: {oldOwnerId}");
                WpfConfig.DefaultLogger.Warn($"新房主的UID: {newOwnerId}");
                WpfConfig.DefaultLogger.Warn($"原房主的名称: {oldOwnerInfo["entity"]["name"]}");
                WpfConfig.DefaultLogger.Warn($"新房主的名称: {newOwnerInfo["entity"]["name"]}");
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"记录房主变更信息时发生错误: {ex.Message}");
            }
        }

        private static void LogVisibilityChange(RoomVisibleStatus newVisibility)
        {
            try
            {
                string visibilityDescription = newVisibility.GetType()
                    .GetField(newVisibility.ToString())
                    .GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .FirstOrDefault() is DescriptionAttribute descriptionAttribute 
                        ? descriptionAttribute.Description 
                        : newVisibility.ToString();

                WpfConfig.DefaultLogger.Info($"可见性更改为: {visibilityDescription}");
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"记录可见性变更信息时发生错误: {ex.Message}");
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

        private static RoomInfoWindow GetRoomInfoWindow()
        {
            foreach (System.Windows.Window window in Application.Current.Windows)
            {
                if (window is RoomInfoWindow roomInfoWindow)
                {
                    return roomInfoWindow;
                }
            }
            return null;
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
            bool ResultBool = SendMessage(ServerID, Data, ifg, ErrorLog);
            
            WpfConfig.DefaultLogger.Info("====================");
            
            WpfConfig.DefaultLogger.Info($"服务器ID: {ServerID}");
            WpfConfig.DefaultLogger.Info($"数据内容: {JsonConvert.SerializeObject(Data, Formatting.Indented)}");
            WpfConfig.DefaultLogger.Info($"接口信息: {(ifg?.ToString() ?? "null")}");
            WpfConfig.DefaultLogger.Info($"错误日志: {ErrorLog}");
            WpfConfig.DefaultLogger.Info($"结果: {ResultBool}");
            
            WpfConfig.DefaultLogger.Info("====================");
            
            return ResultBool;
        }
    }
}