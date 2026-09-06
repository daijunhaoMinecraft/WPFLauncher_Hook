using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Features.IdentitySpoofing;

//随机Mac地址,可用于解决连锁Ban问题
internal class RandomMac : IMethodHook
{
    [HookMethod("WPFLauncher.Manager.Log.Util.asi", "b")]
    public static string GetMacAddress()
    {
        PluginLog.Debug("Identity", $"当前Mac地址:{WpfConfig.MacAddress}, 成功替换伪造的mac地址:{WpfConfig.RandomMacAddress}");
        return WpfConfig.RandomMacAddress;
    }
}