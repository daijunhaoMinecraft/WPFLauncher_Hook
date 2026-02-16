using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
                    Console.WriteLine($"LocalProxyListener started on port {port}");
                    while (_active) {
                        if (!_listener.Pending()) { Thread.Sleep(100); continue; }
                        TcpClient c = _listener.AcceptTcpClient();
                        WebRtcVar.Sessions["LOCAL_CLIENT"] = new UnifiedSession(c);
                    }
                } catch { }
            }) { IsBackground = true }.Start();
        }

        public static void Stop()
        {
            _active = false;
            _listener?.Stop();
        }
    }
}