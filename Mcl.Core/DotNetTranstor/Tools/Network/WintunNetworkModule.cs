using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DotNetTranstor.Hookevent;
using Mcl.Core.DotNetTranstor.Var;

namespace Mcl.Core.DotNetTranstor.Tools.Network
{
    public class WintunRouterService
    {
        public static readonly WintunRouterService Instance = new WintunRouterService();

        private IntPtr _adapter = IntPtr.Zero;
        private IntPtr _session = IntPtr.Zero;
        private bool _isRunning = false;
        
        // 核心路由表：Key = 虚拟IP (如 10.8.0.2), Value = PeerId
        private ConcurrentDictionary<string, string> _routingTable = new ConcurrentDictionary<string, string>();
        
        // 本机的虚拟 IP，用于判断包是不是发给自己的
        public string LocalVirtualIp { get; private set; }

        #region Wintun P/Invoke (省略部分，参考前文)
        private const string WintunDll = "wintun.dll";
        [DllImport(WintunDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern IntPtr WintunCreateAdapter(string Name, string TunnelType, ref Guid RequestedGuid);
        [DllImport(WintunDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void WintunCloseAdapter(IntPtr Adapter);
        [DllImport(WintunDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr WintunStartSession(IntPtr Adapter, uint Capacity);
        [DllImport(WintunDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void WintunEndSession(IntPtr Session);
        [DllImport(WintunDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr WintunReceivePacket(IntPtr Session, out uint PacketSize);
        [DllImport(WintunDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void WintunReleaseReceivePacket(IntPtr Session, IntPtr Packet);
        [DllImport(WintunDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr WintunAllocateSendPacket(IntPtr Session, uint PacketSize);
        [DllImport(WintunDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void WintunSendPacket(IntPtr Session, IntPtr Packet);
        [DllImport(WintunDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr WintunGetReadWaitEvent(IntPtr Session);
        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
        #endregion

        public void Start(string virtualIp)
        {
            if (!Path_Bool.UseNetworkMode) return;
            this.LocalVirtualIp = virtualIp;

            Guid guid = Guid.NewGuid();
            _adapter = WintunCreateAdapter("MclVirtualNic", "Wintun", ref guid);
            ConfigureIp("MclVirtualNic", virtualIp, "255.255.255.0");
            _session = WintunStartSession(_adapter, 0x400000);
            _isRunning = true;

            // 启动网卡监听线程
            Task.Run(() => CaptureLoop());
            Console.WriteLine($"[Router] 虚拟网卡已启动，本机虚拟IP: {virtualIp}");
        }

        /// <summary>
        /// 处理从 WebRTC 接收到的数据包 (Hook_p 调用)
        /// </summary>
        public void OnDataReceived(string peerId, byte[] data)
        {
            // 1. 解析 IP 包头 (IPv4)
            if (data.Length < 20) return;
            
            string srcIp = $"{data[12]}.{data[13]}.{data[14]}.{data[15]}";
            string dstIp = $"{data[16]}.{data[17]}.{data[18]}.{data[19]}";

            // 2. 学习路由：记录这个 PeerId 对应的虚拟 IP
            _routingTable[srcIp] = peerId;

            // 3. 路由分发
            // if (dstIp == LocalVirtualIp)
            // {
            //     // 情况 A: 发给本机的包 -> 注入虚拟网卡
            //     WriteToNic(data);
            // }
            // else
            // {
            // 情况 B: 发给其他玩家的包 (转发)
            // if (WebRtcVar.Mode == ForwardMode.Server)
            // {
            //     if (_routingTable.TryGetValue(dstIp, out string targetPeerId))
            //     {
            //         if (Path_Bool.IsDebug) Console.WriteLine($"[Router] 转发: {srcIp} -> {dstIp} (Peer: {targetPeerId})");
            //         SendData(targetPeerId, data);
            //     }
            // }
            // else
            // {
                // 情况 A: 发给本机的包 -> 注入虚拟网卡
            WriteToNic(data);
            // }
            // }
        }

        /// <summary>
        /// 监听本地网卡发出的包 (准备发往远程)
        /// </summary>
        private void CaptureLoop()
        {
            IntPtr waitEvent = WintunGetReadWaitEvent(_session);
            while (_isRunning)
            {
                uint size;
                IntPtr packetPtr = WintunReceivePacket(_session, out size);
                if (packetPtr != IntPtr.Zero)
                {
                    byte[] packetData = new byte[size];
                    Marshal.Copy(packetPtr, packetData, 0, (int)size);
                    WintunReleaseReceivePacket(_session, packetPtr);

                    // 解析目的 IP
                    if (packetData.Length >= 20)
                    {
                        string dstIp = $"{packetData[16]}.{packetData[17]}.{packetData[18]}.{packetData[19]}";

                        if (WebRtcVar.Mode == ForwardMode.Client)
                        {
                            if (Path_Bool.IsDebug) Console.WriteLine($"[Router] 转发: {dstIp} -> {WebRtcVar.TargetPeerId}");
                            // 客户端很简单：所有包都扔给服务器 (假设服务器 PeerId 已知)
                            SendData(WebRtcVar.TargetPeerId, packetData);
                        }
                        else if (WebRtcVar.Mode == ForwardMode.Server)
                        {
                            // 服务端：根据目的 IP 找 PeerId
                            if (_routingTable.TryGetValue(dstIp, out string targetPeerId))
                            {
                                if (Path_Bool.IsDebug) Console.WriteLine($"[Router] 转发: {dstIp} -> {targetPeerId}");
                                SendData(targetPeerId, packetData);
                            }
                        }
                    }
                }
                else
                {
                    WaitForSingleObject(waitEvent, 10);
                }
            }
        }

        private void WriteToNic(byte[] data)
        {
            IntPtr sendPtr = WintunAllocateSendPacket(_session, (uint)data.Length);
            if (sendPtr != IntPtr.Zero)
            {
                Marshal.Copy(data, 0, sendPtr, data.Length);
                WintunSendPacket(_session, sendPtr);
            }
        }

        // 你提供的发送方法
        public static void SendData(string peerId, byte[] data)
        {
            if (WebRtcVar.CmInstance == null || string.IsNullOrEmpty(peerId)) return;
            IntPtr? peerPtr = WebRtcVar.getIntPtrFromPeerId(peerId);
            if (peerPtr == null) return;

            try {
                Assembly asm = WebRtcVar.CmInstance.GetType().Assembly;
                Type ateType = asm.GetType("WPFLauncher.Manager.LanGame.ate");
                MethodInfo sMethod = ateType.GetMethod("s", BindingFlags.Public | BindingFlags.Static);
                sMethod.Invoke(null, new object[] { peerPtr.Value, data, data.Length });
            } catch { }
        }

        private void ConfigureIp(string name, string ip, string mask)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"interface ip set address name=\"{name}\" static {ip} {mask} none",
                CreateNoWindow = true,
                UseShellExecute = false
            })?.WaitForExit();
        }
    }

    // --- Hook 部分 ---
    // public class WebSocket_WebRtc_ServerAware : IMethodHook
    // {
    //     [OriginalMethod]
    //     private static void Original_p(object target, string peerId, IntPtr dataPtr, int dataSize) { }
    //
    //     [HookMethod("WebRtc.NET.cm", "p", "Original_p")]
    //     private static void Hook_p(object target, string peerId, IntPtr dataPtr, int dataSize)
    //     {
    //         WebRtcVar.CmInstance = target;
    //         if (!WebRtcVar.Enable)
    //         {
    //             Original_p(target, peerId, dataPtr, dataSize);
    //             return;
    //         }
    //
    //         if (Path_Bool.UseNetworkMode)
    //         {
    //             // 收到数据，交给路由器模块处理 (包含路由转发和注入功能)
    //             byte[] rawData = new byte[dataSize];
    //             Marshal.Copy(dataPtr, rawData, 0, dataSize);
    //             WintunRouterService.Instance.OnDataReceived(peerId, rawData);
    //             return; // 跳过原有的端口转发解析
    //         }
    //
    //         // ... 传统的端口转发逻辑 ...
    //     }
    // }
}