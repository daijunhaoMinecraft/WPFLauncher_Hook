using System;
using DotNetTranstor;

namespace Mcl.Core.DotNetTranstor.Hookevent;

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
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"CheckDisable, key: {key}, value: {result}");
        Console.ForegroundColor = ConsoleColor.White;
        return result;
    }
}