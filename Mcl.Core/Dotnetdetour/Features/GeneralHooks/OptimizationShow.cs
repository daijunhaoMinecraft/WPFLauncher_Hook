using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

// 优化加载显示
public class OptimizationShow : IMethodHook
{
    [HookMethod("WPFLauncher.Manager.Game.Pipeline.avp", "a", null)]
    public static string LoadingText(string nowStep)
    {
        return nowStep;
    }
}