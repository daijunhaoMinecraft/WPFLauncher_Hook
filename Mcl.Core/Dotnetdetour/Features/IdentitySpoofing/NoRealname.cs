using System;
using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.Features.IdentitySpoofing;

//去除网易实名认证
internal class NoRealname : IMethodHook
{
    [OriginalMethod]
    protected void No_RealName(string json)
    {
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Unisdk.nz", "onExtendFuncFinish", "No_RealName")]
    protected void onExtendFuncFinish(string json)
    {
        if (WpfConfig.IsDebug) Console.WriteLine("[RealName]json: " + json);
        No_RealName("{\"methodId\":\"getRealnameStatus\",\"status\":3}");
    }
}