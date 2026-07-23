using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.Features.IdentitySpoofing;

//去除网易实名认证
internal class NoRealname_2 : IMethodHook
{
    [OriginalMethod]
    protected void No_RealName(int code)
    {
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Unisdk.nz", "onCompactViewClosed", "No_RealName")]
    protected void onCompactViewClosed(int code)
    {
        if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info("[MpayLogin]code_onCompactViewClosed: " + code);
        No_RealName(3);
    }
}