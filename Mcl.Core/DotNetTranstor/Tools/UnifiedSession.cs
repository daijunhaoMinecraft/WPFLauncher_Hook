using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using DotNetTranstor.Hookevent;
using Mcl.Core.DotNetTranstor.Var;

namespace Mcl.Core
{
    public class UnifiedSession
    {
        public string PeerId { get; }
        public byte ConnId { get; }
        public DateTime LastActive { get; private set; }
        
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private volatile bool _isClosing = false;
        private volatile bool _isConnected = false;
        private readonly object _connectLock = new object();
        private readonly ConcurrentQueue<byte[]> _sendQueue = new ConcurrentQueue<byte[]>();
        private readonly string _logPrefix;

        // ========== 构造函数 (传入已连接的TcpClient) ==========
        public UnifiedSession(TcpClient c, string peerId, byte connId)
        {
            _tcpClient = c;
            _stream = c.GetStream();
            this.PeerId = peerId;
            this.ConnId = connId;
            this.LastActive = DateTime.Now;
            _logPrefix = $"[Session:{PeerId}_{ConnId}]";
            Log("=== 构造函数 === 已传入TcpClient，初始化完成");
            _isConnected = true;
            new Thread(StartReadLoop) { IsBackground = true, Name = $"ReadLoop-{PeerId}_{ConnId}" }.Start();
            Log("读取循环线程已启动");
        }

        // ========== 构造函数 (需要异步连接本地MC) ==========
        public UnifiedSession(string peerId, byte connId)
        {
            this.PeerId = peerId;
            this.ConnId = connId;
            this.LastActive = DateTime.Now;
            _logPrefix = $"[Session:{PeerId}_{ConnId}]";
            Log("=== 构造函数 === 将异步连接本地MC服务器");

            new Thread(ConnectToLocalMc) { IsBackground = true, Name = $"Connect-{PeerId}_{ConnId}" }.Start();
        }

        // ========== 日志辅助方法 ==========
        private void Log(string message)
        {
            if (Path_Bool.IsDebug)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_logPrefix} {message}");
            }
        }

        private void LogError(string message, Exception ex = null)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_logPrefix} [ERROR] {message}");
            if (ex != null)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_logPrefix} [ERROR] 异常详情: {ex}");
            }
        }

        // ========== 连接本地MC服务器 ==========
        private void ConnectToLocalMc()
        {
            Log($"开始连接本地MC: {WebRtcVar.Ip}:{WebRtcVar.Port}");
            try 
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(WebRtcVar.Ip, WebRtcVar.Port);
                _stream = _tcpClient.GetStream();
                
                Log("TCP连接建立成功");
                
                lock (_connectLock)
                {
                    _isConnected = true;
                }
                
                Log("开始刷新发送队列（保证FIFO顺序）...");
                int flushedCount = FlushSendQueue();
                Log($"发送队列刷新完成，共处理 {flushedCount} 条待发送数据");
                
                // 启动读取循环（当前线程继续执行，不会阻塞）
                Log("启动读取循环");
                StartReadLoop();
            }
            catch (Exception ex) 
            {
                LogError($"连接本地MC失败", ex);
                Close();
            }
        }

        // ========== 刷新发送队列（返回实际发送数量） ==========
        private int FlushSendQueue()
        {
            int count = 0;
            while (_sendQueue.TryDequeue(out byte[] data))
            {
                if (_isClosing) 
                {
                    Log("刷新队列时检测到关闭标志，停止发送");
                    break;
                }
                if (_stream == null) 
                {
                    Log("刷新队列时_stream为null，停止发送");
                    break;
                }
                
                try 
                {
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                    count++;
                    Log($"[队列] 发送 #{count}, 大小: {data.Length} bytes");
                } 
                catch (Exception ex)
                {
                    LogError($" 发送队列数据 #{count+1} 失败", ex);
                    break; // 发送失败则停止，避免数据错乱
                }
            }
            return count;
        }

        // ========== 读取循环 ==========
        private void StartReadLoop()
        {
            Log("读取循环启动");
            byte[] buf = new byte[65536];
            try 
            {
                while (!_isClosing) 
                {
                    // 检查连接状态
                    if (_tcpClient == null || !_tcpClient.Connected)
                    {
                        Log("读取循环: 连接已断开，退出循环");
                        break;
                    }
                    
                    int bytesRead = _stream.Read(buf, 0, buf.Length);
                    if (bytesRead > 0) 
                    {
                        this.LastActive = DateTime.Now;
                        byte[] data = new byte[bytesRead];
                        Buffer.BlockCopy(buf, 0, data, 0, bytesRead);
                        
                        Log($"[接收] 从MC收到 {bytesRead} bytes, 回传给WebRTC");
                        
                        // 回传给 WebRTC，带上 ConnId
                        WebSocket_WebRtc.SendBack(data, this.PeerId, this.ConnId);
                    } 
                    else 
                    {
                        Log("读取循环: 读取到0字节，连接可能已关闭");
                        break;
                    }
                }
            } 
            catch (Exception ex) 
            {
                if (!_isClosing) // 避免Close时的重复日志
                {
                    LogError("读取循环异常", ex);
                }
            }
            finally 
            { 
                Log("读取循环结束，调用Close()");
                Close(); 
            }
        }

        // ========== 发送数据到Socket ==========
        public void SendToSocket(byte[] data)
        {
            if (_isClosing) 
            {
                Log($"[发送请求] 会话已关闭，丢弃数据 ({data.Length} bytes)");
                return;
            }
            
            this.LastActive = DateTime.Now;
            Log($"[发送请求] 收到 {data.Length} bytes 数据");

            // 检查连接状态（线程安全读取）
            bool connected;
            lock (_connectLock)
            {
                connected = _isConnected;
            }
            
            if (!connected) 
            {
                Log($"[发送请求] 连接未建立，数据加入发送队列 (队列长度: {_sendQueue.Count + 1})");
                _sendQueue.Enqueue(data);
                return;
            }

            int flushed = FlushSendQueue();
            if (flushed > 0)
            {
                Log($"[混合发送] 已先发送队列中的 {flushed} 条旧数据");
            }
            
            // 发送当前数据
            try
            {
                Log($"[发送] 直接发送新数据: {data.Length} bytes");
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
                Log($"[发送] 发送成功");
            }
            catch (Exception e)
            {
                LogError($"[发送] 发送数据失败", e);
                Close();
            }
        }

        // ========== 关闭会话 ==========
        public void Close()
        {
            if (_isClosing) 
            {
                Log("Close() 被重复调用，忽略");
                return;
            }
            
            _isClosing = true;
            Log("开始关闭会话...");

            // 释放 ID 资源
            WebRtcVar.ConnIdManager.Release(this.ConnId);
            Log($"ConnId {ConnId} 已释放");

            try 
            {
                _stream?.Dispose();
                Log("NetworkStream 已释放");
                _tcpClient?.Close();
                Log("TcpClient 已关闭");
            } 
            catch (Exception ex) 
            { 
                LogError("关闭资源时异常", ex); 
            }

            // 从全局字典移除
            WebRtcVar.Sessions.TryRemove($"{PeerId}_{ConnId}", out _);
            WebRtcVar.Sessions.TryRemove($"LOCAL_CLIENT_{ConnId}", out _);
            Log("已从全局Sessions字典移除");
    
            // 【可选】通知对端连接断开（如需可取消注释）
            // WebSocket_WebRtc.SendBack(new byte[0], this.PeerId, this.ConnId);
            
            Log($"会话关闭完成: {PeerId}_{ConnId}");
        }
    }
}