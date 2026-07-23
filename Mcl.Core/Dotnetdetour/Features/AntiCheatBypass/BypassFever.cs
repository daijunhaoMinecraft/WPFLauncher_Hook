using System.Runtime.InteropServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;

namespace Mcl.Core.Dotnetdetour.Features.AntiCheatBypass;

internal class BypassFever : IMethodHook
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [OriginalMethod]
    public static void X19_Fever_bypass()
    {
    }

    [HookMethod("WPFLauncher.Manager.apm", "aj", "X19_Fever_bypass")]
    public bool Fever_False()
    {
        return false;
    }
}