using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DotNetTranstor.Hookevent;
using Mcl.Core.DotNetTranstor.Model;
using Mcl.Core.DotNetTranstor.Var;
using WebSocketSharp;
using WPFLauncher.Common;
using WPFLauncher.Manager;

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
            if (WebRtcVar.Mode == ForwardMode.Server)
            {
                // 1. 【修改点】使用 for 循环手动查找索引，替代 FindIndex
                // 因为 ObservableCollection<T> 没有 FindIndex 方法
                int index = -1;
                string targetUserId = aze<arg>.Instance.User.UserID.ToString();

                for (int i = 0; i < WebRtcVar.PlayerList.Count; i++)
                {
                    if (WebRtcVar.PlayerList[i].UserID == targetUserId)
                    {
                        index = i;
                        break;
                    }
                }

                // 2. 如果找到了有效索引 (-1 表示未找到)
                if (index != -1)
                {
                    // 3. 【关键】直接通过索引访问并修改列表中的元素
                    var item = WebRtcVar.PlayerList[index]; // 如果是 struct，这里取出的是副本

                    // 修改副本的属性
                    item.Status = "已连接";
                    item.PeerId = WebRtcVar.MyPeerId;
                    item.VirtualIp = virtualIp;

                    WebRtcVar.PlayerList[index] = item;
                    if (WebRtcVar.NetworkMonitor != null) WebRtcVar.NetworkMonitor.RefreshPlayerData();
    
                    // 可选：添加日志确认
                    // Console.WriteLine($"[成功] 用户 {targetUserId} 状态已更新为已连接");
                }
                else
                {
                    // 可选：调试用
                    // Console.WriteLine($"[警告] 未找到 UserID 为 {targetUserId} 的玩家");
                }
            }
            else
            {
                if (IPAddress.TryParse(virtualIp, out IPAddress ipAddr))
                {
                    byte[] ipBytes = ipAddr.GetAddressBytes();
    
                    // 定义头部
                    byte[] header = VirualIpProto.MagicHandshake.ToArray();
    
                    // 合并头部和 IP 字节数组
                    // 结果格式: [0x01, 0x02, 0x03, IP_Byte1, IP_Byte2, IP_Byte3, IP_Byte4]
                    byte[] sendData = header.Concat(ipBytes).ToArray();

                    SendData(WebRtcVar.TargetPeerId, sendData);
                    Console.WriteLine($"[Router] 发送虚拟IP: {virtualIp}");
                }
            }

            // 启动网卡监听线程
            Task.Run(() => CaptureLoop());
            Console.WriteLine($"[Router] 虚拟网卡已启动，本机虚拟IP: {virtualIp}");
        }

        public void SetRouting(string ip, string peerId)
        {
            try
            {
                _routingTable[ip] = peerId;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public void RemoveRouting(string peerId)
        {
            try
            {
                // 查找并移除与该 peerId 关联的路由条目
                var entry = _routingTable.FirstOrDefault(kvp => kvp.Value == peerId);
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    _routingTable.TryRemove(entry.Key, out _);
                    if (Path_Bool.IsDebug)
                    {
                        Console.WriteLine($"[Router] 已移除路由: {entry.Key} -> {peerId}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Router] 移除路由时出错: {e.Message}");
            }
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
                if (Path_Bool.IsDebug) Console.WriteLine($"[WebRtc]发送数据: {BitConverter.ToString(data)}");
                MethodInfo sMethod = ateType.GetMethod("s", BindingFlags.Public | BindingFlags.Static);
                sMethod.Invoke(null, new object[] { peerPtr.Value, data, data.Length });
            } catch { }
        }

        public void SendBoardCastData(byte[] data)
        {
            foreach (var route in this._routingTable)
            {
                SendData(route.Value, data);
            }
        }

        public void SendServerPlayerInfo(string peerId = "")
        {
            // 假设 PlayerList 已经填充了数据
            ObservableCollection<LanGamePlayerInfo> currentPlayers = WebRtcVar.PlayerList;

            // 调用工具生成字节包
            byte[] packetToSend = LanGameProtocolHelper.BuildPlayerListPacket(currentPlayers);
            if (peerId.IsNullOrEmpty())
            {
                // 发送给特定 peer 或 广播
                SendBoardCastData(packetToSend); 
                Console.WriteLine("[Router] 玩家列表已广播。");
            }
            else
            {
                // 发送给特定 peer 或 广播
                SendData(peerId, packetToSend);
                Console.WriteLine($"[Router] 玩家列表已发送给 {peerId}。");
            }
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
        
        public void Stop()
        {
            if (!_isRunning && _adapter == IntPtr.Zero)
            {
                Console.WriteLine("[Router] 服务未运行，无需停止。");
                return;
            }

            Console.WriteLine("[Router] 正在停止虚拟网卡服务...");

            // 1. 标记停止状态，终止 CaptureLoop 循环
            _isRunning = false;

            // 2. 清理 Wintun 会话 (Session)
            // 必须先结束会话，否则 CloseAdapter 可能会失败或挂起
            if (_session != IntPtr.Zero)
            {
                try
                {
                    WintunEndSession(_session);
                    Console.WriteLine("[Router] Wintun 会话已关闭。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Router] 关闭会话时出错: {ex.Message}");
                }
                _session = IntPtr.Zero;
            }

            // 3. 清理 Wintun 适配器 (Adapter)
            if (_adapter != IntPtr.Zero)
            {
                try
                {
                    WintunCloseAdapter(_adapter);
                    Console.WriteLine("[Router] 虚拟网卡适配器已移除。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Router] 关闭适配器时出错: {ex.Message}");
                    // 如果正常关闭失败，可能需要强制清理，但 WintunCloseAdapter 通常很可靠
                }
                _adapter = IntPtr.Zero;
            }

            // 4. 清理路由表
            _routingTable.Clear();

            // 5. 可选：清理 IP 配置 (将网卡设为 DHCP 或删除，防止残留静态 IP)
            // 注意：WintunCloseAdapter 通常会自动移除网卡，IP 配置随之消失。
            // 但如果网卡因异常未完全移除，可以尝试重置。
            try 
            {
                // 尝试将接口设为 DHCP (如果网卡还存在于系统中)
                // 这步是防御性的，通常不需要，因为网卡已经被 CloseAdapter 删除了
                // Process.Start("netsh", $"interface ip set address name=\"MclVirtualNic\" dhcp"); 
                Console.WriteLine("[Router] 网络配置清理完成。");
            }
            catch { }

            // 6. 重置本地状态
            LocalVirtualIp = string.Empty;
            WebRtcVar.Enable = false;
            
            // 7. 通知监控窗口或其他监听者 (如果需要)
            // 例如: OnServiceStopped?.Invoke();

            Console.WriteLine("[Router] 组网服务已完全停止。");
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