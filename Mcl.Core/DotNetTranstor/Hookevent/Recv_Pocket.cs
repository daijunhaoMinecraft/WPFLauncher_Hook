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
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using System.Windows;
using Mcl.Core.Network;
using Mcl.Core.Network.Interface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Login;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Network;
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol;
using WPFLauncher.Network.TransService;
using WPFLauncher.Util.AES;
using MessageBox = System.Windows.MessageBox;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using Application = System.Windows.Application;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
    internal class Recv_Pocket : IMethodHook
    {
        private static readonly object _lockObject = new object();
        private static readonly string LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "recv_pocket.log");

        private static void LogDebug(string message, ConsoleColor color = ConsoleColor.White)
        {
            try
            {
                lock (_lockObject)
                {
                    Console.ForegroundColor = color;
                    string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                    Console.WriteLine(logMessage);
                    
                    // 确保日志目录存在
                    string logDir = Path.GetDirectoryName(LogFilePath);
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    
                    // 写入日志文件
                    File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] 写入日志失败: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        [OriginalMethod]
        public static void Pocket_Info(abx hqx)
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.Network.abu", "b", "Pocket_Info")]
        public static void b(abx hqx)
        {
            //Console.WriteLine($"[Recv_Pocket]:a:{hqx.a},b:{hqx.b},c:{hqx.c},d:{hqx.d}");
            if (hqx == null)
            {
                return;
            }

            // 启动异步任务处理数据包
            Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(hqx.a))
                    {
                        return;
                    }

                    if (hqx.a == "[]")
                    {
                        if (Path_Bool.RoomInfo != null)
                        {
                            await Task.Run(() => LogDebug("你已被房主踢出房间", ConsoleColor.Red));
                            if (Path_Bool.IsStartWebSocket)
                            {
                                await Task.Run(() => WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Recv_Pocket", status = "kick", data = "[]" })));
                            }
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
                                    break;
                                }
                            }
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        return;
                    }

                    JObject Get_Json_Recv;
                    try
                    {
                        Get_Json_Recv = JObject.Parse(hqx.a);
                    }
                    catch (JsonReaderException)
                    {
                        return;
                    }

                    // 使用并行任务处理不同类型的消息
                    var processingTasks = new List<Task>();

                    // 处理游戏状态变化
                    if (((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("game_status"))
                    {
                        processingTasks.Add(Task.Run(() => HandleGameStatus(Get_Json_Recv)));
                    }
                    // 处理玩家进出
                    else if (((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("op"))
                    {
                        processingTasks.Add(Task.Run(() => HandlePlayerOperation(Get_Json_Recv)));
                    }
                    // 处理房间信息变化
                    else if (((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("entity_id") || ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("slogan"))
                    {
                        processingTasks.Add(Task.Run(() => HandleRoomInfoChange(Get_Json_Recv)));
                    }
                    else if (((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("friends"))
                    {
                        processingTasks.Add(Task.Run(() => HandleFriendsList(Get_Json_Recv)));
                    }
                    else if (((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("status") && ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("sn") && 
                            ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("uid") && ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("hint"))
                    {
                        processingTasks.Add(Task.Run(() => HandlePlayerStatus(Get_Json_Recv)));
                    }
                    else if (((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("extra_data") && ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("red_dot_type"))
                    {
                        processingTasks.Add(Task.Run(() => HandleRedDot(Get_Json_Recv)));
                    }
                    else if (((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("online_pcpe") && ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("status_json") && 
                            ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("uid"))
                    {
                        processingTasks.Add(Task.Run(() => HandleOnlinePcpe(Get_Json_Recv)));
                    }
                    else if (((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("status_json") && ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("uid"))
                    {
                        processingTasks.Add(Task.Run(() => HandleStatusJson(Get_Json_Recv)));
                    }
                    else if (((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("comment") && ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("message") && 
                            ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("fid"))
                    {
                        processingTasks.Add(Task.Run(() => HandlePlayerComment(Get_Json_Recv)));
                    }
                    else if (((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("tgt") && ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("msgtp") && 
                            ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("uid") && ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("words") && 
                            ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("time"))
                    {
                        processingTasks.Add(Task.Run(() => HandlePlayerChat(Get_Json_Recv)));
                    }
                    else
                    {
                        processingTasks.Add(Task.Run(() => 
                        {
                            LogDebug($"接收到未分类的数据包: {hqx.a}", ConsoleColor.Magenta);
                            if (Path_Bool.IsStartWebSocket)
                            {
                                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Recv_Pocket", data = Get_Json_Recv }));
                            }
                        }));
                    }

                    // 等待所有处理任务完成
                    await Task.WhenAll(processingTasks);

                    // 更新接收列表
                    try
                    {
                        // 使用线程安全的方式添加到接收列表
                        lock (Path_Bool.RecvList)
                        {
                            Path_Bool.RecvList.Add(Get_Json_Recv);
                        }
                        
                        if (((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("player_chatver_id") || ((IDictionary<string, JToken>)Get_Json_Recv).ContainsKey("err"))
                        {
                            Path_Bool.Get_Recv_String_ChatResult = hqx.a;
                        }
                    }
                    catch (Exception ex)
                    {
                        await Task.Run(() => LogDebug($"更新接收列表失败: {ex.Message}", ConsoleColor.Red));
                    }

                    await Task.Run(() => Pocket_Info(hqx));
                }
                catch (Exception e)
                {
                    await Task.Run(() => LogDebug($"处理数据包时发生错误: {e.Message}\n堆栈跟踪: {e.StackTrace}", ConsoleColor.Red));
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
            string msgtp = jsonData["msgtp"].ToObject<string>();
            switch (msgtp)
            {
                case "0":
                    msgtp = "正常消息";
                    break;
                case "1":
                    msgtp = "系统消息";
                    break;
                case "2":
                    msgtp = "玩家加入房间";
                    break;
                case "3":
                    msgtp = "玩家离开房间";
                    break;
                case "4":
                    msgtp = "玩家被踢出房间";
                    break;
                case "5":
                    msgtp = "玩家权限变更";
                    break;
            }
            string uid = jsonData["uid"].ToObject<string>();
            string words = jsonData["words"].ToObject<string>();
            JObject playerInfo = X19Http.Get_Player_Info(uid);
            string playerName = playerInfo["entity"]["name"].ToObject<string>();
            JObject tgtPlayerInfo = X19Http.Get_Player_Info(tgt);
            string tgtPlayerName = tgtPlayerInfo["entity"]["name"].ToObject<string>();
            string time = X19Http.unix_timestamp_to(jsonData["time"].ToObject<long>());
            LogDebug($"玩家聊天内容: {words} 目标: {tgt} 类型: {msgtp} UID: {uid} 玩家名: {playerName} 目标玩家名: {tgtPlayerName} 时间: {time}", ConsoleColor.Green);
        }

        private static void HandlePlayerComment(JObject jsonData)
        {
            //{"comment":"踉跄玉帝踢足球","message":"","fid":784912429}
            // 添加好友请求
            string comment = jsonData["comment"].ToObject<string>();
            string message = jsonData["message"].ToObject<string>();
            string fid = jsonData["fid"].ToObject<string>();
            LogDebug($"添加好友请求: {comment} 消息: {message} 好友ID: {fid}", ConsoleColor.Green);
        }

        private static void HandleStatusJson(JObject jsonData)
        {
            //{"status_json":"{\"status\":1,\"hint\":\"\"}","uid":647998524}
            //status 1:在线 2:忙碌 3:离开 0:离线
            // 处理状态JSON
            JObject playerInfo = X19Http.Get_Player_Info(jsonData["uid"].ToObject<string>());
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
                        LogDebug($"状态: {statusString} 玩家名: {playerName} 游戏名: {gameName} 游戏类型: {gameType} 游戏ID: {gameId} 房主ID: {hostId}", status == 1 ? ConsoleColor.Green : ConsoleColor.Red);
                    }
                    catch
                    {
                        LogDebug($"状态: {statusString} 玩家名: {playerName} 提示: {hint}", status == 1 ? ConsoleColor.Green : ConsoleColor.Red);
                    }
                }
                else
                {
                    LogDebug($"状态: {statusString} 玩家名: {playerName}", status == 1 ? ConsoleColor.Green : ConsoleColor.Red);
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
            JObject playerInfo = X19Http.Get_Player_Info(uid);
            string playerName = playerInfo["entity"]["name"].ToObject<string>();
            if (jsonData["status_json"].ToString() != "")
            {
                JObject statusJson = JObject.Parse(jsonData["status_json"].ToString());
                string status = statusJson["status"].ToObject<string>();
                string hint = statusJson["hint"].ToObject<string>();
                LogDebug($"在线PCPE: {stringOnlinePcpe} UID:{uid} 玩家名: {playerName} 状态: {status} 提示: {hint}", onlinePcpe == 1 ? ConsoleColor.Green : ConsoleColor.Red);
            }
            else
            {
                LogDebug($"在线PCPE: {stringOnlinePcpe} UID:{uid} 玩家名: {playerName}", onlinePcpe == 1 ? ConsoleColor.Green : ConsoleColor.Red);
            }
        }

        private static void HandleRedDot(JObject jsonData)
        {
            //{"extra_data":{"activity_name":"S3","week_num":2,"quest_id":"q100_login_1"},"red_dot_type":1}
            // 处理活动
            string activityName = jsonData["extra_data"]["activity_name"].ToObject<string>();
            int weekNum = jsonData["extra_data"]["week_num"].ToObject<int>();
            string questId = jsonData["extra_data"]["quest_id"].ToObject<string>();
            LogDebug($"活动名称: {activityName} 周数: {weekNum} 任务ID: {questId}", ConsoleColor.Green);
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
            JObject playerInfo = X19Http.Get_Player_Info(uid);
            string playerName = playerInfo["entity"]["name"].ToObject<string>();
            LogDebug($"玩家状态: {statusString} UID:{uid} 玩家名: {playerName} 提示: {hint}", status == 1 ? ConsoleColor.Green : ConsoleColor.Red);
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
                LogDebug($"好友: {nickname} UID:{uid}", ConsoleColor.Green);
            }
        }

        private static void HandleGameStatus(JObject jsonData)
        {
            try
            {
                if (Path_Bool.RoomInfo?.entity == null)
                {
                    LogDebug("RoomInfo或entity为空，无法更新游戏状态", ConsoleColor.Red);
                    return;
                }

                int gameStatus = jsonData["game_status"].ToObject<int>();
                Path_Bool.RoomInfo.entity.game_status = gameStatus;
                Path_Bool.RoomInfo.entity.cur_num = (uint)(Path_Bool.RoomInfo.entity.fids?.Count ?? 0);

                string statusMessage = gameStatus == 1 
                    ? "游戏状态发生改变,当前可正常启动游戏(服务器在线状态)"
                    : "游戏状态发生改变,当前不可正常启动游戏(服务器离线/不可用状态)";

                LogDebug($"[RoomInfo]{statusMessage}", gameStatus == 1 ? ConsoleColor.Green : ConsoleColor.Red);

                // 更新RoomInfoWindow
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var roomInfoWindow = GetRoomInfoWindow();
                    if (roomInfoWindow != null)
                    {
                        roomInfoWindow.UpdateRoomInfoFromJson(jsonData);
                    }
                });

                if (Path_Bool.IsStartWebSocket)
                {
                    WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
                    { 
                        type = "Recv_Pocket", 
                        status = gameStatus == 1 ? "online" : "offline", 
                        data = jsonData,
                        game_status = gameStatus,
                        message = statusMessage,
                        roomInfo = Path_Bool.RoomInfo.entity,
                        currentPlayers = Path_Bool.RoomInfo.entity.fids,
                        playerCount = Path_Bool.RoomInfo.entity.cur_num
                    }));
                }
            }
            catch (Exception ex)
            {
                LogDebug($"处理游戏状态时发生错误: {ex.Message}", ConsoleColor.Red);
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
                    JObject playerInfo = X19Http.Get_Player_Info(userId);
                    playerInfo["JoinRoomTime"] = X19Http.TimestampHelper.GetCurrentTimestampMilliseconds();
                    Path_Bool.RoomPlayerList.Add(JObject.Parse(playerInfo["entity"].ToString()));
                    HandlePlayerJoin(jsonData, userId);
                }
                else if (operation == "out")
                {
                    JObject playerInfo = X19Http.Get_Player_Info(userId);
                    // 使用安全的方式查找并移除玩家
                    var playerToRemove = Path_Bool.RoomPlayerList.FirstOrDefault(userProfileEntity => 
                        userProfileEntity["entity_id"].ToString() == playerInfo["entity"]["entity_id"].ToString());
                    
                    if (playerToRemove != null)
                    {
                        Path_Bool.RoomPlayerList.Remove(playerToRemove);
                    }
                    
                    //Path_Bool.RoomPlayerList.Remove(JsonConvert.DeserializeObject<acl.UserProfileEntity>(playerInfo["entity"].ToString()));
                    HandlePlayerLeave(userId, playerInfo["entity"]["name"].ToString());
                }
            }
            catch (Exception ex)
            {
                LogDebug($"处理玩家操作时发生错误: {ex.Message}", ConsoleColor.Red);
            }
        }

        private static void HandlePlayerJoin(JObject jsonData, string userId)
        {
            try
            {
                if (Path_Bool.RoomInfo?.entity?.fids == null)
                {
                    LogDebug("RoomInfo或fids列表为空", ConsoleColor.Red);
                    return;
                }

                Path_Bool.RoomInfo.entity.fids.Add(userId);
                Path_Bool.RoomInfo.entity.cur_num = (uint)Path_Bool.RoomInfo.entity.fids.Count;

                // 更新RoomInfoWindow
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var roomInfoWindow = GetRoomInfoWindow();
                    if (roomInfoWindow != null)
                    {
                        //roomInfoWindow.HandlePlayerJoin(userId);
                        roomInfoWindow.UpdatePlayersList(Path_Bool.RoomInfo.entity.fids);
                    }
                });

                if (Path_Bool.EnableRoomBlacklist && Path_Bool.RoomInfo.entity.owner_id == aze<arg>.Instance.User.Id)
                {
                    HandleBlacklistCheck(userId);
                }
                else
                {
                    JObject playerInfo = X19Http.Get_Player_Info(userId);
                    LogDebug($"[RoomInfo]玩家 {playerInfo["entity"]["name"]} UID:{userId} 加入了房间", ConsoleColor.Blue);
                    if (Path_Bool.IsStartWebSocket)
                    {
                        WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
                        { 
                            type = "Recv_Pocket", 
                            status = "join", 
                            playerName = playerInfo["entity"]["name"],
                            userId = userId,
                            playerInfo = playerInfo,
                            currentPlayers = Path_Bool.RoomInfo.entity.fids,
                            playerCount = Path_Bool.RoomInfo.entity.cur_num
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"处理玩家加入时发生错误: {ex.Message}", ConsoleColor.Red);
            }
        }

        private static void HandlePlayerLeave(string userId, string playerName)
        {
            try
            {
                if (Path_Bool.RoomInfo?.entity?.fids == null)
                {
                    LogDebug("RoomInfo或fids列表为空", ConsoleColor.Red);
                    return;
                }

                Path_Bool.RoomInfo.entity.fids.Remove(userId);
                Path_Bool.RoomInfo.entity.cur_num = (uint)Path_Bool.RoomInfo.entity.fids.Count;

                // 更新RoomInfoWindow
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var roomInfoWindow = GetRoomInfoWindow();
                    if (roomInfoWindow != null)
                    {
                        roomInfoWindow.HandlePlayerLeave(userId);
                        roomInfoWindow.UpdatePlayersList(Path_Bool.RoomInfo.entity.fids);
                    }
                });

                LogDebug($"[RoomInfo]玩家 {playerName} UID:{userId} 退出了房间", ConsoleColor.Yellow);

                if (Path_Bool.IsStartWebSocket)
                {
                    WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
                    { 
                        type = "Recv_Pocket", 
                        status = "leave", 
                        playerName = playerName,
                        userId = userId,
                        currentPlayers = Path_Bool.RoomInfo.entity.fids,
                        playerCount = Path_Bool.RoomInfo.entity.cur_num
                    }));
                }
            }
            catch (Exception ex)
            {
                LogDebug($"处理玩家离开时发生错误: {ex.Message}", ConsoleColor.Red);
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
                if (Path_Bool.RoomBlacklist.Contains(userId))
                {
                    HandleBlacklistedPlayer(userId);
                    if (Path_Bool.IsStartWebSocket)
                    {
                        WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
                        { 
                            type = "Recv_Pocket", 
                            status = "kick", 
                            userId = userId,
                            currentPlayers = Path_Bool.RoomInfo.entity.fids,
                            playerCount = Path_Bool.RoomInfo.entity.cur_num
                        }));
                    }
                }
                else
                {
                    JObject playerInfo = X19Http.Get_Player_Info(userId);
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
                    LogDebug($"[RoomInfo]玩家 {playerInfo["entity"]["name"]} UID:{userId} 加入了房间", ConsoleColor.Blue);
                    // 检查正则表达式黑名单
                    CheckRegexBlacklist(userId, playerInfo["entity"]["name"].ToString(), blacklistFilePath);
                    if (Path_Bool.IsStartWebSocket)
                    {
                        WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
                        { 
                            type = "Recv_Pocket", 
                            status = "join", 
                            playerName = playerInfo["entity"]["name"],
                            userId = userId,
                            playerInfo = playerInfo,
                            currentPlayers = Path_Bool.RoomInfo.entity.fids,
                            playerCount = Path_Bool.RoomInfo.entity.cur_num
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"处理黑名单检查时发生错误: {ex.Message}", ConsoleColor.Red);
            }
        }

        private static void EnsureBlacklistFileExists(string folderPath, string filePath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                LogDebug($"创建房间黑名单文件夹: {folderPath}", ConsoleColor.Green);
            }

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "[]");
                LogDebug($"创建房间黑名单文件: {filePath}", ConsoleColor.Green);
            }
        }

        private static void UpdateBlacklist(string filePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    Path_Bool.RoomBlacklist = new List<string>();
                    LogDebug("房间黑名单文件为空,自动替换成空列表", ConsoleColor.Red);
                }
                else
                {
                    Path_Bool.RoomBlacklist = JArray.Parse(jsonContent).Select(x => x.ToString()).ToList();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"读取房间黑名单时发生错误: {ex.Message}", ConsoleColor.Red);
                Path_Bool.RoomBlacklist = new List<string>();
            }
        }
        
        private static void UpdateBlacklist_Regex(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LogDebug("未检测到房间黑名单文件,自动创建文件",ConsoleColor.Red);
                    File.WriteAllText(filePath, "[]");
                }
                string jsonContent = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    Path_Bool.RegexBlacklist = new List<string>();
                    LogDebug("房间黑名单文件为空,自动替换成空列表", ConsoleColor.Red);
                }
                else
                {
                    Path_Bool.RegexBlacklist = JArray.Parse(jsonContent).Select(x => x.ToString()).ToList();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"读取房间黑名单时发生错误: {ex.Message}", ConsoleColor.Red);
                Path_Bool.RegexBlacklist = new List<string>();
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
                LogDebug($"[RoomInfo]玩家 {userId} 在黑名单内,但不是房主,无法踢出房间", ConsoleColor.Red);
            }
        }

        private static bool IsCurrentUserRoomOwner()
        {
            return Path_Bool.RoomInfo?.entity?.owner_id == aze<arg>.Instance.User.Id;
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
                        room_id = Path_Bool.RoomInfo.entity.entity_id, 
                        user_id = userId 
                    });

                    kickResult = JObject.Parse(X19Http.RequestX19Api("/online-lobby-member-kick", requestData));

                    if (kickResult["code"].ToObject<int>() == 0)
                    {
                        LogDebug($"[RoomInfo]玩家 {playerName} {reason},已自动踢出房间", ConsoleColor.Red);
                        
                        if (Path_Bool.IsStartWebSocket)
                        {
                            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new 
                            { 
                                type = "Recv_Pocket", 
                                status = "kick", 
                                playerName = playerName,
                                reason = reason
                            }));
                        }
                        break;
                    }
                    else
                    {
                        LogDebug($"[RoomInfo]玩家 {playerName} {reason},踢出失败,正在重试...", ConsoleColor.Red);
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                LogDebug($"踢出玩家时发生错误: {ex.Message}", ConsoleColor.Red);
            }
        }

        private static void CheckRegexBlacklist(string userId, string playerName, string blacklistFilePath)
        {
            if (Path_Bool.RegexBlacklist == null || !Path_Bool.RegexBlacklist.Any())
            {
                return;
            }

            foreach (var regex in Path_Bool.RegexBlacklist)
            {
                try
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(playerName, regex))
                    {
                        // 添加到黑名单
                        Path_Bool.RoomBlacklist.Add(userId);
                        File.WriteAllText(blacklistFilePath, JsonConvert.SerializeObject(Path_Bool.RoomBlacklist));
                        LogDebug($"[RoomInfo]玩家 {playerName} 匹配正则表达式 {regex}，已添加到黑名单", ConsoleColor.Yellow);

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
                    LogDebug($"处理正则表达式 {regex} 时发生错误: {ex.Message}", ConsoleColor.Red);
                }
            }
        }

        private static void HandleRoomInfoChange(JObject newRoomInfo)
        {
            try
            {
                if (Path_Bool.RoomInfo?.entity == null)
                {
                    LogDebug("RoomInfo或entity为空，无法更新房间信息", ConsoleColor.Red);
                    return;
                }

                LogDebug("房间信息已更改:", ConsoleColor.Cyan);
                LogDebug("-----------------------------------------------------------", ConsoleColor.Cyan);

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
                if (Path_Bool.IsStartWebSocket)
                {
                    WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                    {
                        type = "Recv_Pocket",
                        status = "room_update",
                        roomInfo = newRoomInfo,
                        currentPlayers = Path_Bool.RoomInfo.entity.fids,
                        playerCount = Path_Bool.RoomInfo.entity.cur_num
                    }));
                }

                LogDebug("-----------------------------------------------------------", ConsoleColor.Cyan);
            }
            catch (Exception ex)
            {
                LogDebug($"处理房间信息变化时发生错误: {ex.Message}", ConsoleColor.Red);
            }
        }

        private static void LogRoomInfoChanges(JObject newRoomInfo)
        {
            var entity = Path_Bool.RoomInfo.entity;

            if (entity.entity_id != newRoomInfo["entity_id"].ToObject<string>())
            {
                LogDebug($"房间ID更改为: {newRoomInfo["entity_id"].ToObject<string>()}", ConsoleColor.Green);
            }

            if (entity.owner_id != newRoomInfo["owner_id"].ToObject<string>())
            {
                LogOwnerChange(entity.owner_id, newRoomInfo["owner_id"].ToObject<string>());
            }

            if (entity.room_name != newRoomInfo["room_name"].ToObject<string>())
            {
                LogDebug($"房间名称更改为: {newRoomInfo["room_name"].ToObject<string>()}", ConsoleColor.Magenta);
            }

            if (entity.save_size != newRoomInfo["save_size"].ToObject<uint>())
            {
                LogDebug($"保存大小更改为: {newRoomInfo["save_size"].ToObject<uint>()}", ConsoleColor.Cyan);
            }

            bool newPasswordStatus = newRoomInfo["password"].ToObject<int>() != 0;
            if (entity.password != newPasswordStatus)
            {
                LogDebug($"密码保护更改为: {(newPasswordStatus ? "是" : "否")}", ConsoleColor.Red);
            }

            bool newAllowSave = newRoomInfo["allow_save"].ToObject<int>() != 0;
            if (entity.allow_save != newAllowSave)
            {
                LogDebug($"允许保存更改为: {(newAllowSave ? "是" : "否")}", ConsoleColor.Blue);
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
                JObject oldOwnerInfo = X19Http.Get_Player_Info(oldOwnerId);
                JObject newOwnerInfo = X19Http.Get_Player_Info(newOwnerId);

                LogDebug("当前房间内房主更改", ConsoleColor.Yellow);
                LogDebug($"原房主的UID: {oldOwnerId}", ConsoleColor.Yellow);
                LogDebug($"新房主的UID: {newOwnerId}", ConsoleColor.Yellow);
                LogDebug($"原房主的名称: {oldOwnerInfo["entity"]["name"]}", ConsoleColor.Yellow);
                LogDebug($"新房主的名称: {newOwnerInfo["entity"]["name"]}", ConsoleColor.Yellow);
            }
            catch (Exception ex)
            {
                LogDebug($"记录房主变更信息时发生错误: {ex.Message}", ConsoleColor.Red);
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

                LogDebug($"可见性更改为: {visibilityDescription}", ConsoleColor.Gray);
            }
            catch (Exception ex)
            {
                LogDebug($"记录可见性变更信息时发生错误: {ex.Message}", ConsoleColor.Red);
            }
        }

        private static void UpdateRoomInfo(JObject newRoomInfo)
        {
            var entity = Path_Bool.RoomInfo.entity;
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
            foreach (Window window in Application.Current.Windows)
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
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("====================");
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"服务器ID: {ServerID}");
            Console.WriteLine($"数据内容: {JsonConvert.SerializeObject(Data, Formatting.Indented)}");
            Console.WriteLine($"接口信息: {(ifg?.ToString() ?? "null")}");
            Console.WriteLine($"错误日志: {ErrorLog}");
            Console.WriteLine($"结果: {ResultBool}");
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("====================");
            
            Console.ResetColor();
            return ResultBool;
        }
    }
}