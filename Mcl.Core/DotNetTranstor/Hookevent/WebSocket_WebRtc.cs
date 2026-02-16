using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DotNetTranstor.Hookevent;
using Mcl.Core.DotNetTranstor.Var;
using System.Reflection;
using DotNetTranstor;
using WPFLauncher.SQLite;

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
                        return;
                    }
                } catch (Exception ex) { Log($"[Error] Hook_p: {ex.Message}", ConsoleColor.Red); }
            }
            Original_p(target, peerId, dataPtr, dataSize);
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