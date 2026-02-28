using System;
using System.Linq;
using System.Runtime.InteropServices;
using DotNetTranstor.Hookevent;
using Mcl.Core.DotNetTranstor.Var;
using System.Reflection;
using DotNetTranstor;
using Mcl.Core.DotNetTranstor.Model;
using Newtonsoft.Json;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.LanGame;

namespace Mcl.Core
{
    public class WebSocket_WebRtc : IMethodHook
    {
        [OriginalMethod]
        private static void Original_p(object target, string peerId, IntPtr dataPtr, int dataSize) { }
        
        [OriginalMethod]
        private static void Original_ctor(object target, long app, string apq, bool apr) { }
        
        [HookMethod("WebRtc.NET.cm", ".ctor", "Original_ctor")]
        private static void Hook_ctor(object target, long app, string apq, bool apr) {
            WebRtcVar.CmInstance = target;
            Original_ctor(target, app, apq, apr);
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
        
            byte[] rawData = new byte[dataSize];
            Marshal.Copy(dataPtr, rawData, 0, dataSize);
            // Console.WriteLine($"[RECV] Peer:{peerId} Len:{dataSize}, Data: {BitConverter.ToString(rawData).Replace('-', ' ')}");
        
            // 调试日志：查看原始数据前几个字节
            // Console.WriteLine($"[RECV] Peer:{peerId} Len:{dataSize} Head:{BitConverter.ToString(rawData.Take(Math.Min(dataSize, 4)).ToArray())}");
        
            // 握手识别
            if (IsMagicHandshake(rawData)) {
                WebRtcVar.PeerSupportMultiplex[peerId] = true;
                if (Path_Bool.IsDebug)
                {
                    Console.WriteLine($"[Protocol] {peerId} 握手成功 - 开启多路复用");
                }
                return;
            }
        
            byte connId = 0;
            byte[] mcData;
        
            if (WebRtcVar.PeerSupportMultiplex.ContainsKey(peerId)) {
                connId = rawData[0];
                // 如果收到长度为 1 的包（只有 ConnId），代表对端通知连接断开
                if (dataSize == 1) {
                    if (Path_Bool.IsDebug)
                    {
                        Console.WriteLine($"[Mux] 对端通知关闭 ConnId: {connId}");
                    }
                    if (WebRtcVar.Sessions.TryRemove($"{peerId}_{connId}", out var s)) s.Close();
                    return;
                }
                mcData = new byte[dataSize - 1];
                Buffer.BlockCopy(rawData, 1, mcData, 0, mcData.Length);
            } else {
                mcData = rawData;
            }
            
            byte[] final = mcData;
            if (WebRtcVar.IsNativeCompressionEnabled() && !WebRtcVar.PeerSupportMultiplex.ContainsKey(peerId)) {
                var comp = WebRtcVar.GetCompressor(WebRtcVar.getIntPtrFromPeerId(peerId).Value);
                if (comp != null) final = comp.b(mcData);
                if (Path_Bool.IsDebug)
                {
                    Console.WriteLine($"[Decompress] {peerId} {BitConverter.ToString(final)}");
                }
            }
        
            // 处理 Session 转发
            string sessionKey = (WebRtcVar.Mode == ForwardMode.Server) ? $"{peerId}_{connId}" : $"LOCAL_CLIENT_{connId}";
            
            if (WebRtcVar.Sessions.TryGetValue(sessionKey, out var session)) {
                // 解压逻辑（如果是 Zlib）
                // Console.WriteLine($"Data: {BitConverter.ToString(final)}");
                session.SendToSocket(final);
            } else {
                // 如果服务端没找到 Session，且是多路复用模式，说明是新请求
                if (WebRtcVar.Mode == ForwardMode.Server && WebRtcVar.PeerSupportMultiplex.ContainsKey(peerId)) {
                    if (Path_Bool.IsDebug)
                    {
                        Console.WriteLine($"[Mux] 创建新服务端 Session: {peerId}_{connId}");
                    }

                    var newSession = new UnifiedSession(peerId, connId);
                    WebRtcVar.Sessions[sessionKey] = newSession;
                    // 递归调用一次处理当前包
                    newSession.SendToSocket(final);
                } else {
                    // Console.WriteLine($"[Mux] 丢弃孤立包: {sessionKey} (Len:{mcData.Length})");
                }
            }
        }
        private static bool IsMagicHandshake(byte[] data) {
            if (data.Length < MultiplexProto.MagicHandshake.Length) return false;
            for (int i = 0; i < MultiplexProto.MagicHandshake.Length; i++) {
                if (data[i] != MultiplexProto.MagicHandshake[i]) return false;
            }
            return true;
        }
        
        // 修改后的回传方法，带上 ConnId
        public static void SendBack(byte[] data, string peerId, byte connId)
        {
            if (WebRtcVar.CmInstance == null) return;
        
            IntPtr? peerPtr = WebRtcVar.getIntPtrFromPeerId(peerId);
            if (peerPtr == null) return;
        
            // 1. 压缩数据
            byte[] finalData = data;
            bool isGet = WebRtcVar.PeerSupportMultiplex.TryGetValue(peerId, out bool isMux);

            if (WebRtcVar.IsNativeCompressionEnabled() && !isMux) {
                av compress = WebRtcVar.GetCompressor(peerPtr.Value);
                if (compress != null) finalData = compress.a(data);
            }
        
            // 2. 包装多路复用头
            byte[] payload;
            if (isGet && isMux) {
                payload = new byte[finalData.Length + 1];
                payload[0] = connId;
                Buffer.BlockCopy(finalData, 0, payload, 1, finalData.Length);
            } else {
                payload = finalData;
            }
        
            // 3. 反射调用原生发送
            try {
                Assembly asm = WebRtcVar.CmInstance.GetType().Assembly;
                Type ateType = asm.GetType("WPFLauncher.Manager.LanGame.ate");
                MethodInfo sMethod = ateType.GetMethod("s", BindingFlags.Public | BindingFlags.Static);
                if (Path_Bool.IsDebug)
                {
                    Console.WriteLine($"[SEND] Peer:{peerId} Len:{payload.Length}, Data: {BitConverter.ToString(payload).Replace('-', ' ')}");
                }
                sMethod.Invoke(null, new object[] { peerPtr.Value, payload, payload.Length });
            } catch (Exception ex) {
                Console.WriteLine($"[WebRtc] SendBack 发送失败: {ex.Message}");
            }
        }
        
                [OriginalMethod]
        private bool processReceiveTransferMessage(ushort messageId, byte[] messageData)
        {
            return true;
        }
        
        [HookMethod("WPFLauncher.Network.TransService.ace","b","processReceiveTransferMessage")]
        private bool processReceiveTransferMessage_HookMethod(ushort messageId, byte[] messageData)
        {
            
            if (Path_Bool.IsDebug)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("--- [TransferServer <<< RECV] ---");
                string hexString = BitConverter.ToString(messageData).Replace("-", " ");
                Console.WriteLine($"Message ID: {messageId}");
                Console.WriteLine($"Data (Hex): {hexString}");
                Console.WriteLine("--------------------------\n");
            }

            if (messageId == 517)
            {
                TransferStruct.PlayerStateChange data = default(TransferStruct.PlayerStateChange);
                var deserializer = new PacketDeserializer(messageData);
                deserializer.Deserialize<TransferStruct.PlayerStateChange>(ref data);

                if (WebRtcVar.AitFunction != null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    if (data.State == 1)
                    {
                        Console.WriteLine($"玩家 {data.UserID} 加入了房间");
                        WebRtcVar.PlayerList.Add(data.UserID);
                    }
                    else
                    {
                        Console.WriteLine($"玩家 {data.UserID} 离开了房间");
                        WebRtcVar.PlayerList.Remove(data.UserID);
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            else if (messageId == 257)
            {
                TransferStruct.TransfetLoginResult data = default(TransferStruct.TransfetLoginResult);
                var deserializer = new PacketDeserializer(messageData);
                deserializer.Deserialize<TransferStruct.TransfetLoginResult>(ref data);
                if (data.Result == 255)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"中转服登录失败, 错误码: {data.Result}, 原因: 你未登录/你的账号在另一处登录/Logout");
                }
                else if (data.Result == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"中转服登录成功!");
                    try
                    {
                        WebRtcVar.PlayerList.Add(uint.Parse(aze<arg>.Instance.User.Id));
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
            {
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "TransferMessageRecv", data = new { messageId = messageId, messageData = messageData } }));
            }

            return processReceiveTransferMessage(messageId, messageData);
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
}