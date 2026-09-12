using System;
using System.Diagnostics;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Model;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

public class SkipGameLaunchTimeout : IMethodHook
{
    [OriginalMethod]
    public static string OriginalN(object instance, Process ofy, GameM ofz, int oga)
    {
        return string.Empty;
    }
    
    [HookMethod("WPFLauncher.Manager.Game.Launcher.auw", "n", "OriginalN")]
    public static string HookedN(object instance, Process ofy, GameM ofz, int oga)
    {
        string result = OriginalN(instance, ofy, ofz, oga);

        if (result == "启动超时")
        {
            WpfConfig.DefaultLogger.Info("[+] 拦截到 n() 返回 '启动超时'，已强制篡改为成功状态 (Empty)！");
            
            return string.Empty; 
        }

        return result;
    }
}