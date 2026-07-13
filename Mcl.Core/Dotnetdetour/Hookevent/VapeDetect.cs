using System;
using Mcl.Core.Dotnetdetour.Tools;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.Hookevent
{
    
    public class VapeDetect : IMethodHook
    {
        //去除VAPE检测
        [HookMethod("WPFLauncher.Util.tv", "a", "No_Vape")]
        public static Tuple<string, string> a(string gbp)
        {
            if (Path_Bool.IsStartWebSocket)
            {
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "VapeDetect", IsBypass = true}));
            }
            if (Path_Bool.IsDebug)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[Vape]Vape检测成功绕过!");
                Console.ForegroundColor = ConsoleColor.White;
            }
            return new Tuple<string, string>("", "");
        }
    }
}