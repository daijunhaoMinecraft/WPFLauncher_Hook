using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Update;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Features.AntiCheatBypass;
// 绕过更新 by daijunhao
// update by 2026/01/01

public class BypassUpdate : IMethodHook
{
    [OriginalMethod]
    public bool WpfLauncherUpdate()
    {
        return true;
    }

    [HookMethod("WPFLauncher.Update.xw", "h", "WpfLauncherUpdate")]
    public bool WpfLauncherUpdateHook()
    {
        var result = WpfLauncherUpdate();
        var updateInit = new xw();

        var latestVersion = updateInit.g();
        var currectVersion = updateInit.f();
        WpfConfig.DefaultLogger.Info("[WPFLauncherUpdateInfo]更新情况:");
        WpfConfig.DefaultLogger.Info($" - 当前版本:{currectVersion}");
        WpfConfig.DefaultLogger.Info($" - 最新版本:{latestVersion}");
        var text = string.Format("{0}{1}.{2}.{3}.txt", "/MCUpdate_", latestVersion.Major, latestVersion.Minor,
            latestVersion.Build);
        WpfConfig.DefaultLogger.Info($"最新版更新日志: https://x19.update.netease.com{text}");
        var NeedUpdate = latestVersion > currectVersion;
        if (NeedUpdate)
        {
            WpfConfig.DefaultLogger.Info("发现网易我的世界启动器新版本");
            try
            {
                var httpClient = new HttpClient();
                var updateContentBytes = httpClient.GetByteArrayAsync("https://x19.update.netease.com" + text).Result;
                var updateContent = Encoding.GetEncoding("GBK").GetString(updateContentBytes);
                WpfConfig.DefaultLogger.Info("获取更新内容...");
                WpfConfig.DefaultLogger.Info(updateContent);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404"))
            {
                WpfConfig.DefaultLogger.Warn($"[警告] 更新日志文件未找到: {text}");
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"[错误] 获取更新日志失败: {ex.Message}");
            }

            var isUpdate = uz.q("检测到网易我的世界启动器新版本, 是否更新(请先备份网易我的世界启动器完整目录后再去更新防止hook失效)?\n更新内容:见Windows Console控制台", "",
                "更新", "不更新");
            if (isUpdate == MessageBoxResult.OK) return result;

            return false;
        }

        WpfConfig.DefaultLogger.Info("当前版本已是最新版本");
        return false;
    }
}