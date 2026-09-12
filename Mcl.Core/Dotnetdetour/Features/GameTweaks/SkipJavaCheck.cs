using System.Reflection;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Common;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Model;
using WPFLauncher.SQLite;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

public class SkipJavaCheck : IMethodHook
{
    [OriginalMethod]
    public static void CheckComponent(avq instance, GameM gameM, BaseWindow baseWindow)
    {
        
    }
    
    [HookMethod("WPFLauncher.Manager.Game.Pipeline.avi", "hp", "CheckComponent")]
    public static void CheckComponentHook(avq instance, GameM gameM, BaseWindow baseWindow)
    {
        if (WpfConfig.SkipStartJavaFileFullCheck)
        {
            WpfConfig.DefaultLogger.Debug("成功跳过 Component 的检查");
            instance.hs(LTaskOpcode.NEXT);
            return;
        }
        CheckComponent(instance, gameM, baseWindow);
    }
    
    [OriginalMethod]
    public static void FixJava(avq instance, GameM gameM, BaseWindow baseWindow)
    {
        
    }
    
    [HookMethod("WPFLauncher.Manager.Game.Pipeline.avr", "hp", "FixJava")]
    public static void FixJavaHook(avq instance, GameM gameM, BaseWindow baseWindow)
    {
        if (WpfConfig.SkipStartJavaFileFullCheck)
        {
            WpfConfig.DefaultLogger.Debug("成功跳过 Java 文件完整性的检查");
            instance.hs(LTaskOpcode.NEXT);
            return;
        }
        FixJava(instance, gameM, baseWindow);
    }
    
    [OriginalMethod]
    public static void AntiIndulgence(avq instance, GameM gameM, BaseWindow baseWindow)
    {
        
    }
    
    [HookMethod("WPFLauncher.Manager.Game.Pipeline.Task.awm", "hp", "AntiIndulgence")]
    public static void AntiIndulgenceHook(avq instance, GameM gameM, BaseWindow baseWindow)
    {
        WpfConfig.DefaultLogger.Debug("成功跳过 游戏防沉迷 的检查");
        instance.hs(LTaskOpcode.NEXT);
        return;
    }
    
    [OriginalMethod]
    public static void CheckLibraries(avq instance, GameM gameM, BaseWindow baseWindow)
    {
        
    }
    
    [HookMethod("WPFLauncher.Manager.Game.Pipeline.Task.awg", "hp", "AntiIndulgence")]
    public static void CheckLibrariesHook(avq instance, GameM gameM, BaseWindow baseWindow)
    {
        WpfConfig.DefaultLogger.Debug("成功跳过 Libraries 完整性的检查");
        instance.hs(LTaskOpcode.NEXT);
        return;
    }
    
    [OriginalMethod]
    public static void CheckModAuthority(avq instance, GameM gameM, BaseWindow baseWindow)
    {
        
    }
    
    [HookMethod("WPFLauncher.Manager.Game.Pipeline.Task.awo", "hp", "AntiIndulgence")]
    public static void CheckModAuthorityHook(avq instance, GameM gameM, BaseWindow baseWindow)
    {
        WpfConfig.DefaultLogger.Debug("成功跳过 模组权限 的检查");
        instance.hs(LTaskOpcode.NEXT);
        return;
    }
}