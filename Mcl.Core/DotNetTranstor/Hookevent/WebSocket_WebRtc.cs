using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using DotNetTranstor;
using DotNetTranstor.Hookevent;
using Mcl.Core.DotNetTranstor.Model;
using Mcl.Core.DotNetTranstor.Tools;
using Mcl.Core.DotNetTranstor.Tools.Network;
using Mcl.Core.DotNetTranstor.Var;
using Newtonsoft.Json;
using WPFLauncher.Code;
using WPFLauncher.Common;
using WPFLauncher.Manager;

namespace Mcl.Core;

public class WebSocket_WebRtc : IMethodHook
{
    [OriginalMethod]
    private static void Original_p(object target, string peerId, IntPtr dataPtr, int dataSize)
    {
    }

    [OriginalMethod]
    private static void Original_ctor(object target, long app, string apq, bool apr)
    {
    }

    [HookMethod("WebRtc.NET.cm", ".ctor", "Original_ctor")]
    private static void Hook_ctor(object target, long peerId, string webSocketConnectUrl, bool isCompress)
    {
        WebRtcVar.CmInstance = target;
        WebRtcVar.MyPeerId = peerId.ToString();
        Original_ctor(target, peerId, webSocketConnectUrl, isCompress);
    }

    [HookMethod("WebRtc.NET.cm", "p", "Original_p")]
    private static void Hook_p(object target, string peerId, IntPtr dataPtr, int dataSize)
    {
        WebRtcVar.CmInstance = target;
        // if (!WebRtcVar.Enable || dataSize <= 0) return;
        if (!WebRtcVar.Enable)
        {
            Original_p(target, peerId, dataPtr, dataSize);
            return;
        }

        var rawData = new byte[dataSize];
        Marshal.Copy(dataPtr, rawData, 0, dataSize);
        if (Path_Bool.IsDebug)
            Console.WriteLine(
                $"[RECV] Peer:{peerId} Len:{dataSize}, Data: {BitConverter.ToString(rawData).Replace('-', ' ')}");

        // 调试日志：查看原始数据前几个字节
        // Console.WriteLine($"[RECV] Peer:{peerId} Len:{dataSize} Head:{BitConverter.ToString(rawData.Take(Math.Min(dataSize, 4)).ToArray())}");
        bool passToNetwork = true;

        if (VirualIpProto.IsMagicHeader(rawData))
        {
            if (WebRtcVar.Mode == ForwardMode.Server)
            {
                // 3. 提取 IP 字节部分 (从索引 3 开始，取 4 个字节)
                var ipBytes = new byte[4];
                Array.Copy(rawData, 3, ipBytes, 0, 4);

                // 4. 转换回 IP 地址对象
                var receivedIp = new IPAddress(ipBytes);
                var virtualIpString = receivedIp.ToString();

                Console.WriteLine($"Received PeerId: {peerId} Virtual IP: {virtualIpString}");

                // 【修改点】使用 for 循环手动查找索引，替代 FindIndex
                // ObservableCollection 不支持 FindIndex，但支持通过索引器访问
                int index = -1;
                for (int i = 0; i < WebRtcVar.PlayerList.Count; i++)
                {
                    if (WebRtcVar.PlayerList[i].PeerId == peerId)
                    {
                        index = i;
                        break;
                    }
                }

                // 2. 检查索引是否有效
                if (index != -1)
                {
                    // 3. 通过索引获取当前元素（如果是 struct，这里拿到的是副本）
                    var playerInfo = WebRtcVar.PlayerList[index];
    
                    // 4. 修改副本的属性
                    playerInfo.VirtualIp = virtualIpString;
                    playerInfo.Status = "已连接";
    
                    // 5. 【关键步骤】将修改后的副本写回列表
                    // 对于 ObservableCollection，这一步会触发 CollectionChanged 事件，通知 UI 更新
                    WebRtcVar.PlayerList[index] = playerInfo;
                    WintunRouterService.Instance.SetRouting(virtualIpString, peerId);
                    if (WebRtcVar.NetworkMonitor != null) WebRtcVar.NetworkMonitor.RefreshPlayerData();

                    // 6. 执行后续逻辑
                    WintunRouterService.Instance.SendServerPlayerInfo();
    
                    Console.WriteLine($"[成功] 已更新 PeerId {peerId} 的虚拟 IP 为 {playerInfo.VirtualIp}");
                }
                else
                {
                    // 可选：调试用
                    Console.WriteLine($"[警告] 未找到 PeerId 为 {peerId} 的玩家，无法更新 IP。");
                }

                passToNetwork = false;
            }
        }

        if (GetPlayerListProto.IsMagicHeader(rawData))
        {
            // 假设 PlayerList 已经填充了数据
            ObservableCollection<LanGamePlayerInfo> currentPlayers = WebRtcVar.PlayerList;

            // 调用工具生成字节包
            byte[] packetToSend = LanGameProtocolHelper.BuildPlayerListPacket(currentPlayers);
            SendData(peerId, packetToSend);
            Console.WriteLine($"[Info] peerId: {peerId} 玩家进入选择IP阶段");
            passToNetwork = false;
        }
        
        if (LanGameProtocolHelper.IsGamePlayerInfoMagicHeader(rawData))
        {
            // 尝试解析为玩家列表
            if (LanGameProtocolHelper.TryParsePlayerListPacket(rawData, out var players))
            {
                // 解析成功，更新本地 UI 或逻辑
                Console.WriteLine($"收到玩家列表，共 {players.Count} 人");

                // 例如：更新全局列表
                WebRtcVar.PlayerList = players;
                if (WebRtcVar.NetworkMonitor != null) WebRtcVar.NetworkMonitor.RefreshPlayerData();
                // RefreshUiPlayerList();
            }
            else
            {
                // 解析失败，可能是其他类型的包，或者数据包损坏
                // 这里可以添加对其他 PacketType (如心跳) 的判断逻辑
                Console.WriteLine("收到无效或非玩家列表的数据包");
            }
            passToNetwork = false;
        }

        if (Path_Bool.UseNetworkMode)
        {
            if (passToNetwork)
            {
                // 收到数据，交给路由器模块处理 (包含路由转发和注入功能)
                WintunRouterService.Instance.OnDataReceived(peerId, rawData);
            }
            return; // 跳过原有的端口转发解析
        }

        // 握手识别
        if (IsMagicHandshake(rawData))
        {
            WebRtcVar.PeerSupportMultiplex[peerId] = true;
            if (Path_Bool.IsDebug) Console.WriteLine($"[Protocol] {peerId} 握手成功 - 开启多路复用");
            return;
        }

        if (rawData[0] == 0x00 && rawData.Length == 2)
        {
            int connIdInt = rawData[1];
            if (Path_Bool.IsDebug) Console.WriteLine($"[Protocol] 关闭连接: {connIdInt}");

            var sessionKey_1 = WebRtcVar.Mode == ForwardMode.Server
                ? $"{peerId}_{connIdInt}"
                : $"LOCAL_CLIENT_{connIdInt}";
            if (WebRtcVar.Sessions.TryGetValue(sessionKey_1, out var session_1))
            {
                session_1.Close();
                WebRtcVar.Sessions.TryRemove(sessionKey_1, out _);
            }

            return;
        }

        byte connId = 0;
        byte[] mcData;

        if (WebRtcVar.PeerSupportMultiplex.ContainsKey(peerId))
        {
            connId = rawData[0];
            // 如果收到长度为 1 的包（只有 ConnId），代表对端通知连接断开
            if (dataSize == 1)
            {
                if (Path_Bool.IsDebug) Console.WriteLine($"[Mux] 对端通知关闭 ConnId: {connId}");
                if (WebRtcVar.Sessions.TryRemove($"{peerId}_{connId}", out var s)) s.Close();
                return;
            }

            mcData = new byte[dataSize - 1];
            Buffer.BlockCopy(rawData, 1, mcData, 0, mcData.Length);
        }
        else
        {
            mcData = rawData;
        }

        var final = mcData;
        if (WebRtcVar.IsNativeCompressionEnabled() && !WebRtcVar.PeerSupportMultiplex.ContainsKey(peerId))
        {
            var comp = WebRtcVar.GetCompressor(WebRtcVar.getIntPtrFromPeerId(peerId).Value);
            if (comp != null) final = comp.b(mcData);
            if (Path_Bool.IsDebug) Console.WriteLine($"[Decompress] {peerId} {BitConverter.ToString(final)}");
        }

        // 处理 Session 转发
        var sessionKey = WebRtcVar.Mode == ForwardMode.Server ? $"{peerId}_{connId}" : $"LOCAL_CLIENT_{connId}";

        if (WebRtcVar.Sessions.TryGetValue(sessionKey, out var session))
        {
            // 解压逻辑（如果是 Zlib）
            // Console.WriteLine($"Data: {BitConverter.ToString(final)}");
            session.SendToSocket(final);
        }
        else
        {
            // 如果服务端没找到 Session，且是多路复用模式，说明是新请求
            if (WebRtcVar.Mode == ForwardMode.Server && WebRtcVar.PeerSupportMultiplex.ContainsKey(peerId))
            {
                if (Path_Bool.IsDebug) Console.WriteLine($"[Mux] 创建新服务端 Session: {peerId}_{connId}");

                var newSession = new UnifiedSession(peerId, connId);
                WebRtcVar.Sessions[sessionKey] = newSession;
                // 递归调用一次处理当前包
                newSession.SendToSocket(final);
            }
            else
            {
                if (WebRtcVar.IsNativeCompressionEnabled() && !WebRtcVar.PeerSupportMultiplex.ContainsKey(peerId))
                {
                    // 创建新连接
                    if (Path_Bool.IsDebug) Console.WriteLine($"[Mux] 创建新客户端 Session: {peerId}_{connId}");
                    WebRtcVar.Sessions[sessionKey] = new UnifiedSession(peerId, true);
                    WebRtcVar.Sessions[sessionKey].SendToSocket(final);
                }
                else
                {
                    if (Path_Bool.IsDebug) Console.WriteLine($"[Mux] 丢弃孤立包: {sessionKey} (Len:{mcData.Length})");
                }
            }
        }
    }

    private static bool IsMagicHandshake(byte[] data)
    {
        if (data.Length < MultiplexProto.MagicHandshake.Length) return false;
        for (var i = 0; i < MultiplexProto.MagicHandshake.Length; i++)
            if (data[i] != MultiplexProto.MagicHandshake[i])
                return false;
        return true;
    }

    // 修改后的回传方法，带上 ConnId
    public static void SendBack(byte[] data, string peerId, byte connId)
    {
        if (WebRtcVar.CmInstance == null) return;

        var peerPtr = WebRtcVar.getIntPtrFromPeerId(peerId);
        if (peerPtr == null) return;

        // 1. 压缩数据
        var finalData = data;
        var isGet = WebRtcVar.PeerSupportMultiplex.TryGetValue(peerId, out var isMux);

        if (WebRtcVar.IsNativeCompressionEnabled() && !isMux)
        {
            var compress = WebRtcVar.GetCompressor(peerPtr.Value);
            if (compress != null) finalData = compress.a(data);
        }

        // 2. 包装多路复用头
        byte[] payload;
        if (isGet && isMux)
        {
            payload = new byte[finalData.Length + 1];
            payload[0] = connId;
            Buffer.BlockCopy(finalData, 0, payload, 1, finalData.Length);
        }
        else
        {
            payload = finalData;
        }

        // 3. 反射调用原生发送
        try
        {
            var asm = WebRtcVar.CmInstance.GetType().Assembly;
            var ateType = asm.GetType("WPFLauncher.Manager.LanGame.ate");
            var sMethod = ateType.GetMethod("s", BindingFlags.Public | BindingFlags.Static);
            if (Path_Bool.IsDebug)
                Console.WriteLine(
                    $"[SEND] Peer:{peerId} Len:{payload.Length}, Data: {BitConverter.ToString(payload).Replace('-', ' ')}");
            var result = sMethod.Invoke(null, new object[] { peerPtr.Value, payload, payload.Length });
            var success = (bool)result;

            if (!success) Console.WriteLine("[WebRtc] 一个包发送失败!");
            // Console.WriteLine($"发送调用结果: {success}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebRtc] SendBack 发送失败: {ex.Message}");
        }
    }

    public static void SendClosePacket(string peerId, byte connId)
    {
        if (WebRtcVar.CmInstance == null) return;

        var peerPtr = WebRtcVar.getIntPtrFromPeerId(peerId);
        if (peerPtr == null) return;
        // 2. 包装多路复用头
        var payload = new byte[] { 0x00, connId };

        // 3. 反射调用原生发送
        try
        {
            var asm = WebRtcVar.CmInstance.GetType().Assembly;
            var ateType = asm.GetType("WPFLauncher.Manager.LanGame.ate");
            var sMethod = ateType.GetMethod("s", BindingFlags.Public | BindingFlags.Static);
            if (Path_Bool.IsDebug)
                Console.WriteLine(
                    $"[SEND] Peer:{peerId} Len:{payload.Length}, Data: {BitConverter.ToString(payload).Replace('-', ' ')}");
            var result = sMethod.Invoke(null, new object[] { peerPtr.Value, payload, payload.Length });
            var success = (bool)result;

            if (!success) Console.WriteLine("[WebRtc] 一个包发送失败!");
            // Console.WriteLine($"发送调用结果: {success}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebRtc] SendBack 发送失败: {ex.Message}");
        }
    }

    public static void SendData(string peerId, byte[] data)
    {
        if (WebRtcVar.CmInstance == null) return;

        var peerPtr = WebRtcVar.getIntPtrFromPeerId(peerId);
        if (peerPtr == null) return;

        // 3. 反射调用原生发送
        try
        {
            var asm = WebRtcVar.CmInstance.GetType().Assembly;
            var ateType = asm.GetType("WPFLauncher.Manager.LanGame.ate");
            var sMethod = ateType.GetMethod("s", BindingFlags.Public | BindingFlags.Static);
            if (Path_Bool.IsDebug)
                Console.WriteLine(
                    $"[SEND] Peer:{peerId} Len:{data.Length}, Data: {BitConverter.ToString(data).Replace('-', ' ')}");
            var result = sMethod.Invoke(null, new object[] { peerPtr.Value, data, data.Length });
            var success = (bool)result;

            if (!success) Console.WriteLine("[WebRtc] 一个包发送失败!");
            // Console.WriteLine($"发送调用结果: {success}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebRtc] SendBack 发送失败: {ex.Message}");
        }
    }


    [OriginalMethod]
    private bool processReceiveTransferMessage(ushort messageId, byte[] messageData)
    {
        return true;
    }

    [HookMethod("WPFLauncher.Network.TransService.ace", "b", "processReceiveTransferMessage")]
    private bool processReceiveTransferMessage_HookMethod(ushort messageId, byte[] messageData)
    {
        try
        {
            if (Path_Bool.IsDebug)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("--- [TransferServer <<< RECV] ---");
                var hexString = BitConverter.ToString(messageData).Replace("-", " ");
                Console.WriteLine($"Message ID: {messageId}");
                Console.WriteLine($"Data (Hex): {hexString}");
                Console.WriteLine("--------------------------\n");
            }
            // if (messageId == 517)
            // {
            //     TransferStruct.PlayerStateChange data = default(TransferStruct.PlayerStateChange);
            //     var deserializer = new PacketDeserializer(messageData);
            //     deserializer.Deserialize<TransferStruct.PlayerStateChange>(ref data);
            //
            //     if (WebRtcVar.AitFunction != null)
            //     {
            //         Console.ForegroundColor = ConsoleColor.Yellow;
            //         if (data.State == 1)
            //         {
            //             Console.WriteLine($"玩家 {data.UserID} 加入了房间");
            //             WebRtcVar.PlayerList.Add(data.UserID);
            //         }
            //         else
            //         {
            //             Console.WriteLine($"玩家 {data.UserID} 离开了房间");
            //             WebRtcVar.PlayerList.Remove(data.UserID);
            //         }
            //         Console.ForegroundColor = ConsoleColor.White;
            //     }
            // }
            else if (messageId == 515)
            {
                var data = default(TransferStruct.PlayerCreateWebRtcConnectEvent);
                var deserializer = new PacketDeserializer(messageData);
                deserializer.Deserialize(ref data);
                Console.WriteLine(
                    $"MessageData: {BitConverter.ToString(messageData)}, UserId: {data.UserID}, PeerId: {data.PeerId}");

                var playerInfo = X19Http.Get_Player_Info(data.UserID.ToString());
                Console.WriteLine($"玩家 {playerInfo["entity"]["name"]} 创建了一个 WebRTC 连接, PeerId: {data.PeerId}");
                var player = new LanGamePlayerInfo
                {
                    Name = playerInfo["entity"]["name"].ToString(),
                    UserID = data.UserID.ToString(),
                    PeerId = data.PeerId.ToString(),
                    VirtualIp = "",
                    Status = "连接中..."
                };
                WebRtcVar.PlayerList.Add(player);
                
                // 假设 PlayerList 已经填充了数据
                ObservableCollection<LanGamePlayerInfo> currentPlayers = WebRtcVar.PlayerList;

                // 调用工具生成字节包
                byte[] packetToSend = LanGameProtocolHelper.BuildPlayerListPacket(currentPlayers);
                // 发送给特定 peer 或 广播
                SendData(data.PeerId.ToString(), packetToSend);
                Console.WriteLine($"[Router] 玩家列表已发送给 {data.PeerId.ToString()}。");
                WintunRouterService.Instance.SendServerPlayerInfo();
                if (WebRtcVar.NetworkMonitor != null) WebRtcVar.NetworkMonitor.RefreshPlayerData();
            }
            else if (messageId == 257)
            {
                var data = default(TransferStruct.TransfetLoginResult);
                var deserializer = new PacketDeserializer(messageData);
                deserializer.Deserialize(ref data);
                if (data.Result == 255)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"中转服登录失败, 错误码: {data.Result}, 原因: 你未登录/你的账号在另一处登录/Logout");
                }
                else if (data.Result == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("中转服登录成功!");
                    try
                    {
                        var player = new LanGamePlayerInfo
                        {
                            Name = aze<arg>.Instance.User.Nickname,
                            UserID = aze<arg>.Instance.User.UserID.ToString(),
                            PeerId = "Any",
                            VirtualIp = "",
                            Status = "无状态"
                        };
                        WebRtcVar.PlayerList.Add(player);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("添加自己到玩家列表失败: " + e);
                    }
                }
                else if (data.Result == 4)
                {
                    Console.WriteLine($"中转服登录失败, 错误码: {data.Result}, 原因: 服务器忙，登录中转服失败");
                }

                Console.ForegroundColor = ConsoleColor.White;
            }

            if (Path_Bool.IsStartWebSocket)
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                    { type = "TransferMessageRecv", data = new { messageId, messageData } }));
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e);
            Console.ForegroundColor = ConsoleColor.White;
        }

        return processReceiveTransferMessage(messageId, messageData);
    }

    [OriginalMethod]
    private void OriginalOnDataClose(string peerId)
    {
    }

    [HookMethod("WebRtc.NET.cm", "s", "OriginalOnDataClose")]
    private void OnDataClose(string peerId)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        // 尝试取值
        var player = WebRtcVar.PlayerList.FirstOrDefault(p => p.PeerId == peerId);
        if (player != null)
        {
            Console.WriteLine($"玩家 {player.Name} 断开连接");
            WebRtcVar.PlayerList.Remove(player);
            WintunRouterService.Instance.RemoveRouting(peerId);
            WintunRouterService.Instance.SendServerPlayerInfo();
            if (WebRtcVar.NetworkMonitor != null) WebRtcVar.NetworkMonitor.RefreshPlayerData();
        }
        else
        {
            Console.WriteLine($"PeerId 断开连接: {peerId}");
        }

        Console.ForegroundColor = ConsoleColor.White;
        OriginalOnDataClose(peerId);
    }

    // [OriginalMethod]
    //  private void Original_WebSocket_WebRtc_OnMessage(string Message)
    //  {
    //  }
    //
    //  [HookMethod("WebRtc.NET.cm", "f", "Original_WebSocket_WebRtc_OnMessage")]
    //  private void WebSocket_OnMessage(string Message)
    //  {
    //      Console.ForegroundColor = ConsoleColor.Green;
    //      Console.WriteLine($"[WebSocket_WebRtc]收到消息:{Message}");
    //      Console.ForegroundColor = ConsoleColor.White;
    //      Original_WebSocket_WebRtc_OnMessage(Message);
    //  }
    //  [OriginalMethod]
    //  private void Original_WebSocket_WebRtc_SendMessage(string Message)
    //  {
    //  }
    //  [HookMethod("WebRtc.NET.cm", "h", "Original_WebSocket_WebRtc_SendMessage")]
    //  private void WebSocket_SendMessage(string Message)
    //  {
    //      Console.ForegroundColor = ConsoleColor.Green;
    //      Console.WriteLine($"[WebSocket_WebRtc]发送消息:{Message}");
    //      Console.ForegroundColor = ConsoleColor.White;
    //      Original_WebSocket_WebRtc_SendMessage(Message);
    //  }
    //  
    //  
    //  // ====================================================================
    //  // 1. Hook WebRTC 数据接收
    //  // 目标: WebRtc.NET.cm.p(string, IntPtr, int)
    //  // ====================================================================
    //
    //  [OriginalMethod]
    //  private void Original_OnWebRtcDataReceived(string peerId, IntPtr dataPtr, int dataSize)
    //  {
    //      // 这个方法体是空的，因为它会被Hook框架替换为对原始方法的调用。
    //  }
    //
    //  [HookMethod("WebRtc.NET.cm", "p", "Original_OnWebRtcDataReceived")]
    //  private void Hook_OnWebRtcDataReceived(string peerId, IntPtr dataPtr, int dataSize)
    //  {
    //      // try
    //      // {
    //      //     // 提取数据
    //      //     byte[] receivedData = new byte[aqp];
    //      //     Marshal.Copy(aqo, receivedData, 0, aqp);
    //      //     if (Path_Bool.IsDebug)
    //      //     {
    //      //         // 打印日志
    //      //         Console.ForegroundColor = ConsoleColor.Cyan; // 接收用青色
    //      //         Console.WriteLine("--- [WebRTC <<< RECV] ---");
    //      //         Console.WriteLine($"From Peer ID: {aqn}");
    //      //         Console.WriteLine($"Data Size: {aqp} bytes");
    //      //     
    //      //         string hexString = BitConverter.ToString(receivedData).Replace("-", " ");
    //      //         Console.WriteLine($"Data (Hex): {hexString}");
    //      //         Console.WriteLine("--------------------------\n");
    //      //         Console.ForegroundColor = ConsoleColor.White;
    //      //     }
    //      // }
    //      // catch (Exception ex)
    //      // {
    //      //     Console.WriteLine($"[Hook Error] in OnWebRtcDataReceived: {ex.Message}");
    //      // }
    //      //     // 调用原始方法，确保游戏逻辑继续正常运行
    //      //     Original_OnWebRtcDataReceived(aqn, aqo, aqp);
    //      // }
    //      try
    //      {
    //          // 提取数据
    //          byte[] receivedData = new byte[dataSize];
    //          Marshal.Copy(dataPtr, receivedData, 0, dataSize);
    //          
    //          // 日志输出
    //          if (Path_Bool.IsDebug)
    //          {
    //              Console.ForegroundColor = ConsoleColor.Cyan; // 接收用青色
    //              Console.WriteLine("--- [WebRTC <<< RECV] ---");
    //              Console.WriteLine($"From Peer ID: {peerId}");
    //              Console.WriteLine($"Data Size: {dataSize} bytes");
    //              
    //              string hexString = BitConverter.ToString(receivedData).Replace("-", " ");
    //              Console.WriteLine($"Data (Hex): {hexString}");
    //              Console.WriteLine("--------------------------\n");
    //              Console.ForegroundColor = ConsoleColor.White;
    //          }
    //      }
    //      catch (Exception ex)
    //      {
    //          Console.WriteLine($"[Hook Error] in OnWebRtcDataReceived: {ex.Message}");
    //      }
    //
    //      // 调用原始方法，确保游戏逻辑继续正常运行
    //      Original_OnWebRtcDataReceived(peerId, dataPtr, dataSize);
    //  }
    //
    //  // ====================================================================
    //  // 2. Hook WebRTC 数据发送
    //  // 目标: WebRtc.NET.cm.ai(IntPtr, byte[])
    //  // ====================================================================
    //  
    //  [OriginalMethod]
    //  private bool Original_SendDataInternal(IntPtr peerHandle, byte[] data)
    //  {
    //      // 空方法体，会被框架替换
    //      return false; // 返回值需要匹配原方法
    //  }
    //
    //  [HookMethod("WebRtc.NET.cm", "ai", "Original_SendDataInternal")]
    //  private bool Hook_SendDataInternal(IntPtr arm, byte[] arn)
    //  {
    //      try
    //      {
    //          // // 尝试从 cm 实例中找到 Peer ID
    //          // string peerId = "Unknown";
    //          // // __instance 是Hook框架注入的对 cm 实例的引用
    //          // if (__instance != null && __instance.d != null)
    //          // {
    //          //     foreach (var entry in __instance.d) // 遍历 d (peerConnections) 字典
    //          //     {
    //          //         if (entry.Value == arm)
    //          //         {
    //          //             peerId = entry.Key;
    //          //             break;
    //          //         }
    //          //     }
    //          // }
    //          if (Path_Bool.IsDebug)
    //          {
    //              // 打印日志
    //              Console.ForegroundColor = ConsoleColor.Yellow; // 发送用黄色
    //              Console.WriteLine("--- [WebRTC >>> SEND] ---");
    //              Console.WriteLine($"Handle: {arm}");
    //              Console.WriteLine($"Data Size: {arn.Length} bytes");
    //
    //              string hexString = BitConverter.ToString(arn).Replace("-", " ");
    //              Console.WriteLine($"Data (Hex): {hexString}");
    //              byte[] compressData = new av().a(arn);
    //              Console.WriteLine($"Data (Compress): {BitConverter.ToString(compressData).Replace("-", " ")}");
    //              Console.WriteLine("--------------------------\n");
    //              Console.ForegroundColor = ConsoleColor.White;
    //          }
    //      }
    //      catch (Exception ex)
    //      {
    //          Console.WriteLine($"[Hook Error] in SendDataInternal: {ex.Message}");
    //      }
    //
    //      // 调用原始方法，真正地把数据发送出去
    //      return Original_SendDataInternal(arm, arn);
    //  }
    //  
    // [OriginalMethod]
    // public bool sendTransferMessage(params object[] ObjectMessage)
    // {
    //     return true;
    // }
    //
    // [HookMethod("WPFLauncher.Manager.LanGame.atm","as","sendTransferMessage")]
    // public bool SendTransferMessage_HookMethod(params object[] ObjectMessage)
    // {
    //     Console.ForegroundColor = ConsoleColor.Yellow; // 发送用黄色
    //     Console.WriteLine("--- [TransferServer >>> SEND] ---");
    //     Console.WriteLine(JsonConvert.SerializeObject(ObjectMessage));
    //     Console.WriteLine("--------------------------\n");
    //     return sendTransferMessage(ObjectMessage);
    // }
    //
    // [OriginalMethod]
    // private bool processReceiveTransferMessage(ushort messageId, byte[] messageData)
    // {
    //     return true;
    // }
    //
    // [HookMethod("WPFLauncher.Network.TransService.ace","b","processReceiveTransferMessage")]
    // private bool processReceiveTransferMessage_HookMethod(ushort messageId, byte[] messageData)
    // {
    //     Console.ForegroundColor = ConsoleColor.Cyan;
    //     Console.WriteLine("--- [TransferServer <<< RECV] ---");
    //     string hexString = BitConverter.ToString(messageData).Replace("-", " ");
    //     Console.WriteLine($"Message ID: {messageId}");
    //     Console.WriteLine($"Data (Hex): {hexString}");
    //     Console.WriteLine("--------------------------\n");
    //     return processReceiveTransferMessage(messageId, messageData);
    // }
    //
    //  [OriginalMethod]
    //  public void handleReceiveLauncherMessage(ushort messageId, byte[] messageData)
    //  {
    //  }
    //  
    //  [HookMethod("WPFLauncher.Network.acb","b","handleReceiveLauncherMessage")]
    //  public void handleReceiveLauncherMessage_HookMethod(ushort messageId, byte[] messageData)
    //  {
    //      Console.ForegroundColor = ConsoleColor.Cyan;
    //      Console.WriteLine("--- [Launcher <<< RECV] ---");
    //      string hexString = BitConverter.ToString(messageData).Replace("-", " ");
    //      Console.WriteLine($"Message ID: {messageId}");
    //      Console.WriteLine($"Data (Hex): {hexString}");
    //      Console.WriteLine("--------------------------\n");
    //      handleReceiveLauncherMessage(messageId, messageData);
    //  }
    //  [OriginalMethod]
    //  private void SendLauncherMessage(byte[] messageData)
    //  {
    //  }
    //  
    //  [HookMethod("WPFLauncher.Model.ait","as","SendLauncherMessage")]
    //  private void SendLauncherMessage_HookMethod(byte[] messageData)
    //  {
    //      Console.ForegroundColor = ConsoleColor.Cyan;
    //      Console.WriteLine("--- [Launcher <<< SEND] ---");
    //      string hexString = BitConverter.ToString(messageData).Replace("-", " ");
    //      Console.WriteLine($"Data (Hex): {hexString}");
    //      Console.WriteLine("--------------------------\n");
    //      SendLauncherMessage(messageData);
    //  }
}