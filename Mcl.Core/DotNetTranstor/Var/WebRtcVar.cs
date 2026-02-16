using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Mcl.Core.DotNetTranstor.Window;
using WPFLauncher.Manager.LanGame;
using WPFLauncher.Model;
using WPFLauncher.SQLite;

namespace Mcl.Core.DotNetTranstor.Var
{
    // 定义转发模式枚举
    public enum ForwardMode { None, Server, Client }

    // // 定义每个 Peer 的持久化上下文（用于维持 Zlib 字典）
    // public class PeerContext
    // {
    //     public av Compressor { get; } = new av();
    //     public string Id { get; set; }
    // }

    public class WebRtcVar
    {
        public static string Ip = "127.0.0.1";
        public static int Port = 25565;
        public static bool Enable = false;
        public static ForwardMode Mode = ForwardMode.None;
        
        public static ait AitFunction = null;
        public static object CmInstance = null;
        // public static ProcessTcpServer TcpServer = null;
        public static ForwarderControlPanel ControlPanel = new ForwarderControlPanel();


        private static string _targetPeerId = "Any";

        public static string TargetPeerId
        {
            get => _targetPeerId;
            set => _targetPeerId = value;
        }
        public static ConcurrentDictionary<string, UnifiedSession> Sessions = new ConcurrentDictionary<string, UnifiedSession>();

        public static void InitForwarder()
        {
            Enable = true;
            if (Mode == ForwardMode.Client) Mcl.Core.LocalProxyListener.Start(Port);
            // if (Mode == ForwardMode.Server)
            // {
            //     TcpServer = new ProcessTcpServer(WebRtcVar.Ip, WebRtcVar.Port);
            // }
        }

        public static void StopForwarder()
        {
            Enable = false;
            Mcl.Core.LocalProxyListener.Stop();
            // TcpServer.Stop();
            foreach (var s in Sessions.Values) s.Close();
            Sessions.Clear();
            _targetPeerId = "Any";
            CmInstance = null;
        }

        // 反射读取原生压缩开关
        public static bool IsNativeCompressionEnabled()
        {
            if (CmInstance == null) return false;
            try {
                var f = CmInstance.GetType().GetField("p", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                return (bool)f.GetValue(CmInstance);
            } catch { return false; }
        }

        public static Dictionary<string, IntPtr> getIntPtrList()
        {
            // 1. 直接获取 cm 实例（无需反射 WebRtcVar）
            object cmInstance = WebRtcVar.CmInstance;
            if (cmInstance == null)
            {
                Console.WriteLine("WebRtcVar.CmInstance is null");
                return new Dictionary<string, IntPtr>();
                // throw new InvalidOperationException("WebRtcVar.CmInstance is null");
            }

            // 2. 获取字段 'd' —— 注意：cm 是 internal 类，不能直接 cast，所以用反射
            Type cmType = cmInstance.GetType(); // 运行时类型就是 WebRtc.NET.cm

            FieldInfo fieldD = cmType.GetField("d", BindingFlags.Public | BindingFlags.Instance);
            if (fieldD == null)
            {
                Console.WriteLine("Field d 未找到");
                return new Dictionary<string, IntPtr>();
                // throw new MissingFieldException("Field 'd' not found in cm instance");
            }

            // 3. 读取字段值
            var dict = (Dictionary<string, IntPtr>)fieldD.GetValue(cmInstance);
            // Console.WriteLine($"字典长度：{dict.Count}");
            // // 4. 现在你可以使用这个字典了
            // foreach (var kvp in dict)
            // {
            //     Console.WriteLine($"Key: {kvp.Key}, IntPtr: {kvp.Value}");
            // }
            return dict;
        }

        public static Dictionary<IntPtr, av> GetCompressListFunction()
        {
            // 1. 获取 cm 实例（你已拥有）
            object cmInstance = WebRtcVar.CmInstance;
            if (cmInstance == null)
            {
                Console.WriteLine("WebRtcVar.CmInstance is null");
                return new Dictionary<IntPtr, av>();
            }

            // 2. 获取 cm 类型
            Type cmType = cmInstance.GetType(); // 应为 WebRtc.NET.cm

            // 3. 获取私有字段 's'
            FieldInfo fieldS = cmType.GetField("s", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldS == null)
            {
                Console.WriteLine("未能找到 cm 的私有字段 's'");
                return new Dictionary<IntPtr, av>();
            }

            // 4. 读取字段值
            var dictS = (Dictionary<IntPtr, av>)fieldS.GetValue(cmInstance);
            // 注意：av 是 internal 类，无法直接 cast，所以用 object 代替 value 类型
            
            return dictS;
        }

        public static IntPtr? getIntPtrFromPeerId(string peerId)
        {
            Dictionary<string, IntPtr> intPtrList = WebRtcVar.getIntPtrList();
            IntPtr intPtr = new IntPtr();
            intPtrList.TryGetValue(peerId, out intPtr);
            if (intPtr == new IntPtr(0))
            {
                Console.WriteLine("无法找到IntPtr");
                return null;
            }
            else
            {
                // Console.WriteLine($"成功找到IntPtr: {intPtr}");
                return intPtr;
            }
        }
        public static av GetCompressor(IntPtr ptr)
        {
            Dictionary<IntPtr, av> compressList = GetCompressListFunction();
            av compressor = null;
            compressList.TryGetValue(ptr, out compressor);
            if (compressor == null)
            {
                Console.WriteLine("无法获取到压缩函数");
            }
            // else
            // {
            //     Console.WriteLine("成功获取到压缩函数");
            // }

            return compressor;
        }
        
        public static bool ResetCompressor(IntPtr ptr)
        {
            object cmInstance = WebRtcVar.CmInstance;
            if (cmInstance == null)
            {
                Console.WriteLine("[ResetCompressor] CmInstance is null");
                return false;
            }

            var cmType = cmInstance.GetType();
            var fieldS = cmType.GetField("s", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldS == null)
            {
                Console.WriteLine("[ResetCompressor] Field 's' not found");
                return false;
            }

            object dictObj = fieldS.GetValue(cmInstance);
            if (dictObj == null)
            {
                Console.WriteLine("[ResetCompressor] s dictionary is null");
                return false;
            }

            // 反射调用 TryGetValue
            var tryGetValue = dictObj.GetType().GetMethod("TryGetValue");
            var args = new object[] { ptr, null };
            bool exists = (bool)tryGetValue.Invoke(dictObj, args);
            object oldAv = args[1];

            if (!exists || oldAv == null)
            {
                Console.WriteLine($"[ResetCompressor] No compressor for {ptr}");
                return false;
            }

            // 调用 oldAv.c()
            var cleanup = oldAv.GetType().GetMethod("c", BindingFlags.Public | BindingFlags.Instance);
            if (cleanup != null)
            {
                try { cleanup.Invoke(oldAv, null); }
                catch (Exception ex) { Console.WriteLine($"Cleanup error: {ex}"); }
            }

            // 创建新实例
            object newAv = Activator.CreateInstance(oldAv.GetType());
            if (newAv == null)
            {
                Console.WriteLine("[ResetCompressor] Failed to create new av");
                return false;
            }

            // 设置 dict[ptr] = newAv
            var indexer = dictObj.GetType().GetProperty("Item");
            indexer.SetValue(dictObj, newAv, new object[] { ptr });

            Console.WriteLine($"[ResetCompressor] Reset success for {ptr}");
            return true;
        }
    }
}