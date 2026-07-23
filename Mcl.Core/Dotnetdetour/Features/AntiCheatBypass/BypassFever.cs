using System.Runtime.InteropServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;

namespace Mcl.Core.Dotnetdetour.Features.AntiCheatBypass;

internal class BypassFever : IMethodHook
{
    public const string ClassName = "WPFLauncher.Manager.apm";
    [HookMethod(ClassName, "aj", null)]
    public bool EnabledFever()
    {
        return false;
    }
}