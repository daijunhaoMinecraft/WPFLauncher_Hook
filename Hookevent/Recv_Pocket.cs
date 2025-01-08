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
using aax = WPFLauncher.Network.aax;
using MessageBox = System.Windows.MessageBox;

namespace DotNetTranstor.Hookevent
{
    // Token: 0x02000017 RID: 23
    internal class Recv_Pocket : IMethodHook
    {
        // Token: 0x17001049 RID: 4169
        // (get) Token: 0x06004157 RID: 16727 RVA: 0x0001E1A6 File Offset: 0x0001C3A6
        // Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
        [OriginalMethod]
        public static void Pocket_Info(abp hqx)
        {
        }

        // Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
        [CompilerGenerated]
        [HookMethod("WPFLauncher.Network.abm", "b", "Pocket_Info")]
        // Token: 0x06003885 RID: 14469 RVA: 0x000DE7B0 File Offset: 0x000DC9B0
        public static void b(abp hqx)
        {
            try
            {
                if (hqx.a != "[]")
                {
                    JObject Get_Json_Recv = JObject.Parse(hqx.a);
                    if (Get_Json_Recv.ContainsKey("game_status"))
                    {
                        Path_Bool.RoomInfo.entity.game_status = Get_Json_Recv["game_status"].ToObject<int>();
                        Path_Bool.RoomInfo.entity.cur_num = (uint)Path_Bool.RoomInfo.entity.fids.Count;
                        if (Get_Json_Recv["game_status"].ToObject<int>() == 1)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[RoomInfo]游戏状态发生改变,当前可正常启动游戏(服务器在线状态)");
                            if (Path_Bool.IsStartWebSocket)
                            {
                                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Recv_Pocket", status = "online", data = JObject.Parse(hqx.a) }));
                            }
                        }
                        else if (Get_Json_Recv["game_status"].ToObject<int>() == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[RoomInfo]游戏状态发生改变,当前不可正常启动游戏(服务器离线/不可用状态)");
                            if (Path_Bool.IsStartWebSocket)
                            {
                                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Recv_Pocket", status = "offline", data = JObject.Parse(hqx.a) }));
                            }
                        }
                    }
                    else if (Get_Json_Recv.ContainsKey("op"))
                    {
                        if (Get_Json_Recv["op"].ToString() == "in")
                        {
                            Path_Bool.RoomInfo.entity.fids.Add(Get_Json_Recv["uid"].ToObject<string>());
                            Path_Bool.RoomInfo.entity.cur_num = (uint)Path_Bool.RoomInfo.entity.fids.Count;

                            JObject Get_Player_Info = X19Http.Get_Player_Info(Get_Json_Recv["uid"].ToObject<string>());

                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine("[RoomInfo]玩家 " + Get_Player_Info["entity"]["name"] + " UID:" + Get_Json_Recv["uid"] + " 加入了房间");

                            if (Path_Bool.EnableRoomBlacklist) // 判断房间黑名单功能是否开启
                            {
                                // 刷新房间黑名单
                                string blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
                                string blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");

                                // 检查文件夹是否存在，如果不存在则创建
                                if (!Directory.Exists(blacklistFolderPath))
                                {
                                    Directory.CreateDirectory(blacklistFolderPath);
                                    Console.WriteLine("房间黑名单文件夹已创建: " + blacklistFolderPath);
                                }

                                // 检查BlackList.json文件是否存在，如果不存在则创建
                                if (!File.Exists(blacklistFilePath))
                                {
                                    File.WriteAllText(blacklistFilePath, "[]");
                                    Console.WriteLine("房间黑名单文件已创建: " + blacklistFilePath + "，内容为空列表。");
                                }

                                // 读取BlackList.json文件内容
                                try
                                {
                                    string jsonContent = File.ReadAllText(blacklistFilePath);
                                    if (jsonContent == string.Empty)
                                    {
                                        Path_Bool.RoomBlacklist = new List<string>();
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("[RoomInfo]房间黑名单文件为空,自动替换成空列表");
                                    }
                                    else
                                    {
                                        Path_Bool.RoomBlacklist = JArray.Parse(jsonContent).Select(x => x.ToString()).ToList();
                                        //Console.WriteLine("房间黑名单内容: " + string.Join(", ", Path_Bool.RoomBlacklist)); // 修复输出格式
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("读取房间黑名单时发生错误: " + ex.Message);
                                    Path_Bool.RoomBlacklist = new List<string>(); // 如果读取失败，返回空的List<string>
                                }

                                string userId = Get_Json_Recv["uid"]?.ToObject<string>(); // 安全获取用户ID
                                if (!string.IsNullOrEmpty(userId) && Path_Bool.RoomBlacklist.Contains(userId)) // 判断用户是否在黑名单内
                                {
                                    if (Path_Bool.RoomInfo?.entity?.owner_id == ayx<aqz>.Instance.User.Id)
                                    {
                                        if (Get_Player_Info?["entity"]?["name"] != null)
                                        {
                                            JObject RemovePlayerReturn;
                                            do
                                            {
                                                RemovePlayerReturn = JObject.Parse(X19Http.RequestX19Api("/online-lobby-member-kick", JsonConvert.SerializeObject(new { room_id = Path_Bool.RoomInfo.entity.entity_id, user_id = userId })));
                                                if (RemovePlayerReturn["code"].ToObject<int>() == 0)
                                                {
                                                    Console.ForegroundColor = ConsoleColor.Red;
                                                    Console.WriteLine("[RoomInfo]玩家 " + Get_Player_Info["entity"]["name"] + " 在黑名单内,已自动踢出房间");
                                                    if (Path_Bool.RoomBlacklist != null && Path_Bool.RoomBlacklist.Contains(userId))
                                                    {
                                                        Path_Bool.RoomBlacklist.Remove(userId);
                                                    }
                                                    if (Path_Bool.IsStartWebSocket)
                                                    {
                                                        WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Recv_Pocket", status = "kick", playerName = Get_Player_Info["entity"]["name"], data = JObject.Parse(hqx.a) }));
                                                    }
                                                    break; // 成功踢出后退出循环
                                                }
                                                else
                                                {
                                                    Console.ForegroundColor = ConsoleColor.Red;
                                                    Console.WriteLine("[RoomInfo]玩家 " + Get_Player_Info["entity"]["name"] + " 在黑名单内,踢出失败,正在重试...");
                                                }
                                            } while (true); // 一直重试直到成功
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("[RoomInfo]玩家信息未找到，无法踢出房间");
                                        }
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("[RoomInfo]玩家 " + Get_Player_Info["entity"]["name"] + " 在黑名单内,但不是房主,无法踢出房间");
                                    }
                                }
                                else if (!string.IsNullOrEmpty(Get_Player_Info?["entity"]?["name"]?.ToObject<string>()))
                                {
                                    string playerName = Get_Player_Info?["entity"]?["name"]?.ToObject<string>();
                                    foreach (var regex in Path_Bool.RegexBlacklist)
                                    {
                                        if (System.Text.RegularExpressions.Regex.IsMatch(playerName, regex))
                                        {
                                            // 将玩家UID加入黑名单
                                            Path_Bool.RoomBlacklist.Add(userId);
                                            
                                            // 刷新黑名单文件
                                            blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
                                            blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");
                                            File.WriteAllText(blacklistFilePath, JsonConvert.SerializeObject(Path_Bool.RoomBlacklist));
                                            
                                            // 踢出玩家
                                            JObject RemovePlayerReturn;
                                            do
                                            {
                                                RemovePlayerReturn = JObject.Parse(X19Http.RequestX19Api("/online-lobby-member-kick", JsonConvert.SerializeObject(new { room_id = Path_Bool.RoomInfo.entity.entity_id, user_id = userId })));
                                                if (RemovePlayerReturn["code"].ToObject<int>() == 0)
                                                {
                                                    Console.ForegroundColor = ConsoleColor.Red;
                                                    Console.WriteLine("[RoomInfo]玩家 " + playerName + " 在正则黑名单内,已自动踢出房间");
                                                    break; // 成功踢出后退出循环
                                                }
                                                else
                                                {
                                                    Console.ForegroundColor = ConsoleColor.Red;
                                                    Console.WriteLine("[RoomInfo]玩家 " + playerName + " 在正则黑名单内,踢出失败,正在重试...");
                                                }
                                            } while (true); // 一直重试直到成功
                                            break; // 找到匹配后退出循环
                                        }
                                    }
                                }
                            }

                            if (Path_Bool.IsStartWebSocket)
                            {
                                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Recv_Pocket", status = "join", playerName = Get_Player_Info["entity"]["name"], data = JObject.Parse(hqx.a) }));
                            }
                        }
                        else if (Get_Json_Recv["op"].ToString() == "out")
                        {
                            Path_Bool.RoomInfo.entity.fids.Remove(Get_Json_Recv["uid"].ToObject<string>());
                            Path_Bool.RoomInfo.entity.cur_num = (uint)Path_Bool.RoomInfo.entity.fids.Count;

                            JObject Get_Player_Info = X19Http.Get_Player_Info(Get_Json_Recv["uid"].ToObject<string>());

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[RoomInfo]玩家 " + Get_Player_Info["entity"]["name"] + " UID:" + Get_Json_Recv["uid"] + " 退出了房间");
                            if (Path_Bool.IsStartWebSocket)
                            {
                                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Recv_Pocket", status = "leave", playerName = Get_Player_Info["entity"]["name"], data = JObject.Parse(hqx.a) }));
                            }
                        }
                    }
                    else if (Get_Json_Recv.ContainsKey("entity_id") || Get_Json_Recv.ContainsKey("slogan"))
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("[RoomInfo]房间信息已更改:");
                        Console.WriteLine("-----------------------------------------------------------");
                        // 输出变化的内容
                        if (Path_Bool.RoomInfo.entity.entity_id != Get_Json_Recv["entity_id"].ToObject<string>())
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"房间ID更改为: {Get_Json_Recv["entity_id"].ToObject<string>()}");
                        }
                        if (Path_Bool.RoomInfo.entity.owner_id != Get_Json_Recv["owner_id"].ToObject<string>())
                        {
                            JObject Get_Owner_Player_Info = X19Http.Get_Player_Info(Get_Json_Recv["owner_id"].ToObject<string>());
                            JObject Get_Old_Owner_Player_Info = X19Http.Get_Player_Info(Path_Bool.RoomInfo.entity.owner_id);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("当前房间内房主更改");
                            Console.WriteLine($"原房主的UID: {Path_Bool.RoomInfo.entity.owner_id}");
                            Console.WriteLine($"新房主的UID: {Get_Json_Recv["owner_id"].ToObject<string>()}");
                            Console.WriteLine($"原房主的名称: {Get_Old_Owner_Player_Info["entity"]["name"]}");
                            Console.WriteLine($"新房主的名称: {Get_Owner_Player_Info["entity"]["name"]}");
                        }
                        if (Path_Bool.RoomInfo.entity.room_name != Get_Json_Recv["room_name"].ToObject<string>())
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($"房间名称更改为: {Get_Json_Recv["room_name"].ToObject<string>()}");
                        }
                        if (Path_Bool.RoomInfo.entity.save_size != Get_Json_Recv["save_size"].ToObject<uint>())
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"保存大小更改为: {Get_Json_Recv["save_size"].ToObject<uint>()}");
                        }
                        if (Path_Bool.RoomInfo.entity.password != (Get_Json_Recv["password"].ToObject<int>() == 0 ? false : true))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"密码保护更改为: {(Get_Json_Recv["password"].ToObject<int>() == 0 ? "否" : "是")}");
                        }
                        if (Path_Bool.RoomInfo.entity.allow_save != (Get_Json_Recv["allow_save"].ToObject<int>() == 0 ? false : true))
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine($"允许保存更改为: {(Get_Json_Recv["allow_save"].ToObject<int>() == 0 ? "否" : "是")}");
                        }
                        if (Path_Bool.RoomInfo.entity.visibility != Get_Json_Recv["visibility"].ToObject<RoomVisibleStatus>())
                        {
                            RoomVisibleStatus visibilityStatus = Get_Json_Recv["visibility"].ToObject<RoomVisibleStatus>();
                            string visibilityDescription = visibilityStatus.GetType()
                                .GetField(visibilityStatus.ToString())
                                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                                .FirstOrDefault() is DescriptionAttribute descriptionAttribute ? descriptionAttribute.Description : visibilityStatus.ToString();
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine($"可见性更改为: {visibilityDescription}");
                        }
                        Path_Bool.RoomInfo.entity.entity_id = Get_Json_Recv["entity_id"].ToObject<string>();
                        Path_Bool.RoomInfo.entity.owner_id = Get_Json_Recv["owner_id"].ToObject<string>();
                        Path_Bool.RoomInfo.entity.room_name = Get_Json_Recv["room_name"].ToObject<string>();
                        Path_Bool.RoomInfo.entity.save_size = Get_Json_Recv["save_size"].ToObject<uint>();
                        Path_Bool.RoomInfo.entity.password = Get_Json_Recv["password"].ToObject<int>() == 0 ? false : true;
                        Path_Bool.RoomInfo.entity.allow_save = Get_Json_Recv["allow_save"].ToObject<int>() == 0 ? false : true;
                        Path_Bool.RoomInfo.entity.visibility = Get_Json_Recv["visibility"].ToObject<RoomVisibleStatus>();

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("-----------------------------------------------------------");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        if (Path_Bool.IsStartWebSocket)
                        {
                            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Recv_Pocket", data = JObject.Parse(hqx.a) }));
                        }
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"[Recv_Pocket]接收到来自网易聊天服务器请求包:{hqx.a}");
                    }
                    
                }
                else
                {
                    if (Path_Bool.RoomInfo != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[RoomInfo]你已被房主踢出房间");
                        if (Path_Bool.IsStartWebSocket)
                        {
                            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Recv_Pocket", status = "kick", data = JObject.Parse(hqx.a) }));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] 发生错误: " + e.Message);
                Console.WriteLine("[STACK TRACE] " + e.StackTrace);
            }
            Console.ForegroundColor = ConsoleColor.White;
            Path_Bool.RecvList.Add(JObject.Parse(hqx.a));
            Pocket_Info(hqx);
        }
    }
}