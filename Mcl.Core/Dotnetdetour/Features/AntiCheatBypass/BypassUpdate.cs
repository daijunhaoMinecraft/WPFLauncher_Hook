using System.Threading;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

namespace Mcl.Core.Dotnetdetour.Features.AntiCheatBypass;

/// <summary>
/// 阻止宿主（网易我的世界启动器）自带的更新检查。
/// 官方更新会用原版 Mcl.Core.dll 覆盖钩子文件，因此这里始终返回 false，
/// 启动器更新改由 Mcl.Core.Updater.UpdateManager 接管（入口见 Initialize.cs 的更新逻辑）。
/// </summary>
public class BypassUpdate : IMethodHook
{
    private static int _blockedLogged = 0;

    [OriginalMethod]
    public bool WpfLauncherUpdate()
    {
        // 仅用于 Hook 引擎配对原方法签名；插件不再调用宿主原逻辑，避免触发官方更新流程。
        return false;
    }

    /// <summary>默认返回 false：无论宿主何时调用更新检查，都阻止其执行官方更新。</summary>
    [HookMethod("WPFLauncher.Update.xw", "h", "WpfLauncherUpdate")]
    public bool WpfLauncherUpdateHook()
    {
        if (Interlocked.Exchange(ref _blockedLogged, 1) == 0)
        {
            PluginLog.Info("Core", "已阻止宿主自带更新（更新由插件接管；可在更新设置中禁用插件更新）");
        }

        return false;
    }
}
