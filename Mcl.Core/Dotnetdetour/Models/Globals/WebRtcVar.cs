using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Entities;
using Mcl.Core.Dotnetdetour.UI.Controls;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using WPFLauncher.Common;
using WPFLauncher.Model;

namespace Mcl.Core.Dotnetdetour.Models.Globals;

public enum ForwardMode
{
    None,
    Server,
    Client
}

public static class MultiplexProto
{
    public static readonly byte[] MagicHandshake = { 0xFA, 0xFB, 0xFC, 0xFD, 0x01 };
}

public static class VirualIpProto
{
    public static readonly byte[] MagicHandshake = { 0x01, 0x02, 0x03 };

    public static bool IsMagicHeader(byte[] bytes)
    {
        if (bytes == null || bytes.Length < MagicHandshake.Length) return false;
        for (var i = 0; i < MagicHandshake.Length; i++)
            if (bytes[i] != MagicHandshake[i])
                return false;
        return true;
    }
}

public static class GetPlayerListProto
{
    public static readonly byte[] MagicHandshake = { 0x0F, 0x0A, 0x0C, 0x0F };

    public static bool IsMagicHeader(byte[] bytes)
    {
        if (bytes == null || bytes.Length != MagicHandshake.Length) return false;
        for (var i = 0; i < MagicHandshake.Length; i++)
            if (bytes[i] != MagicHandshake[i])
                return false;
        return true;
    }
}

public class WebRtcVar
{
    public static string Ip = "127.0.0.1";
    public static int Port = 25565;
    public static bool Enable;
    public static ForwardMode Mode = ForwardMode.None;

    public static object CmInstance = null;
    public static string MyPeerId = string.Empty;
    public static ait LanGameManager = null;

    public static WintunRouterService WintunNetworkService = new();

    public static ConcurrentDictionary<string, UnifiedSession> Sessions = new();

    // [修改点 1] 显式声明私有静态字段 (.NET Framework 标准写法)

    public static string TargetPeerId = "Any";

    public static NetworkMonitorForm NetworkMonitor = new();

    public static ConcurrentDictionary<string, bool> PeerSupportMultiplex = new();

    public static string MyVirtualIp { get; set; } = string.Empty;

    // [修改点 2] 属性实现：移除 field 关键字，使用显式字段，并订阅内部事件
    public static ObservableCollection<LanGamePlayerInfo> PlayerList { get; set; } = new();

    public static void InitForwarder()
    {
        Enable = true;
        // // 确保初始化时就订阅事件 (以防万一属性 set 没走)
        // if (_playerList != null)
        // {
        //     _playerList.CollectionChanged -= PlayerList_CollectionChanged; // 防止重复
        //     _playerList.CollectionChanged += PlayerList_CollectionChanged;
        // }

        if (Mode == ForwardMode.Client) LocalProxyListener.Start(Port);
        StartCleanupTask();
    }

    public static void StopForwarder()
    {
        Enable = false;
        LocalProxyListener.Stop();
        foreach (var s in Sessions.Values) s.Close();
        Sessions.Clear();
        PeerSupportMultiplex.Clear();
        MyPeerId = string.Empty;
        NetworkMonitor = null;
        ClearActiveRoomsViaReflection();

        // Clear 会触发 CollectionChanged 事件，从而自动通知 UI
        PlayerList.Clear();

        if (LanGameManager != null && LanGameManager.aya != null) LanGameManager.aya.e(null);
    }

    private static void StartCleanupTask()
    {
        new Thread(() =>
        {
            while (Enable)
            {
                Thread.Sleep(10000);
                var now = DateTime.Now;
                // 注意：遍历 ConcurrentDictionary 是安全的，但关闭 Session 时要小心
                foreach (var session in Sessions.Values)
                    if ((now - session.LastActive).TotalSeconds > 60)
                    {
                        Console.WriteLine($"[WebRtc] Session {session.PeerId}_{session.ConnId} 超时关闭");
                        session.Close();
                    }
            }
        }) { IsBackground = true }.Start();
    }

    // --- 反射辅助函数保持不变 ---
    public static bool IsNativeCompressionEnabled()
    {
        if (CmInstance == null) return false;
        try
        {
            var f = CmInstance.GetType()
                .GetField("p", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return (bool)f.GetValue(CmInstance);
        }
        catch
        {
            return false;
        }
    }

    public static IntPtr? getIntPtrFromPeerId(string peerId)
    {
        var list = getIntPtrList();
        if (list.TryGetValue(peerId, out var ptr)) return ptr;
        return null;
    }

    public static Dictionary<string, IntPtr> getIntPtrList()
    {
        if (CmInstance == null) return new Dictionary<string, IntPtr>();
        var fieldD = CmInstance.GetType().GetField("d", BindingFlags.Public | BindingFlags.Instance);
        return (Dictionary<string, IntPtr>)fieldD.GetValue(CmInstance);
    }

    public static av GetCompressor(IntPtr ptr)
    {
        var dictS = GetCompressListFunction();
        if (dictS.TryGetValue(ptr, out var compressor)) return compressor;
        return null;
    }

    public static Dictionary<IntPtr, av> GetCompressListFunction()
    {
        if (CmInstance == null) return new Dictionary<IntPtr, av>();
        var fieldS = CmInstance.GetType().GetField("s", BindingFlags.NonPublic | BindingFlags.Instance);
        return (Dictionary<IntPtr, av>)fieldS.GetValue(CmInstance);
    }

    public static void ClearActiveRoomsViaReflection()
    {
        try
        {
            Type atnType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                atnType = asm.GetType("WPFLauncher.Manager.LanGame.atn", false, true);
                if (atnType != null) break;
            }

            if (atnType == null)
            {
                Console.WriteLine("[Error] 未找到类型：WPFLauncher.Manager.LanGame.atn");
                return;
            }

            var azeOpenType = typeof(azf<>);
            if (azeOpenType == null)
            {
                Console.WriteLine("[Error] 未找到泛型类型：WPFLauncher.Common.aze`1");
                return;
            }

            var azeClosedType = azeOpenType.MakeGenericType(atnType);
            var instanceProp = azeClosedType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);

            if (instanceProp == null)
            {
                Console.WriteLine("[Error] 未找到 Instance 属性");
                return;
            }

            var singleInstance = instanceProp.GetValue(null);
            if (singleInstance == null)
            {
                Console.WriteLine("[Error] Instance 为 null");
                return;
            }

            var roomsField = atnType.GetField("ActiveRooms",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (roomsField == null)
            {
                Console.WriteLine("[Error] 在 atn 类中未找到 ActiveRooms 字段");
                return;
            }

            var activeRoomsObj = roomsField.GetValue(singleInstance);
            if (activeRoomsObj == null)
            {
                Console.WriteLine("[Warning] ActiveRooms 集合对象为 null");
                return;
            }

            var clearMethod = activeRoomsObj.GetType().GetMethod("Clear");
            if (clearMethod != null)
            {
                clearMethod.Invoke(activeRoomsObj, null);
                Console.WriteLine("[Success] 成功清空 ActiveRooms!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Exception] {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"[Inner] {ex.InnerException.Message}");
        }
    }

    public static class ConnIdManager
    {
        private static readonly HashSet<byte> _activeIds = new();
        private static readonly object _lock = new();

        public static byte Allocate()
        {
            lock (_lock)
            {
                for (byte i = 1; i < 255; i++)
                    if (!_activeIds.Contains(i))
                    {
                        _activeIds.Add(i);
                        return i;
                    }

                return 255;
            }
        }

        public static void Release(byte id)
        {
            lock (_lock)
            {
                _activeIds.Remove(id);
                if (WpfConfig.IsDebug) Console.WriteLine($"[ConnManager] 已释放 ID: {id}, 当前活跃数: {_activeIds.Count}");
            }
        }
    }
}