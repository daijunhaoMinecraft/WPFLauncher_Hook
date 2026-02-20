// using System;
// using System.Runtime.InteropServices;
// using DotNetTranstor;
// using DotNetTranstor.Hookevent;
// using Mcl.Core.DotNetTranstor.Var;
// using Newtonsoft.Json;
//
// namespace Mcl.Core;
//
// public class WebSocket_WebRtc : IMethodHook
// {
//     [OriginalMethod]
//     private void Original_WebSocket_WebRtc_OnMessage(string Message)
//     {
//     }
//
//     [HookMethod("WebRtc.NET.cm", "f", "Original_WebSocket_WebRtc_OnMessage")]
//     private void WebSocket_OnMessage(string Message)
//     {
//         Console.ForegroundColor = ConsoleColor.Green;
//         Console.WriteLine($"[WebSocket_WebRtc]收到消息:{Message}");
//         Console.ForegroundColor = ConsoleColor.White;
//         Original_WebSocket_WebRtc_OnMessage(Message);
//     }
//     [OriginalMethod]
//     private void Original_WebSocket_WebRtc_SendMessage(string Message)
//     {
//     }
//     [HookMethod("WebRtc.NET.cm", "h", "Original_WebSocket_WebRtc_SendMessage")]
//     private void WebSocket_SendMessage(string Message)
//     {
//         Console.ForegroundColor = ConsoleColor.Green;
//         Console.WriteLine($"[WebSocket_WebRtc]发送消息:{Message}");
//         Console.ForegroundColor = ConsoleColor.White;
//         Original_WebSocket_WebRtc_SendMessage(Message);
//     }
//     
//     
//     // ====================================================================
//     // 1. Hook WebRTC 数据接收
//     // 目标: WebRtc.NET.cm.p(string, IntPtr, int)
//     // ====================================================================
//
//     [OriginalMethod]
//     private void Original_OnWebRtcDataReceived(string peerId, IntPtr dataPtr, int dataSize)
//     {
//         // 这个方法体是空的，因为它会被Hook框架替换为对原始方法的调用。
//     }
//
//     [HookMethod("WebRtc.NET.cm", "p", "Original_OnWebRtcDataReceived")]
//     private void Hook_OnWebRtcDataReceived(string peerId, IntPtr dataPtr, int dataSize)
//     {
//         // try
//         // {
//         //     // 提取数据
//         //     byte[] receivedData = new byte[aqp];
//         //     Marshal.Copy(aqo, receivedData, 0, aqp);
//         //     if (Path_Bool.IsDebug)
//         //     {
//         //         // 打印日志
//         //         Console.ForegroundColor = ConsoleColor.Cyan; // 接收用青色
//         //         Console.WriteLine("--- [WebRTC <<< RECV] ---");
//         //         Console.WriteLine($"From Peer ID: {aqn}");
//         //         Console.WriteLine($"Data Size: {aqp} bytes");
//         //     
//         //         string hexString = BitConverter.ToString(receivedData).Replace("-", " ");
//         //         Console.WriteLine($"Data (Hex): {hexString}");
//         //         Console.WriteLine("--------------------------\n");
//         //         Console.ForegroundColor = ConsoleColor.White;
//         //     }
//         // }
//         // catch (Exception ex)
//         // {
//         //     Console.WriteLine($"[Hook Error] in OnWebRtcDataReceived: {ex.Message}");
//         // }
//         //     // 调用原始方法，确保游戏逻辑继续正常运行
//         //     Original_OnWebRtcDataReceived(aqn, aqo, aqp);
//         // }
//         try
//         {
//             // 提取数据
//             byte[] receivedData = new byte[dataSize];
//             Marshal.Copy(dataPtr, receivedData, 0, dataSize);
//             
//             // 日志输出
//             if (Path_Bool.IsDebug)
//             {
//                 Console.ForegroundColor = ConsoleColor.Cyan; // 接收用青色
//                 Console.WriteLine("--- [WebRTC <<< RECV] ---");
//                 Console.WriteLine($"From Peer ID: {peerId}");
//                 Console.WriteLine($"Data Size: {dataSize} bytes");
//                 
//                 string hexString = BitConverter.ToString(receivedData).Replace("-", " ");
//                 Console.WriteLine($"Data (Hex): {hexString}");
//                 Console.WriteLine("--------------------------\n");
//                 Console.ForegroundColor = ConsoleColor.White;
//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"[Hook Error] in OnWebRtcDataReceived: {ex.Message}");
//         }
//
//         // 调用原始方法，确保游戏逻辑继续正常运行
//         Original_OnWebRtcDataReceived(peerId, dataPtr, dataSize);
//     }
//
//     // ====================================================================
//     // 2. Hook WebRTC 数据发送
//     // 目标: WebRtc.NET.cm.ai(IntPtr, byte[])
//     // ====================================================================
//     
//     [OriginalMethod]
//     private bool Original_SendDataInternal(IntPtr peerHandle, byte[] data)
//     {
//         // 空方法体，会被框架替换
//         return false; // 返回值需要匹配原方法
//     }
//
//     [HookMethod("WebRtc.NET.cm", "ai", "Original_SendDataInternal")]
//     private bool Hook_SendDataInternal(IntPtr arm, byte[] arn)
//     {
//         try
//         {
//             // // 尝试从 cm 实例中找到 Peer ID
//             // string peerId = "Unknown";
//             // // __instance 是Hook框架注入的对 cm 实例的引用
//             // if (__instance != null && __instance.d != null)
//             // {
//             //     foreach (var entry in __instance.d) // 遍历 d (peerConnections) 字典
//             //     {
//             //         if (entry.Value == arm)
//             //         {
//             //             peerId = entry.Key;
//             //             break;
//             //         }
//             //     }
//             // }
//             if (Path_Bool.IsDebug)
//             {
//                 // 打印日志
//                 Console.ForegroundColor = ConsoleColor.Yellow; // 发送用黄色
//                 Console.WriteLine("--- [WebRTC >>> SEND] ---");
//                 Console.WriteLine($"Handle: {arm}");
//                 Console.WriteLine($"Data Size: {arn.Length} bytes");
//
//                 string hexString = BitConverter.ToString(arn).Replace("-", " ");
//                 Console.WriteLine($"Data (Hex): {hexString}");
//                 byte[] compressData = new av().a(arn);
//                 Console.WriteLine($"Data (Compress): {BitConverter.ToString(compressData).Replace("-", " ")}");
//                 Console.WriteLine("--------------------------\n");
//                 Console.ForegroundColor = ConsoleColor.White;
//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"[Hook Error] in SendDataInternal: {ex.Message}");
//         }
//
//         // 调用原始方法，真正地把数据发送出去
//         return Original_SendDataInternal(arm, arn);
//     }
//     
//     [OriginalMethod]
//     public bool sendTransferMessage(params object[] ObjectMessage)
//     {
//         return true;
//     }
//     
//     [HookMethod("WPFLauncher.Manager.LanGame.atm","as","sendTransferMessage")]
//     public bool SendTransferMessage_HookMethod(params object[] ObjectMessage)
//     {
//         Console.ForegroundColor = ConsoleColor.Yellow; // 发送用黄色
//         Console.WriteLine("--- [TransferServer >>> SEND] ---");
//         Console.WriteLine(JsonConvert.SerializeObject(ObjectMessage));
//         Console.WriteLine("--------------------------\n");
//         return sendTransferMessage(ObjectMessage);
//     }
//
//     [OriginalMethod]
//     private bool processReceiveTransferMessage(ushort messageId, byte[] messageData)
//     {
//         return true;
//     }
//     
//     [HookMethod("WPFLauncher.Network.TransService.ace","b","processReceiveTransferMessage")]
//     private bool processReceiveTransferMessage_HookMethod(ushort messageId, byte[] messageData)
//     {
//         Console.ForegroundColor = ConsoleColor.Cyan;
//         Console.WriteLine("--- [TransferServer <<< RECV] ---");
//         string hexString = BitConverter.ToString(messageData).Replace("-", " ");
//         Console.WriteLine($"Message ID: {messageId}");
//         Console.WriteLine($"Data (Hex): {hexString}");
//         Console.WriteLine("--------------------------\n");
//         return processReceiveTransferMessage(messageId, messageData);
//     }
//
//     [OriginalMethod]
//     public void handleReceiveLauncherMessage(ushort messageId, byte[] messageData)
//     {
//     }
//     
//     [HookMethod("WPFLauncher.Network.acb","b","handleReceiveLauncherMessage")]
//     public void handleReceiveLauncherMessage_HookMethod(ushort messageId, byte[] messageData)
//     {
//         Console.ForegroundColor = ConsoleColor.Cyan;
//         Console.WriteLine("--- [Launcher <<< RECV] ---");
//         string hexString = BitConverter.ToString(messageData).Replace("-", " ");
//         Console.WriteLine($"Message ID: {messageId}");
//         Console.WriteLine($"Data (Hex): {hexString}");
//         Console.WriteLine("--------------------------\n");
//         handleReceiveLauncherMessage(messageId, messageData);
//     }
//     [OriginalMethod]
//     private void SendLauncherMessage(byte[] messageData)
//     {
//     }
//     
//     [HookMethod("WPFLauncher.Model.ait","as","SendLauncherMessage")]
//     private void SendLauncherMessage_HookMethod(byte[] messageData)
//     {
//         Console.ForegroundColor = ConsoleColor.Cyan;
//         Console.WriteLine("--- [Launcher <<< SEND] ---");
//         string hexString = BitConverter.ToString(messageData).Replace("-", " ");
//         Console.WriteLine($"Data (Hex): {hexString}");
//         Console.WriteLine("--------------------------\n");
//         SendLauncherMessage(messageData);
//     }
// }
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DotNetTranstor.Hookevent;
using Mcl.Core.DotNetTranstor.Var;
using System.Reflection;
using System.Windows.Forms;
using DotNetTranstor;
using Mcl.Core.DotNetTranstor.Model;
using Newtonsoft.Json;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.LanGame;
using WPFLauncher.Model;
using WPFLauncher.SQLite;
using WPFLauncher.Util;

namespace Mcl.Core
{
    public class WebSocket_WebRtc : IMethodHook
    {
        public WebSocket_WebRtc() { }

        [OriginalMethod]
        private static void Original_p(object target, string peerId, IntPtr dataPtr, int dataSize) { }

        // [OriginalMethod]
        // public static bool Original_aj(object target, byte[] data, string peerId) => false;

        [OriginalMethod]
        private static void Original_ctor(object target, long app, string apq, bool apr) { }

        [HookMethod("WebRtc.NET.cm", ".ctor", "Original_ctor")]
        private static void Hook_ctor(object target, long app, string apq, bool apr)
        {
            WebRtcVar.CmInstance = target;
            // WebRtcVar.IsCompression = apr;
            Log($"[System] 捕获到 cm 实例! Hash:{target.GetHashCode():X}, 是否压缩: {apr}", ConsoleColor.Magenta);
            Original_ctor(target, app, apq, apr);
        }

        [HookMethod("WebRtc.NET.cm", "p", "Original_p")]
        private static void Hook_p(object target, string peerId, IntPtr dataPtr, int dataSize)
        {
            WebRtcVar.CmInstance = target;
            // if (peerId != "Any") WebRtcVar.TargetPeerId = peerId;
            WebRtcVar.TargetPeerId = peerId;

            if (WebRtcVar.Enable && dataSize > 0)
            {
                try {
                    byte[] rawData = new byte[dataSize];
                    Marshal.Copy(dataPtr, rawData, 0, dataSize);
                    
                    // bool nativeCmp = WebRtcVar.IsNativeCompressionEnabled();
                    // bool hasZlibHead = IsZlib(rawData);

                    // Log($"[RECV] WebRTC->MC | Peer:{peerId} | Len:{dataSize} | NativeP:{nativeCmp} | Zlib:{hasZlibHead}", ConsoleColor.Cyan);
                    // Log($"[RECV] WebRTC->MC | Peer:{peerId} | Len:{dataSize} | NativeP:{nativeCmp}", ConsoleColor.Cyan);
                    bool nativeZlibCompression = WebRtcVar.IsNativeCompressionEnabled();
                    if (Path_Bool.IsDebug)
                    {
                        Log($"[RECV] WebRTC->MC | Peer:{peerId} | Len:{dataSize} | Data: {BitConverter.ToString(rawData).Replace("-", " ")} | nativeZlibCompression: {nativeZlibCompression}", ConsoleColor.Cyan);
                    }
                    
                    byte[] processedData = rawData;
                    // if (WebRtcVar.IsCompression && nativeCmp)
                    // if (WebRtcVar.IsCompression)
                    if (nativeZlibCompression)
                    {
                        // GetIntPtr
                        IntPtr? peerPtr = WebRtcVar.getIntPtrFromPeerId(peerId);
                        if (peerPtr == null)
                        {
                            Console.WriteLine($"[WebRTC] 无法获取到IntPtr, peerId: {peerId}");
                        }
                        // getCompressFunction
                        av compress = WebRtcVar.GetCompressor(peerPtr.Value);
                        if (compress == null)
                        {
                            Console.WriteLine($"[WebRTC] 获取压缩器失败, peerId: {peerId}");
                        }
                        processedData = compress.b(rawData);
                        // Log("--------------------------", ConsoleColor.DarkCyan);
                        // Log($"[Zlib] 解压成功 {rawData.Length} -> {processedData.Length}", ConsoleColor.DarkCyan);
                        // Log($"Original: {BitConverter.ToString(rawData).Replace("-", " ")}", ConsoleColor.DarkCyan);
                        // Log($"Processed: {BitConverter.ToString(processedData).Replace("-", " ")}", ConsoleColor.DarkCyan);
                        // Log("--------------------------", ConsoleColor.DarkCyan);
                        // var ctx = WebRtcVar.Contexts.GetOrAdd(peerId, _ => new PeerContext());
                        // processedData = ctx.Compressor.b(rawData);
                        // Log($"   [Zlib] 解压成功 {rawData.Length} -> {processedData.Length}", ConsoleColor.DarkCyan);
                    }
                    UnifiedSession session = null;
                    if (WebRtcVar.Mode == ForwardMode.Server)
                    {
                        // session = WebRtcVar.Sessions.GetOrAdd(peerId, id => new ProcessTcpServer(id));
                        session = WebRtcVar.Sessions.GetOrAdd(peerId, id => new UnifiedSession(id));
                        // Console.WriteLine("创建与服务端连接成功!");
                    }
                    else
                        WebRtcVar.Sessions.TryGetValue("LOCAL_CLIENT", out session);
                    
                    if (session != null) {
                        session.SendToSocket(processedData);
                        // WebRtcVar.TotalBytesTransferred += processedData.Length;
                        return;
                    }
                } catch (Exception ex) { Log($"[Error] Hook_p: {ex.Message}", ConsoleColor.Red); }
            }
            Original_p(target, peerId, dataPtr, dataSize);
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

        // [HookMethod("WebRtc.NET.cm", "aj", "Original_aj")]
        // private static bool Hook_aj(object target, byte[] data, string peerId)
        // {
        //     WebRtcVar.CmInstance = target;
        //     return Original_aj(target, data, peerId);
        // }

        public static void SendBack(byte[] data, string peerId)
        {
            if (WebRtcVar.CmInstance == null) return;
            // string finalPid = (!string.IsNullOrEmpty(WebRtcVar.TargetPeerId)) ? WebRtcVar.TargetPeerId : peerId;
            // if (string.IsNullOrEmpty(finalPid) || finalPid == "Any") return;
            if (Path_Bool.IsDebug)
            {
                Log(
                    $"[SEND] MC->WebRTC | Peer:{peerId} | Len:{data.Length} | Data: {BitConverter.ToString(data).Replace("-", " ")}",
                    ConsoleColor.Green);
            }

            // GetIntPtr
            IntPtr? peerPtr = WebRtcVar.getIntPtrFromPeerId(peerId);
            if (peerPtr == null)
            {
                Console.WriteLine($"[WebRTC] 无法获取到IntPtr, peerId: {peerId}");
            }
            // getCompressFunction
            av compress = WebRtcVar.GetCompressor(peerPtr.Value);
            if (WebRtcVar.IsNativeCompressionEnabled())
            {
                // int OriginalLength = data.Length;
                data = compress.a(data);
                // Console.WriteLine($"[WebRtc] 压缩成功, data Length: {OriginalLength.ToString()} -> {data.Length.ToString()}");
            }
            
            // Get ate
            object cmInstance = WebRtcVar.CmInstance;
            if (cmInstance == null)
            {
                Console.WriteLine("CmInstance is null");
                return;
            }

            Assembly asm = cmInstance.GetType().Assembly;
            Type ateType = asm.GetType("WPFLauncher.Manager.LanGame.ate");

            if (ateType == null)
            {
                // 再次模糊匹配（以防万一）
                ateType = asm.GetTypes()
                    .FirstOrDefault(t => t.Name == "ate" && t.Namespace == "WPFLauncher.Manager.LanGame");
            }

            if (ateType == null)
            {
                Console.WriteLine("仍然找不到 ate 类型！");
                return;
            }
            
            
            // 2. 获取 s 方法
            MethodInfo sMethod = ateType.GetMethod("s", BindingFlags.Public | BindingFlags.Static,
                null, new Type[] { typeof(IntPtr), typeof(byte[]), typeof(int) }, null);
            if (sMethod == null)
            {
                Console.WriteLine("无法找到 s 方法");
                return;
            }
            
            object result = sMethod.Invoke(null, new object[] { peerPtr.Value, data, data.Length });
            bool success = (bool)result;
            if (!success && Path_Bool.IsDebug)
            {
                Console.WriteLine("[WebRTC] 发送数据失败");
            }
            // WebRtcVar.TotalBytesTransferred += data.Length;
            // Console.WriteLine($"IsSuccess: {success}");

            // Original_aj(WebRtcVar.CmInstance, data, finalPid);
        }
        private static void Log(string m, ConsoleColor c) {
            Console.ForegroundColor = c;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {m}");
            Console.ResetColor();
        }
    }
}