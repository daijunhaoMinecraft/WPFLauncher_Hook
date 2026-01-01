using System;
using System.IO;
using WPFLauncher.Util;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
    //解决因为StartDump问题导致创建房间崩端的问题
    public class No_MclLog : IMethodHook
    {
        [HookMethod("WPFLauncher.Manager.apg", "r", null)]
        public void r(string errorInfo = "No Exception Description\r\n", int maw = 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR]ErrorInfo:{errorInfo}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}