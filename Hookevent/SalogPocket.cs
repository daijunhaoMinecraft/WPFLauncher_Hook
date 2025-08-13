using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetTranstor;
using DotNetTranstor.Hookevent;
using Newtonsoft.Json;
using WPFLauncher.Manager.Login;
using WPFLauncher.Util;
using MicrosoftTranslator.DotNetTranstor.Tools;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Model;

namespace MicrosoftTranslator.DotNetTranstor.Hookevent
{
    public class SalogPocket : IMethodHook
    {
        
        [OriginalMethod]
        public static void Original_SalogHandler_1(string salogType, string salogData)
        {
        }

        [HookMethod("WPFLauncher.co", "k", "Original_SalogHandler_1")]
        public static void SalogHandler1(string salogType, string salogData)
        {
            if (Path_Bool.IsStartWebSocket)
            {
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "salog-new", SalogType = salogType, data = salogData}));
            }
			
            Console.WriteLine("[INFO]发现/salog-new的post请求(进程检测/dll检测/注入检测等)已及时制止");
            Console.WriteLine("[INFO]检测类型:" + salogType + "发送数据:" + salogData);
        }
        
        [OriginalMethod]
        public static void Original_SalogHandler_2(string loginInfo, LoginErrorCode errorCode, bool isPostClientLog = false)
        {
        }

        [HookMethod("WPFLauncher.co", "n", "Original_SalogHandler_2")]
        public static void SalogHandler2(string loginInfo, LoginErrorCode errorCode, bool isPostClientLog = false)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[login_statistics]LoginInfo:{loginInfo},LoginErrorCode:{errorCode},IsPostclient-log:{isPostClientLog}");
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }
        
        [OriginalMethod]
        public static void Original_SalogHandler_3(string logType, string logData)
        {
        }

        [HookMethod("WPFLauncher.co", "j", "f")]
        public static void SalogHandler3(string logType, string logData)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[salog]type:{logType},data:{logData}");
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }
        
        [OriginalMethod]
        public static void Original_SalogHandler_4(string logType, Dictionary<string, object> logValues)
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.co", "i", "f")]
        public static int SalogHandler4(string logType, Dictionary<string, object> logValues)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[salog]type:{logType},value:{JsonConvert.SerializeObject(logValues)}");
            Console.ForegroundColor = ConsoleColor.White;
            return 0;
        }
        
        [OriginalMethod]
        public static void Original_SalogHandler_5(string logType, Dictionary<string, object> logData)
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.co", "h", "f")]
        public static int SalogHandler5(string logType, Dictionary<string, object> logData)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[salog]type:{logType},value:{JsonConvert.SerializeObject(logData)}");
            Console.ForegroundColor = ConsoleColor.White;
            return 0;
        }
        
        [OriginalMethod]
        public static void Original_SalogHandler_6(string logType, Dictionary<string, string> logEntries)
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.co", "g", "f")]
        public static int SalogHandler6(string logType, Dictionary<string, string> logEntries)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("----------------------------");
            foreach (KeyValuePair<string, string> entry in logEntries)
            {
                Console.WriteLine($"Key:{entry.Key},value:{entry.Value}");
            }
            Console.WriteLine("----------------------------");
            Console.WriteLine($"[salog]type:{logType}");
            Console.BackgroundColor = ConsoleColor.Black;
            return 0;
        }
        
        [OriginalMethod]
        public static void Original_SalogHandler_7(string logType, string phase, int code = 0, string message = "", string tag1 = "", string tag2 = "", string tag3 = "", bool flag = false)
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.Network.Protocol.DC.agy", "a", "f")]
        public static void SalogHandler7(string logType, string phase, int code = 0, string message = "", string tag1 = "", string tag2 = "", string tag3 = "", bool flag = false)
        {
            td.Default.Info(string.Format("DoDiagnosticLog;type:{0}, phase:{1}, code:{2}, message:{3}, tag1:{4}, tag2:{5}, tag3:{6} ", 
                new object[] { logType, phase, code, message, tag1, tag2, tag3 }), new object[0]);
            return;
        }
        
        [OriginalMethod]
        public static void Original_SalogHandler_8(string projectType, Dictionary<string, string> tags, Dictionary<string, int> fields)
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.Network.Protocol.DC.agv", "b", "f")]
        public static void SalogHandler8(string projectType, Dictionary<string, string> tags, Dictionary<string, int> fields)
        {
            // project = "x19",
            // type = projectType,
            // tags = tags,
            // fields = fields,
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[diagnostic-value]project:x19,type:{projectType},tags:{JsonConvert.SerializeObject(tags)},fields:{JsonConvert.SerializeObject(fields)}");
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }
        
        [HookMethod("WPFLauncher.co", "m", "Original_SalogHandler_2")]
        public static void SalogHandler9(GameM atf, string atg, LTaskOpcode ath, long ati, string atj, int atk = 0, string atl = "")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[login_statistics]LoginInfo:{atg},LTaskOpcode:{ath}");
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }
    }
}