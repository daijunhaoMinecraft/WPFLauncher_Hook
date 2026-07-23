using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.Features.AntiCheatBypass;

public class BypassForbidden : IMethodHook
{
    [OriginalMethod]
    public bool CheckDisable(string key)
    {
        return true;
    }

    [HookMethod("WPFLauncher.Manager.apm", "ag", "CheckDisableOriginal")]
    public bool CheckDisableHook(string key)
    {
        var result = CheckDisable(key);
        WpfConfig.DefaultLogger.Info($"CheckDisable, key: {key}, value: {result}");
        return result;
    }
}