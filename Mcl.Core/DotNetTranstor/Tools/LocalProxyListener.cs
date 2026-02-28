using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using DotNetTranstor.Hookevent;
using Mcl.Core.DotNetTranstor.Var;

namespace Mcl.Core
{
    public static class LocalProxyListener
    {
        private static TcpListener _listener;
        private static bool _active = false;

        public static void Start(int port)
        {
            _active = true;
            new Thread(() => {
                try {
                    _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                    _listener.Start();
                    Console.WriteLine($"[LocalProxy] 监听启动: {port}");
                    
                    while (_active) {
                        if (!_listener.Pending()) { Thread.Sleep(50); continue; }
                        TcpClient client = _listener.AcceptTcpClient();
                        
                        // 1. 分配 ID
                        byte connId = WebRtcVar.ConnIdManager.Allocate();
                        
                        // 2. 获取正确的 PeerId (优先使用 TargetPeerId)
                        string pid = WebRtcVar.TargetPeerId;

                        if (Path_Bool.IsDebug)
                        {
                            Console.WriteLine($"[LocalProxy] 新连接! ConnId={connId}, Target={pid}");
                        }

                        // 3. 必须确保发送握手（如果这个 Peer 还没标记为 Mux）
                        if (!WebRtcVar.PeerSupportMultiplex.ContainsKey(pid) || pid == "Any")
                        {
                             SendMultiplexHandshake(pid);
                        }

                        var session = new UnifiedSession(client, pid, connId);
                        WebRtcVar.Sessions[$"{pid}_{connId}"] = session;
                        // 为了兼容 Hook_p 中的查找
                        WebRtcVar.Sessions[$"LOCAL_CLIENT_{connId}"] = session;
                    }
                } catch { }
            }) { IsBackground = true }.Start();
        }

        private static void SendMultiplexHandshake(string pid)
        {
            IntPtr? ptr = WebRtcVar.getIntPtrFromPeerId(pid);
            if (ptr != null)
            {
                try {
                    var sMethod = WebRtcVar.CmInstance.GetType().Assembly
                        .GetType("WPFLauncher.Manager.LanGame.ate")
                        .GetMethod("s", BindingFlags.Public | BindingFlags.Static);
                    
                    sMethod.Invoke(null, new object[] { ptr.Value, MultiplexProto.MagicHandshake, MultiplexProto.MagicHandshake.Length });
                    WebRtcVar.PeerSupportMultiplex[pid] = true;
                    if (Path_Bool.IsDebug)
                    {
                        Console.WriteLine($"[LocalProxy] 成功向 {pid} 发送 Mux 握手信号");
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"[LocalProxy] 握手失败: {ex.Message}");
                }
            }
        }

        public static void Stop() => _active = false;
    }
}