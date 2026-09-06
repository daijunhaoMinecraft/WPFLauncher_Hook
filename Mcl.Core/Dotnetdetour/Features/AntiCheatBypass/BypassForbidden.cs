using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
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
        PluginLog.Debug("Core", $"CheckDisable, key: {key}, value: {result}");
        return result;
    }
}