using System;
using System.IO;
using WPFLauncher.Util;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
    //解决因为StartDump问题导致创建房间崩端的问题
    public class No_MclLog : IMethodHook
    {
        public static void No_MclLog_1(string gxv, string gxw, bool gxx = false)
        {
            
        }
        [HookMethod("WPFLauncher.Manager.apf", "r", "No_MclLog_1")]
        public void r(string mav = "No Exception Description\r\n", int maw = 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR]ErrorInfo:{mav}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}