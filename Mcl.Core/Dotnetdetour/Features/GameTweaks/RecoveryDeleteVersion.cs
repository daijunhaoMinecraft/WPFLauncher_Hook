using System;
using System.Collections.Generic;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Model;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

public class RecoveryDeleteVersion : IMethodHook
{
    // 恢复网易删除的版本
    [HookMethod("WPFLauncher.Model.aie", "g", null)]
    public static List<GameVersion> GetVersions()
    {
        return GetDisplayableVersions();
    }

    [HookMethod("WPFLauncher.ViewModel.Launcher.jz", "ar", null)]
    protected virtual List<GameVersion> GetVersionList()
    {
        List<GameVersion> list = GetDisplayableVersions();
        // 移除网易服务器上已不存在的版本
        list.Remove(GameVersion.V_1_6_4);
        list.Remove(GameVersion.V_1_7_2);

        // 补充: 1.8.9 游戏文件还在, 但是 authlib 相关的验证mod文件不在了, 因此删除
        list.Remove(GameVersion.V_1_8_9);
        return list;
    }

    private static List<GameVersion> GetDisplayableVersions()
    {
        Array values = Enum.GetValues(typeof(GameVersion));
        List<GameVersion> list = new List<GameVersion>((GameVersion[])values);

        // Host version converters return an empty label for these internal enum values.
        list.Remove(GameVersion.NONE);
        list.Remove(GameVersion.V_X64_CPP);
        return list;
    }
}
