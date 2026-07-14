using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Mcl.Core.Dotnetdetour.Model;
using Mcl.Core.Dotnetdetour.Tools;
using Mcl.Core.Dotnetdetour.Tools.Network;
using Mcl.Core.Dotnetdetour.Var;
using Mcl.Core.NeteaseProtocol;
using Newtonsoft.Json;
using WPFLauncher.Manager;

namespace Mcl.Core.Dotnetdetour.HookList;

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
        if (WpfConfig.IsDebug)
            WpfConfig.DefaultLogger.Info(
                $"[RECV] Peer:{peerId} Len:{dataSize}, Data: {BitConverter.ToString(rawData).Replace('-', ' ')}");

        if (WpfConfig.UseNetworkMode)
        {
            // 调试日志：查看原始数据前几个字节
            // WpfConfig.DefaultLogger.Info($"[RECV] Peer:{peerId} Len:{dataSize} Head:{BitConverter.ToString(rawData.Take(Math.Min(dataSize, 4)).ToArray())}");
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

                    WpfConfig.DefaultLogger.Info($"Received PeerId: {peerId} Virtual IP: {virtualIpString}");

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
        
                        WpfConfig.DefaultLogger.Info($"[成功] 已更新 PeerId {peerId} 的虚拟 IP 为 {playerInfo.VirtualIp}");
                    }
                    else
                    {
                        // 可选：调试用
                        WpfConfig.DefaultLogger.Warn($"[警告] 未找到 PeerId 为 {peerId} 的玩家，无法更新 IP。");
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
                WpfConfig.DefaultLogger.Info($"peerId: {peerId} 玩家进入选择IP阶段");
                passToNetwork = false;
            }
            
            if (LanGameProtocolHelper.IsGamePlayerInfoMagicHeader(rawData))
            {
                // 尝试解析为玩家列表
                if (LanGameProtocolHelper.TryParsePlayerListPacket(rawData, out var players))
                {
                    // 解析成功，更新本地 UI 或逻辑
                    WpfConfig.DefaultLogger.Info($"收到玩家列表，共 {players.Count} 人");

                    // 例如：更新全局列表
                    WebRtcVar.PlayerList = players;
                    if (WebRtcVar.NetworkMonitor != null) WebRtcVar.NetworkMonitor.RefreshPlayerData();
                    // RefreshUiPlayerList();
                }
                else
                {
                    // 解析失败，可能是其他类型的包，或者数据包损坏
                    // 这里可以添加对其他 PacketType (如心跳) 的判断逻辑
                    WpfConfig.DefaultLogger.Warn("收到无效或非玩家列表的数据包");
                }
                passToNetwork = false;
            }
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
            if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info($"[Protocol] {peerId} 握手成功 - 开启多路复用");
            return;
        }

        if (rawData[0] == 0x00 && rawData.Length == 2)
        {
            int connIdInt = rawData[1];
            if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info($"[Protocol] 关闭连接: {connIdInt}");

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
                if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info($"[Mux] 对端通知关闭 ConnId: {connId}");
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
            if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info($"[Decompress] {peerId} {BitConverter.ToString(final)}");
        }

        // 处理 Session 转发
        var sessionKey = WebRtcVar.Mode == ForwardMode.Server ? $"{peerId}_{connId}" : $"LOCAL_CLIENT_{connId}";

        if (WebRtcVar.Sessions.TryGetValue(sessionKey, out var session))
        {
            // 解压逻辑（如果是 Zlib）
            // WpfConfig.DefaultLogger.Info($"Data: {BitConverter.ToString(final)}");
            session.SendToSocket(final);
        }
        else
        {
            // 如果服务端没找到 Session，且是多路复用模式，说明是新请求
            if (WebRtcVar.Mode == ForwardMode.Server && WebRtcVar.PeerSupportMultiplex.ContainsKey(peerId))
            {
                if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info($"[Mux] 创建新服务端 Session: {peerId}_{connId}");

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
                    if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info($"[Mux] 创建新客户端 Session: {peerId}_{connId}");
                    WebRtcVar.Sessions[sessionKey] = new UnifiedSession(peerId, true);
                    WebRtcVar.Sessions[sessionKey].SendToSocket(final);
                }
                else
                {
                    if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Warn($"[Mux] 丢弃孤立包: {sessionKey} (Len:{mcData.Length})");
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
            if (WpfConfig.IsDebug)
                WpfConfig.DefaultLogger.Info(
                    $"[SEND] Peer:{peerId} Len:{payload.Length}, Data: {BitConverter.ToString(payload).Replace('-', ' ')}");
            var result = sMethod.Invoke(null, new object[] { peerPtr.Value, payload, payload.Length });
            var success = (bool)result;

            if (!success) WpfConfig.DefaultLogger.Error("[WebRtc] 一个包发送失败!");
            // WpfConfig.DefaultLogger.Info($"发送调用结果: {success}");
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Info($"[WebRtc] SendBack 发送失败: {ex.Message}");
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
            if (WpfConfig.IsDebug)
                WpfConfig.DefaultLogger.Info(
                    $"[SEND] Peer:{peerId} Len:{payload.Length}, Data: {BitConverter.ToString(payload).Replace('-', ' ')}");
            var result = sMethod.Invoke(null, new object[] { peerPtr.Value, payload, payload.Length });
            var success = (bool)result;

            if (!success) WpfConfig.DefaultLogger.Error("[WebRtc] 一个包发送失败!");
            // WpfConfig.DefaultLogger.Info($"发送调用结果: {success}");
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"[WebRtc] SendBack 发送失败: {ex.Message}");
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
            if (WpfConfig.IsDebug)
                WpfConfig.DefaultLogger.Info(
                    $"[SEND] Peer:{peerId} Len:{data.Length}, Data: {BitConverter.ToString(data).Replace('-', ' ')}");
            var result = sMethod.Invoke(null, new object[] { peerPtr.Value, data, data.Length });
            var success = (bool)result;

            if (!success) WpfConfig.DefaultLogger.Error("[WebRtc] 一个包发送失败!");
            // WpfConfig.DefaultLogger.Info($"发送调用结果: {success}");
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"[WebRtc] SendBack 发送失败: {ex.Message}");
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
            if (WpfConfig.IsDebug)
            {
                WpfConfig.DefaultLogger.Info("--- [TransferServer <<< RECV] ---");
                var hexString = BitConverter.ToString(messageData).Replace("-", " ");
                WpfConfig.DefaultLogger.Info($"Message ID: {messageId}");
                WpfConfig.DefaultLogger.Info($"Data (Hex): {hexString}");
                WpfConfig.DefaultLogger.Info("--------------------------\n");
            }
            if (messageId == 517)
            {
                TransferStruct.PlayerStateChange data = default(TransferStruct.PlayerStateChange);
                var deserializer = new PacketDeserializer(messageData);
                deserializer.Deserialize<TransferStruct.PlayerStateChange>(ref data);
            
                if (WebRtcVar.LanGameManager != null)
                {
                    if (data.State == 1)
                    {
                        WpfConfig.DefaultLogger.Warn($"玩家 {data.UserID} 加入了房间");
                        // WebRtcVar.PlayerList.Add(data.UserID);
                    }
                    else
                    {
                        WpfConfig.DefaultLogger.Warn($"玩家 {data.UserID} 离开了房间");
                        var player = WebRtcVar.PlayerList.FirstOrDefault(x => x.UserID == data.UserID.ToString());
                        
                        if (player != null)
                        {
                            WebRtcVar.PlayerList.Remove(player);
                        }
                    }
                }
            }
            else if (messageId == 515)
            {
                var data = default(TransferStruct.PlayerCreateWebRtcConnectEvent);
                var deserializer = new PacketDeserializer(messageData);
                deserializer.Deserialize(ref data);
                WpfConfig.DefaultLogger.Info(
                    $"MessageData: {BitConverter.ToString(messageData)}, UserId: {data.UserID}, PeerId: {data.PeerId}");

                var playerInfo = X19Http.GetPlayerInfo(data.UserID.ToString());
                WpfConfig.DefaultLogger.Info($"玩家 {playerInfo["entity"]["name"]} 创建了一个 WebRTC 连接, PeerId: {data.PeerId}");
                var player = new LanGamePlayerInfo
                {
                    Name = playerInfo["entity"]["name"].ToString(),
                    UserID = data.UserID.ToString(),
                    PeerId = data.PeerId.ToString(),
                    VirtualIp = "",
                    Status = "连接中..."
                };
                WebRtcVar.PlayerList.Add(player);
                if (WpfConfig.UseNetworkMode)
                {
                    // 假设 PlayerList 已经填充了数据
                    ObservableCollection<LanGamePlayerInfo> currentPlayers = WebRtcVar.PlayerList;

                    // 调用工具生成字节包
                    byte[] packetToSend = LanGameProtocolHelper.BuildPlayerListPacket(currentPlayers);
                    // 发送给特定 peer 或 广播
                    SendData(data.PeerId.ToString(), packetToSend);
                    WpfConfig.DefaultLogger.Info($"[Router] 玩家列表已发送给 {data.PeerId.ToString()}。");
                    WintunRouterService.Instance.SendServerPlayerInfo();
                    if (WebRtcVar.NetworkMonitor != null) WebRtcVar.NetworkMonitor.RefreshPlayerData();
                }
            }
            else if (messageId == 257)
            {
                var data = default(TransferStruct.TransfetLoginResult);
                var deserializer = new PacketDeserializer(messageData);
                deserializer.Deserialize(ref data);
                if (data.Result == 255)
                {
                    WpfConfig.DefaultLogger.Error($"中转服登录失败, 错误码: {data.Result}, 原因: 你未登录/你的账号在另一处登录/Logout");
                }
                else if (data.Result == 0)
                {
                    WpfConfig.DefaultLogger.Info("中转服登录成功!");
                    try
                    {
                        var player = new LanGamePlayerInfo
                        {
                            Name = WPFLauncher.Common.azf<arg>.Instance.User.Nickname,
                            UserID = WPFLauncher.Common.azf<arg>.Instance.User.UserID.ToString(),
                            PeerId = "Any",
                            VirtualIp = "",
                            Status = "无状态"
                        };
                        WebRtcVar.PlayerList.Add(player);
                    }
                    catch (Exception e)
                    {
                        WpfConfig.DefaultLogger.Info("添加自己到玩家列表失败: " + e);
                    }
                }
                else if (data.Result == 4)
                {
                    WpfConfig.DefaultLogger.Error($"中转服登录失败, 错误码: {data.Result}, 原因: 服务器忙，登录中转服失败");
                }
            }

            if (WpfConfig.IsStartWebSocket)
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                    { type = "TransferMessageRecv", data = new { messageId, messageData } }));
        }
        catch (Exception e)
        {
            WpfConfig.DefaultLogger.Error(e);
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
        // 尝试取值
        var player = WebRtcVar.PlayerList.FirstOrDefault(p => p.PeerId == peerId);
        if (player != null)
        {
            WpfConfig.DefaultLogger.Info($"玩家 {player.Name} 断开连接");
            WebRtcVar.PlayerList.Remove(player);
            if (WpfConfig.UseNetworkMode)
            {
                WintunRouterService.Instance.RemoveRouting(peerId);
                WintunRouterService.Instance.SendServerPlayerInfo();
            }

            if (WebRtcVar.NetworkMonitor != null) WebRtcVar.NetworkMonitor.RefreshPlayerData();
        }
        else
        {
            WpfConfig.DefaultLogger.Info($"PeerId 断开连接: {peerId}");
        }

        OriginalOnDataClose(peerId);
    }

    [OriginalMethod]
     private void Original_WebSocket_WebRtc_OnMessage(string Message)
     {
     }
    
     [HookMethod("WebRtc.NET.cm", "f", "Original_WebSocket_WebRtc_OnMessage")]
     private void WebSocket_OnMessage(string Message)
     {
         WpfConfig.DefaultLogger.Info($"[WebSocket_WebRtc]收到消息:{Message}");
         Original_WebSocket_WebRtc_OnMessage(Message);
     }
     [OriginalMethod]
     private void Original_WebSocket_WebRtc_SendMessage(string Message)
     {
     }
     [HookMethod("WebRtc.NET.cm", "h", "Original_WebSocket_WebRtc_SendMessage")]
     private void WebSocket_SendMessage(string Message)
     {
         WpfConfig.DefaultLogger.Info($"[WebSocket_WebRtc]发送消息:{Message}");
         Original_WebSocket_WebRtc_SendMessage(Message);
     }
     
    [OriginalMethod]
    public bool sendTransferMessage(params object[] ObjectMessage)
    {
        return true;
    }
    
    [HookMethod(TargetConst.LanGameManager,"as","sendTransferMessage")]
    public bool SendTransferMessage_HookMethod(params object[] ObjectMessage)
    {
                WpfConfig.DefaultLogger.Info("--- [TransferServer >>> SEND] ---");
        WpfConfig.DefaultLogger.Info(JsonConvert.SerializeObject(ObjectMessage));
        WpfConfig.DefaultLogger.Info("--------------------------\n");
        return sendTransferMessage(ObjectMessage);
    }
    
     [OriginalMethod]
     public void handleReceiveLauncherMessage(ushort messageId, byte[] messageData)
     {
     }
     
     [HookMethod("WPFLauncher.Network.acb","b","handleReceiveLauncherMessage")]
     public void handleReceiveLauncherMessage_HookMethod(ushort messageId, byte[] messageData)
     {
         WpfConfig.DefaultLogger.Debug("--- [Launcher <<< RECV] ---");
         string hexString = BitConverter.ToString(messageData).Replace("-", " ");
         WpfConfig.DefaultLogger.Debug($"Message ID: {messageId}");
         WpfConfig.DefaultLogger.Debug($"Data (Hex): {hexString}");
         WpfConfig.DefaultLogger.Debug("--------------------------\n");
         handleReceiveLauncherMessage(messageId, messageData);
     }
     [OriginalMethod]
     private void SendLauncherMessage(byte[] messageData)
     {
     }
     
     [HookMethod("WPFLauncher.Model.ait","as","SendLauncherMessage")]
     private void SendLauncherMessage_HookMethod(byte[] messageData)
     {
         WpfConfig.DefaultLogger.Debug("--- [Launcher <<< SEND] ---");
         string hexString = BitConverter.ToString(messageData).Replace("-", " ");
         WpfConfig.DefaultLogger.Debug($"Data (Hex): {hexString}");
         WpfConfig.DefaultLogger.Debug("--------------------------\n");
         SendLauncherMessage(messageData);
     }
}