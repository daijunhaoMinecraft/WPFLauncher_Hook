using System;
using System.Text;
using System.Threading;
using System.Timers;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Windows;
using DotNetTranstor.Hookevent;
using DotNetTranstor.Tools;
using MicrosoftTranslator.DotNetTranstor.Hookevent;
using MicrosoftTranslator.DotNetTranstor.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // 需要安装 Newtonsoft.Json 库

public static class WebSocketHelper
{
    // WebSocket 服务器实例
    // private static WebSocketServer _server;
    //
    // // 启动 WebSocket 服务器
    // public static void StartWebSocketServer()
    // {
    //     if (_server == null)
    //     {
    //         // 使用Path_Bool中的WebSocket端口配置
    //         string wsAddress = $"ws://localhost:{Path_Bool.WebSocketPort}/";
    //         _server = new WebSocketServer(wsAddress);
    //         _server.AddWebSocketService<WebSocketServerBehavior>("/");
    //         _server.Start();
    //         DebugPrint.LogDebug_NoColorSelect($"[WebSocket]WebSocket 服务器已启动,Websocket连接地址{wsAddress}");
    //         Thread heartbeatThread = new Thread(() =>
    //         {
    //             while (true)
    //             {
    //                 SendHeartbeatToClients();
    //                 Thread.Sleep(10000);
    //             }
    //         });
    //         heartbeatThread.IsBackground = true; // 设置为后台线程
    //         heartbeatThread.Start();
    //     }
    // }

    public static void SendHeartbeatToClients()
    {
        var heartbeat = new
        {
            type = "heartbeat",
            timestamp = DateTime.UtcNow.ToString("o") // 使用 UTC 时间并格式化为 ISO 8601 格式
        };

        string message = JsonConvert.SerializeObject(heartbeat);
        //DebugPrint.LogDebug_NoColorSelect($"[WebSocket]发送心跳包: {message}");

        // 向所有连接的客户端发送消息
        //_server.WebSocketServices["/"].Sessions.Broadcast(message);
        SimpleHttpServer.BroadcastWebSocketMessage(message);
    }

    public static void SendToClient(string message)
    {
        SimpleHttpServer.BroadcastWebSocketMessage(message);
        // // 检查WebSocket服务是否已启动
        // if (_server == null)
        // {
        //     // WebSocket服务未启动，直接返回，不执行任何操作
        //     if (Path_Bool.IsDebug)
        //     {
        //         DebugPrint.LogDebug_NoColorSelect($"[WebSocket]WebSocket服务未启动，消息未发送: {message}");
        //     }
        //     return;
        // }
        //
        // if (Path_Bool.IsDebug)
        // {
        //     DebugPrint.LogDebug_NoColorSelect($"[WebSocket]发送内容: {message}");
        // }
        // // 向所有连接的客户端发送消息
        // _server.WebSocketServices["/"].Sessions.Broadcast(message);
    }
    // // WebSocket 服务器的行为，处理客户端连接
    // public class WebSocketServerBehavior : WebSocketBehavior
    // {
    //     protected override void OnOpen()
    //     {
    //         // 客户端连接时，发送欢迎消息
    //         DebugPrint.LogDebug_NoColorSelect("[WebSocket]客户端连接已建立.");
    //         Send(JsonConvert.SerializeObject(new { Connect = true, Message = "WebSocket Connected!" }));
    //     }
    //
    //     protected override void OnMessage(WebSocketSharp.MessageEventArgs e)
    //     {
    //         // 接收客户端消息
    //         // try
    //         // {
    //         //     // 使用JObject解析数据
    //         //     JObject message = JObject.Parse(e.Data);
    //         //     
    //         //     if (message["type"] != null)
    //         //     {
    //         //         switch (message["type"].ToString())
    //         //         {
    //         //             case "console":
    //         //                 // 处理控制台命令
    //         //                 if (message["action"] != null)
    //         //                 {
    //         //                     switch (message["action"].ToString())
    //         //                     {
    //         //                         case "log":
    //         //                             string logMsg = message["message"]?.ToString() ?? "无消息内容";
    //         //                             string color = message["color"]?.ToString() ?? "white";
    //         //                             
    //         //                             ConsoleColor consoleColor = ConsoleColor.White;
    //         //                             switch(color.ToLower())
    //         //                             {
    //         //                                 case "red": consoleColor = ConsoleColor.Red; break;
    //         //                                 case "green": consoleColor = ConsoleColor.Green; break;
    //         //                                 case "blue": consoleColor = ConsoleColor.Blue; break;
    //         //                                 case "yellow": consoleColor = ConsoleColor.Yellow; break;
    //         //                                 case "cyan": consoleColor = ConsoleColor.Cyan; break;
    //         //                                 case "magenta": consoleColor = ConsoleColor.Magenta; break;
    //         //                                 default: consoleColor = ConsoleColor.White; break;
    //         //                             }
    //         //                             
    //         //                             Console.ForegroundColor = consoleColor;
    //         //                             DebugPrint.LogDebug_NoColorSelect($"[WebSocket] {logMsg}");
    //         //                             Console.ResetColor();
    //         //                             
    //         //                             Send(JsonConvert.SerializeObject(new { type = "console", status = "success", message = "日志已记录" }));
    //         //                             break;
    //         //                         
    //         //                         case "clear":
    //         //                             Console.Clear();
    //         //                             Send(JsonConvert.SerializeObject(new { type = "console", status = "success", message = "控制台已清空" }));
    //         //                             break;
    //         //                         
    //         //                         default:
    //         //                             Send(JsonConvert.SerializeObject(new { type = "console", status = "error", message = "未知的控制台操作" }));
    //         //                             break;
    //         //                     }
    //         //                 }
    //         //                 break;
    //         //             
    //         //             case "room":
    //         //                 // 处理房间操作
    //         //                 if (message["action"] != null)
    //         //                 {
    //         //                     switch (message["action"].ToString())
    //         //                     {
    //         //                         case "refresh":
    //         //                             // 通知刷新房间信息
    //         //                             WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { 
    //         //                                 type = "Recv_Pocket", 
    //         //                                 status = "room_update",
    //         //                                 message = "房间信息已更新" 
    //         //                             }));
    //         //                             
    //         //                             Send(JsonConvert.SerializeObject(new { type = "room", status = "success", message = "房间信息刷新通知已发送" }));
    //         //                             break;
    //         //                         
    //         //                         default:
    //         //                             Send(JsonConvert.SerializeObject(new { type = "room", status = "error", message = "未知的房间操作" }));
    //         //                             break;
    //         //                     }
    //         //                 }
    //         //                 break;
    //         //             
    //         //             default:
    //         //                 // 未知的消息类型
    //         //                 Send(JsonConvert.SerializeObject(new { type = "error", message = "未知的消息类型" }));
    //         //                 break;
    //         //         }
    //         //     }
    //         // }
    //         // catch (Exception ex)
    //         // {
    //         //     DebugPrint.LogDebug_NoColorSelect($"[WebSocket] 处理消息时发生错误: {ex.Message}");
    //         //     Send(JsonConvert.SerializeObject(new { type = "error", message = $"处理消息时发生错误: {ex.Message}" }));
    //         // }
    //     }
    //
    //     protected override void OnClose(CloseEventArgs e)
    //     {
    //         DebugPrint.LogDebug_NoColorSelect("[WebSocket]客户端连接已关闭.");
    //     }
    // }
}
