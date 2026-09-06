using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

//去除网易实名认证
internal class Mpay_Log : IMethodHook
{
    [OriginalMethod]
    protected void OnLog(string log)
    {
    }

    [CompilerGenerated]
    [HookMethod(TargetConst.Mpay, "onLog", "OnLog")]
    protected void OnLogHook(string log)
    {
        if (WpfConfig.EnableVerboseLogging) PluginLog.InfoContent("Hook", "Host log", log);
    }
}