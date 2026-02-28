using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using DotNetTranstor.Hookevent;
using Mcl.Core.DotNetTranstor.Hookevent;
using WPFLauncher.Model;
using WPFLauncher.SQLite;

namespace Mcl.Core.DotNetTranstor.Var
{
    public enum ForwardMode { None, Server, Client }

    public static class MultiplexProto
    {
        // 魔数握手：[0xFA, 0xFB, 0xFC, 0xFD, 0x01]
        // 客户端在 DataChannel Open 后第一时间发送此包
        public static readonly byte[] MagicHandshake = { 0xFA, 0xFB, 0xFC, 0xFD, 0x01 };
    }

    public class WebRtcVar
    {
        public static class ConnIdManager
        {
            private static HashSet<byte> _activeIds = new HashSet<byte>();
            private static object _lock = new object();

            public static byte Allocate()
            {
                lock (_lock)
                {
                    for (byte i = 0; i < 255; i++)
                    {
                        if (!_activeIds.Contains(i))
                        {
                            _activeIds.Add(i);
                            return i;
                        }
                    }
                    return 255; // 满了
                }
            }

            public static void Release(byte id)
            {
                lock (_lock)
                {
                    _activeIds.Remove(id);
                    if (Path_Bool.IsDebug)
                    {
                        Console.WriteLine($"[ConnManager] 已释放 ID: {id}, 当前活跃数: {_activeIds.Count}");
                    }
                }
            }
        }
        
        public static string Ip = "127.0.0.1";
        public static int Port = 25565;
        public static bool Enable = false;
        public static ForwardMode Mode = ForwardMode.None;
        
        public static object CmInstance = null;
        public static ait AitFunction = null;
        
        public static List<uint> PlayerList = new List<uint>();

        // Key: "PeerId_ConnId" (例如: "636067..._0", "636067..._1")
        public static ConcurrentDictionary<string, UnifiedSession> Sessions = new ConcurrentDictionary<string, UnifiedSession>();
        
        // 记录哪些 Peer 启用了多路复用模式
        public static ConcurrentDictionary<string, bool> PeerSupportMultiplex = new ConcurrentDictionary<string, bool>();

        public static string TargetPeerId = "Any";

        public static void InitForwarder()
        {
            Enable = true;
            if (Mode == ForwardMode.Client) Mcl.Core.LocalProxyListener.Start(Port);
            
            // 启动定时清理过期 Session 的任务
            StartCleanupTask();
        }

        public static void StopForwarder()
        {
            Enable = false;
            Mcl.Core.LocalProxyListener.Stop();
            foreach (var s in Sessions.Values) s.Close();
            Sessions.Clear();
            PeerSupportMultiplex.Clear();
            ClearActiveRoomsViaReflection();
            AitFunction.axy.e(null);
        }

        private static void StartCleanupTask()
        {
            new System.Threading.Thread(() => {
                while (Enable) {
                    System.Threading.Thread.Sleep(10000); // 每10秒检查一次
                    DateTime now = DateTime.Now;
                    foreach (var session in Sessions.Values) {
                        if ((now - session.LastActive).TotalSeconds > 60) {
                            Console.WriteLine($"[WebRtc] Session {session.PeerId}_{session.ConnId} 超时关闭");
                            session.Close();
                        }
                    }
                }
            }) { IsBackground = true }.Start();
        }

        // --- 以下保留你原有的反射辅助函数 ---
        public static bool IsNativeCompressionEnabled() {
            if (CmInstance == null) return false;
            try {
                var f = CmInstance.GetType().GetField("p", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                return (bool)f.GetValue(CmInstance);
            } catch { return false; }
        }

        public static IntPtr? getIntPtrFromPeerId(string peerId) {
            var list = getIntPtrList();
            if (list.TryGetValue(peerId, out IntPtr ptr)) return ptr;
            return null;
        }

        public static Dictionary<string, IntPtr> getIntPtrList() {
            if (CmInstance == null) return new Dictionary<string, IntPtr>();
            var fieldD = CmInstance.GetType().GetField("d", BindingFlags.Public | BindingFlags.Instance);
            return (Dictionary<string, IntPtr>)fieldD.GetValue(CmInstance);
        }

        public static av GetCompressor(IntPtr ptr) {
            var dictS = GetCompressListFunction();
            if (dictS.TryGetValue(ptr, out av compressor)) return compressor;
            return null;
        }

        public static Dictionary<IntPtr, av> GetCompressListFunction() {
            if (CmInstance == null) return new Dictionary<IntPtr, av>();
            var fieldS = CmInstance.GetType().GetField("s", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Dictionary<IntPtr, av>)fieldS.GetValue(CmInstance);
        }
        
        public static void ClearActiveRoomsViaReflection()
        {
            try
            {
                // 1. 获取 internal 类 'atn' 的 Type
                // 命名空间: WPFLauncher.Manager.LanGame
                // 类名: atn
                Type atnType = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // 尝试查找，ignoreCase=true 防止大小写问题
                    atnType = asm.GetType("WPFLauncher.Manager.LanGame.atn", false, true);
                    if (atnType != null) break;
                }

                if (atnType == null)
                {
                    Console.WriteLine("[Error] 未找到类型: WPFLauncher.Manager.LanGame.atn");
                    return;
                }

                // 2. 获取泛型单例类 'aze<>' 的定义
                // 命名空间: WPFLauncher.Common
                // 类名: aze`1 (注意反编译中泛型类通常显示为 name`1)
                Type azeOpenType = typeof(WPFLauncher.Common.aze<>); 
                // 如果 WPFLauncher.Common.aze<> 在当前程序集不可见（也是 internal），则同样需要用反射获取：
                // Type azeOpenType = Type.GetType("WPFLauncher.Common.aze`1, WPFLauncher"); 
                // 或者遍历程序集查找 "WPFLauncher.Common.aze`1"

                if (azeOpenType == null)
                {
                    Console.WriteLine("[Error] 未找到泛型类型: WPFLauncher.Common.aze`1");
                    return;
                }

                // 3. 动态构造泛型类型: aze<atn>
                Type azeClosedType = azeOpenType.MakeGenericType(atnType);

                // 4. 获取 'Instance' 静态属性
                // 根据提供的代码，Instance 是 public static 的
                PropertyInfo instanceProp = azeClosedType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);

                if (instanceProp == null)
                {
                    Console.WriteLine("[Error] 未找到 Instance 属性");
                    return;
                }

                // 5. 获取单例实例 (相当于 aze<atn>.Instance)
                object singleInstance = instanceProp.GetValue(null);

                if (singleInstance == null)
                {
                    Console.WriteLine("[Error] Instance 为 null");
                    return;
                }

                // 6. 获取 'ActiveRooms' 字段
                // 根据之前的片段: public alj ActiveRooms = new alj();
                // 它是实例字段 (Instance Field)
                FieldInfo activeRoomsField = azeClosedType.GetField("ActiveRooms", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                // 注意：这里有一个陷阱。
                // 泛型类 aze<cq> 的定义中可能并没有直接定义 ActiveRooms 字段。
                // 回顾你之前的片段：
                // "WPFLauncher.Manager.LanGame.atn ... public alj ActiveRooms = new alj();"
                // 这意味着 ActiveRooms 是定义在 'atn' 类里的，而不是 'aze' 类里的！
                // 逻辑链条是：aze<atn>.Instance 返回的是一个 'atn' 类型的对象。
                // 所以我们需要从 'atnType' 中获取 ActiveRooms 字段，而不是从 'azeClosedType' 获取。

                // 修正：从 atnType (即 cq 类型) 获取 ActiveRooms 字段
                FieldInfo roomsField = atnType.GetField("ActiveRooms", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (roomsField == null)
                {
                    Console.WriteLine("[Error] 在 atn 类中未找到 ActiveRooms 字段");
                    // 调试用：打印 atn 的所有字段
                    // Console.WriteLine("atn Fields: " + string.Join(", ", atnType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Select(f => f.Name)));
                    return;
                }

                // 7. 从单例实例 (它是 atn 类型) 中获取 ActiveRooms 的值
                object activeRoomsObj = roomsField.GetValue(singleInstance);

                if (activeRoomsObj == null)
                {
                    Console.WriteLine("[Warning] ActiveRooms 集合对象为 null");
                    return;
                }

                // 8. 调用 Clear() 方法
                MethodInfo clearMethod = activeRoomsObj.GetType().GetMethod("Clear");
                if (clearMethod != null)
                {
                    clearMethod.Invoke(activeRoomsObj, null);
                    Console.WriteLine("[Success] 成功清空 ActiveRooms!");
                }
                else
                {
                    Console.WriteLine("[Error] ActiveRooms 对象没有 Clear 方法");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Exception] {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[Inner] {ex.InnerException.Message}");
            }
        }
    }
}