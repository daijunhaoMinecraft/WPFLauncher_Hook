using System;
using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using Newtonsoft.Json;
using WPFLauncher.Manager.GrayUpdate;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

internal class UnlockAllMode : IMethodHook
{
    //解锁网易被暗藏的功能(也就是只能通过概率的方式获得)例如:x64_mc(64位mc)等
    [OriginalMethod]
    public void EnableGrayUpdateFeature(GrayUpdateType feature)
    {
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Manager.GrayUpdate.aud", "m", "EnableGrayUpdateFeature")]
    public bool EnableGrayUpdateFeatureHook(GrayUpdateType feature)
    {
        // 获取 GrayUpdateType 的名称
        var updateTypeName = Enum.GetName(typeof(GrayUpdateType), feature);
        if (feature == GrayUpdateType.CppGameX64)
        {
            if (!WpfConfig.IsEnableX64mc) return false;
        }
        else if (feature == GrayUpdateType.ChangeMinecraftPath)
        {
            WpfConfig.DefaultLogger.Info("发现网易修改游戏路径功能已被制止");
            return false;
        }

        if (WpfConfig.IsStartWebSocket)
            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                { type = "GrayUpdateEnable", data = updateTypeName }));
        if (updateTypeName == "A50Setup")
        {
            if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info($"该功能为发烧平台,已返回为false以绕过发烧平台: {updateTypeName}");
            return false;
        }

        if (updateTypeName == "ChangeMinecraftPath")
        {
            if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info($"该功能为更改.minecraft路径功能已被制止, 功能: {updateTypeName}");
            return false;
        }

        WpfConfig.DefaultLogger.Info($"成功调用需要概率的功能: {updateTypeName}");
        return true;
    }
}