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
    public static bool Enable = false;
    public static ForwardMode Mode = ForwardMode.None;

    public static object CmInstance = null;
    public static string MyPeerId = string.Empty;
    public static ait LanGameManager = null;

    public static WintunRouterService WintunNetworkService = new();
    public static ConcurrentDictionary<string, UnifiedSession> Sessions = new();

    public static string TargetPeerId = "Any";
    
    // [修复] 替换为 WPF 窗口类
    public static NetworkMonitorWindow NetworkMonitor = null;

    public static ConcurrentDictionary<string, bool> PeerSupportMultiplex = new();
    public static string MyVirtualIp { get; set; } = string.Empty;

    public static ObservableCollection<LanGamePlayerInfo> PlayerList { get; set; } = new();

    public static void InitForwarder()
    {
        Enable = true;
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
        
        // [新增] 安全地跨线程关闭 WPF 监控窗口
        if (NetworkMonitor != null)
        {
            try
            {
                NetworkMonitor.Dispatcher.Invoke(() => NetworkMonitor.Close());
            }
            catch { /* 忽略已销毁的异常 */ }
            NetworkMonitor = null;
        }

        ClearActiveRoomsViaReflection();

        // [新增] 跨线程安全清理 List
        if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
            System.Windows.Application.Current.Dispatcher.Invoke(() => PlayerList.Clear());
        else
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
                foreach (var session in Sessions.Values)
                {
                    if ((now - session.LastActive).TotalSeconds > 60)
                    {
                        Console.WriteLine($"[WebRtc] Session {session.PeerId}_{session.ConnId} 超时关闭");
                        session.Close();
                    }
                }
            }
        }) { IsBackground = true }.Start();
    }

    public static bool IsNativeCompressionEnabled()
    {
        if (CmInstance == null) return false;
        try
        {
            var f = CmInstance.GetType().GetField("p", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return (bool)f.GetValue(CmInstance);
        }
        catch { return false; }
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

            if (atnType == null) return;

            var azeOpenType = typeof(azf<>);
            if (azeOpenType == null) return;

            var azeClosedType = azeOpenType.MakeGenericType(atnType);
            var instanceProp = azeClosedType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProp == null) return;

            var singleInstance = instanceProp.GetValue(null);
            if (singleInstance == null) return;

            var roomsField = atnType.GetField("ActiveRooms", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (roomsField == null) return;

            var activeRoomsObj = roomsField.GetValue(singleInstance);
            if (activeRoomsObj == null) return;

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
                {
                    if (!_activeIds.Contains(i))
                    {
                        _activeIds.Add(i);
                        return i;
                    }
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