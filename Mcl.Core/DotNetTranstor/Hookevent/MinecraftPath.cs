using System;
using System.IO;
using DotNetTranstor;
using DotNetTranstor.Hookevent;
using WPFLauncher.Util;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace MicrosoftTranslator.DotNetTranstor.Hookevent
{
    public class MinecraftPath : IMethodHook
    {
        [OriginalMethod]
        private static string ChangeMinecraftPath()
        {
            return "";
        }

        [HookMethod("WPFLauncher.Util.tb", "c", "ChangeMinecraftPath")]
        public static string c()
        {
            string MinecraftPath = ChangeMinecraftPath();
            string NowMinecraftPath = Path.Combine(new string[] { tb.n, "Game", ".minecraft" });
            if (Path_Bool.IsDebug)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[MinecraftPath]Minecraft路径: " + MinecraftPath);
                Console.WriteLine("[MinecraftPath]现在Mincraft路径: " + NowMinecraftPath);
            }
            return NowMinecraftPath;
        }
    }
}