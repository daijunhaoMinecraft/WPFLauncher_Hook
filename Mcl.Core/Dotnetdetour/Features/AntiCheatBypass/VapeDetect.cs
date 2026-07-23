using System;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.Features.AntiCheatBypass;

public class VapeDetect : IMethodHook
{
    //去除VAPE检测
    [HookMethod("WPFLauncher.Util.tv", "a", "No_Vape")]
    public static Tuple<string, string> a(string gbp)
    {
        if (WpfConfig.IsStartWebSocket)
            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "VapeDetect", IsBypass = true }));
        WpfConfig.DefaultLogger.Info("Vape检测成功绕过!");
        return new Tuple<string, string>("", "");
    }
}