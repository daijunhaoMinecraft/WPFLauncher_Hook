using System;
using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.Features.IdentitySpoofing;

//去除网易实名认证
internal class NoRealname : IMethodHook
{
    [OriginalMethod]
    protected void OnExtendFuncFinish(string json)
    {
    }

    [CompilerGenerated]
    [HookMethod(TargetConst.Mpay, "onExtendFuncFinish", "OnExtendFuncFinish")]
    protected void OnExtendFuncFinishHook(string json)
    {
        if (WpfConfig.IsDebug) Console.WriteLine("[RealName]json: " + json);
        OnExtendFuncFinish("{\"methodId\":\"getRealnameStatus\",\"status\":3}");
    }
    
    [OriginalMethod]
    protected void OnCompactViewClosed(int code)
    {
    }

    [HookMethod(TargetConst.Mpay, "onCompactViewClosed", "OnCompactViewClosed")]
    protected void OnCompactViewClosedHook(int code)
    {
        if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info("[MpayLogin]code_onCompactViewClosed: " + code);
        OnCompactViewClosed(3);
    }
}