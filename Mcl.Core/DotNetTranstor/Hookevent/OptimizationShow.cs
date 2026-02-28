using DotNetTranstor;

namespace Mcl.Core.DotNetTranstor.Hookevent;

// 优化加载显示
public class OptimizationShow : IMethodHook
{
    [HookMethod("WPFLauncher.Manager.Game.Pipeline.avp", "a", null)]
    public static string LoadingText(string nowStep)
    {
        return nowStep;
    }
}