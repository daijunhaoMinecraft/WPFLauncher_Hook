using System;
using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.Tools;
using Newtonsoft.Json;
using WPFLauncher.Manager.GrayUpdate;

namespace Mcl.Core.Dotnetdetour.Hookevent
{
    internal class UnlockAllMode : IMethodHook
    {
        //解锁网易被暗藏的功能(也就是只能通过概率的方式获得)例如:x64_mc(64位mc)等
        [OriginalMethod]
        public void UnlockAll(GrayUpdateType nzp)
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.Manager.GrayUpdate.aud", "m", "UnlockAll")]
        public bool ak(GrayUpdateType nzp)
        {
            // 获取 GrayUpdateType 的名称
            string updateTypeName = Enum.GetName(typeof(GrayUpdateType), nzp);
            if (nzp == GrayUpdateType.CppGameX64)
            {
                if (!Path_Bool.IsEnableX64mc)
                {
                    return false;
                }
            }
            else if (nzp == GrayUpdateType.ChangeMinecraftPath)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[INFO]发现网易修改游戏路径功能已被制止");
                return false;
            }
            if (Path_Bool.IsStartWebSocket)
            {
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "GrayUpdateEnable", data = updateTypeName}));
            }
            Console.ForegroundColor = ConsoleColor.Green;
            if (updateTypeName == "A50Setup")
            {
                if (Path_Bool.IsDebug)
                {
                    Console.WriteLine($"[GrayUpdate]该功能为发烧平台,已返回为false以绕过发烧平台: {updateTypeName}");
                }
                return false;
            }
            if (updateTypeName == "ChangeMinecraftPath")
            {
                if (Path_Bool.IsDebug)
                {
                    Console.WriteLine($"[GrayUpdate]该功能为更改.minecraft路径功能已被制止, 功能: {updateTypeName}");
                }
                return false;
            }
            // 输出包含枚举名称的消息
            if (Path_Bool.IsDebug)
            {
                Console.WriteLine($"[GrayUpdate]成功调用需要概率的功能: {updateTypeName}");
            }
            Console.ForegroundColor = ConsoleColor.White;
            return true;
        }
    }
}