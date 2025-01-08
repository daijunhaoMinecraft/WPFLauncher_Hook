using System;
using System.IO;
using System.Linq;
using Mcl.Core.Utils;
using Newtonsoft.Json;
using WPFLauncher.Util;
using NLog;
using NLog.Layouts;
using NLog.Targets;

namespace DotNetTranstor.Hookevent
{
    //Show Log
    public class WPFLauncherLogging : IMethodHook
    {
        public static byte[] LauncherLog(LogEventInfo logEvent)
        {
            return null;
        }

        [HookMethod("WPFLauncher.Util.sw", "GetBytesToWrite", "No_MclLog_1")]
        protected byte[] GetBytesToWrite(LogEventInfo logEvent)
        {
            if (logEvent.Level == LogLevel.Trace)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }
            else if (logEvent.Level == LogLevel.Debug)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else if (logEvent.Level == LogLevel.Info)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else if (logEvent.Level == LogLevel.Warn)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            }
            else if (logEvent.Level == LogLevel.Error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (logEvent.Level == LogLevel.Fatal)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            Console.WriteLine("Logger - " + logEvent.Level + " - " + logEvent.Message);
            Console.ForegroundColor = ConsoleColor.White;
            if (Path_Bool.IsStartWebSocket)
            {
                var logData = new 
                {
                    type = "Logger",
                    level = logEvent.Level.ToString(),
                    message = logEvent.Message,
                    timestamp = logEvent.TimeStamp
                };
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(logData));
            }
            return LauncherLog(logEvent);
        }
        // 生成详细的日志消息
    }
}