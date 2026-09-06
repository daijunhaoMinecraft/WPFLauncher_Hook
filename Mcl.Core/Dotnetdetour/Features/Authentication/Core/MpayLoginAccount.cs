using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Features.Authentication.Core;

internal class MpayLoginAccount : IMethodHook
{
    [OriginalMethod]
    protected void No_RealName(int code) { }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Unisdk.nx", "onLoginFinish", "No_RealName")]
    protected void onLoginFinish(int code)
    {
        if (WpfConfig.EnableVerboseLogging)
        {
            PluginLog.Debug("Auth", $"[MpayLogin] 返回代码: {code}");
        }
        
        No_RealName(code);
    }
}