using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.Features.AntiCheatBypass;

public class BypassForbidden : IMethodHook
{
    [OriginalMethod]
    public bool CheckDisableOriginal(string key)
    {
        return true;
    }

    [HookMethod("WPFLauncher.Manager.apm", "ag", "CheckDisableOriginal")]
    public bool CheckDisable(string key)
    {
        var result = CheckDisableOriginal(key);
        WpfConfig.DefaultLogger.Info($"CheckDisable, key: {key}, value: {result}");
        return result;
    }
}