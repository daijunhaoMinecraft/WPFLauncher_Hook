using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

//去除网易实名认证
internal class Mpay_Log : IMethodHook
{
    [OriginalMethod]
    protected void Mpay_Log_Show(string log)
    {
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Unisdk.nz", "onLog", "Mpay_Log_Show")]
    protected void onLog(string log)
    {
        if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info(log);
    }
}