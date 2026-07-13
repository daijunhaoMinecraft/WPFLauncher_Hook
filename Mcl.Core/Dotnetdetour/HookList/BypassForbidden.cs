using System;

namespace Mcl.Core.Dotnetdetour.HookList;

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
        bool result = CheckDisableOriginal(key);
        WpfConfig.DefaultLogger.Info($"CheckDisable, key: {key}, value: {result}");
        return result;
    }
}