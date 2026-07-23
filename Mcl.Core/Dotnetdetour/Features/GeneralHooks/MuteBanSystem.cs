using System;
using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using WPFLauncher.Common;
using WPFLauncher.Manager;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

//去除禁言
internal class MuteBanSystem : IMethodHook
{
    [OriginalMethod]
    public new bool CheckMuteBan()
    {
        return false;
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Model.UserM", "a", "No_MuteBan_1")]
    public new bool CheckMuteBanHook()
    {
        var isMuted = CheckMuteBan();
        Console.WriteLine($"[INFO] mChatBan: {isMuted}");
        if (isMuted)
        {
            var banChatExpiredAt = azf<arg>.Instance.User.BanChatExpiredAt;
            WpfConfig.DefaultLogger.Error("[MuteSystem] 您被系统暂时禁言, 时间至 " + X19Tools.unix_timestamp_to(banChatExpiredAt));
        }

        if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Warn("发现网易尝试检测该账号是否在禁言状态, 已被制止");
        return false;
    }
}