using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using Mcl.Core.DotNetTranstor.Var;

namespace Mcl.Core
{
    public class UnifiedSession
    {
        public string PeerId { get; set; }
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        public av Compressor { get; } = new av();
        private bool _isClosing = false;
        private readonly ConcurrentQueue<byte[]> _sendQueue = new ConcurrentQueue<byte[]>();
        private bool _isConnected = false; // 标记是否已成功连接

        // 构造函数 1：从已有 TcpClient 创建（Server 模式）
        public UnifiedSession(TcpClient c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            _tcpClient = c;
            _stream = c.GetStream();
            this.PeerId = "Any";
            _isConnected = true;
            Console.WriteLine($"[UnifiedSession] 接受新客户端连接，PeerId 设为 'Any'");

            new Thread(StartReadLoop) { IsBackground = true }.Start();
        }

        // 构造函数 2：主动连接到本地 MC 服务器（Client/Forward 模式）
        public UnifiedSession(string peerId)
        {
            this.PeerId = peerId ?? "null";
            Console.WriteLine($"[UnifiedSession] 尝试为 PeerId '{this.PeerId}' 建立到本地 MC 服务器的连接 ({WebRtcVar.Ip}:{WebRtcVar.Port})");

            new Thread(() =>
            {
                try
                {
                    _tcpClient = new TcpClient();
                    _tcpClient.Connect(WebRtcVar.Ip, WebRtcVar.Port);
                    _stream = _tcpClient.GetStream();
                    _isConnected = true;

                    Console.WriteLine($"[UnifiedSession] 成功连接到本地 MC 服务器，PeerId: {this.PeerId}");

                    // 连接成功后，立即发送队列中的数据
                    FlushSendQueue();

                    StartReadLoop();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UnifiedSession] 连接本地 MC 服务器失败 (PeerId: {this.PeerId}): {ex.Message}");
                    Close();
                }
            }) { IsBackground = true }.Start();
        }

        private void FlushSendQueue()
        {
            Console.WriteLine($"[UnifiedSession] 开始刷新发送队列 (PeerId: {PeerId})");
            int count = 0;
            while (_sendQueue.TryDequeue(out byte[] data))
            {
                if (!_isClosing && _stream != null && _tcpClient?.Connected == true)
                {
                    try
                    {
                        _stream.Write(data, 0, data.Length);
                        _stream.Flush();
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[UnifiedSession] 刷新队列时发送失败 (PeerId: {PeerId}): {ex.Message}");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"[UnifiedSession] 刷新中断：连接已关闭 (PeerId: {PeerId})");
                    break;
                }
            }
            Console.WriteLine($"[UnifiedSession] 发送队列刷新完成，共发送 {count} 条消息 (PeerId: {PeerId})");
        }

        private void StartReadLoop()
        {
            byte[] buf = new byte[65536];
            Console.WriteLine($"[UnifiedSession] 开始读取循环 (PeerId: {PeerId})");

            try
            {
                while (!_isClosing && _tcpClient?.Connected == true)
                {
                    int bytesRead = _stream.Read(buf, 0, buf.Length);
                    if (bytesRead > 0)
                    {
                        byte[] data = new byte[bytesRead];
                        Buffer.BlockCopy(buf, 0, data, 0, bytesRead);
                        // Console.WriteLine($"[UnifiedSession] 从 TCP 读取 {bytesRead} 字节 (PeerId: {PeerId})，转发至 WebRTC");
                        WebSocket_WebRtc.SendBack(data, this.PeerId);
                    }
                    else
                    {
                        Console.WriteLine($"[UnifiedSession] 对端关闭连接 (PeerId: {PeerId})");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_isClosing)
                {
                    Console.WriteLine($"[UnifiedSession] 读取时发生异常 (PeerId: {PeerId}): {ex.Message}");
                }
            }
            finally
            {
                Console.WriteLine($"[UnifiedSession] 准备关闭会话 (PeerId: {PeerId})");
                Close();
            }
        }

        public void SendToSocket(byte[] data)
        {
            if (data == null)
            {
                Console.WriteLine($"[UnifiedSession] 警告：尝试发送 null 数据 (PeerId: {PeerId})");
                return;
            }

            if (_isClosing)
            {
                Console.WriteLine($"[UnifiedSession] 会话已关闭，丢弃 {data.Length} 字节数据 (PeerId: {PeerId})");
                return;
            }

            // 如果尚未连接成功，先入队
            if (!_isConnected)
            {
                _sendQueue.Enqueue(data);
                Console.WriteLine($"[UnifiedSession] 连接未就绪，暂存 {data.Length} 字节到队列 (PeerId: {PeerId})");
                return;
            }

            // 已连接，直接发送
            try
            {
                if (_stream != null && _tcpClient?.Connected == true)
                {
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                    // Console.WriteLine($"[UnifiedSession] 成功发送 {data.Length} 字节到 TCP (PeerId: {PeerId})");
                }
                else
                {
                    // 理论上不会走到这里，但保险起见
                    _sendQueue.Enqueue(data);
                    Console.WriteLine($"[UnifiedSession] 连接意外断开，暂存 {data.Length} 字节到队列 (PeerId: {PeerId})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnifiedSession] 发送数据失败 (PeerId: {PeerId}): {ex.Message}");
                // 可选：触发 Close()
                // Close();
            }
        }

        public void Close()
        {
            if (_isClosing)
            {
                Console.WriteLine($"[UnifiedSession] Close() 已调用，跳过重复操作 (PeerId: {PeerId})");
                return;
            }

            _isClosing = true;
            _isConnected = false;
            Console.WriteLine($"[UnifiedSession] 开始关闭会话 (PeerId: {PeerId})");

            // 清空队列（可选：也可以选择丢弃）
            int queued = 0;
            while (_sendQueue.TryDequeue(out _)) queued++;
            if (queued > 0)
            {
                Console.WriteLine($"[UnifiedSession] 关闭时丢弃队列中 {queued} 条未发送消息 (PeerId: {PeerId})");
            }

            // 清理网络资源
            try
            {
                _stream?.Close();
                _stream?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnifiedSession] 关闭 Stream 时出错 (PeerId: {PeerId}): {ex.Message}");
            }

            try
            {
                _tcpClient?.Close();
                _tcpClient?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnifiedSession] 关闭 TcpClient 时出错 (PeerId: {PeerId}): {ex.Message}");
            }

            _stream = null;
            _tcpClient = null;

            // 从全局 Sessions 中移除
            try
            {
                if (WebRtcVar.Mode == ForwardMode.Server)
                {
                    bool removed = WebRtcVar.Sessions.TryRemove(PeerId, out _);
                    Console.WriteLine($"[UnifiedSession] 从 Sessions 移除 PeerId '{PeerId}': {(removed ? "成功" : "未找到")}");
                }
                else
                {
                    bool removed = WebRtcVar.Sessions.TryRemove("Any", out _);
                    Console.WriteLine($"[UnifiedSession] 从 Sessions 移除 'Any': {(removed ? "成功" : "未找到")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnifiedSession] 移除 Sessions 时出错 (PeerId: {PeerId}): {ex.Message}");
            }

            Console.WriteLine($"[UnifiedSession] 会话已完全关闭 (PeerId: {PeerId})");
        }
    }
}