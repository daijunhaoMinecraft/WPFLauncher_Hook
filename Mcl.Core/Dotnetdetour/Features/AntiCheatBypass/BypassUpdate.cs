using System;
using System.Net.Http;
using System.Text;
using System.Threading; // 新增：用于线程安全的 Interlocked
using System.Windows;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Update;
using WPFLauncher.Util;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Features.AntiCheatBypass;
// 绕过更新 by daijunhao
// update by 2026/01/01

public class BypassUpdate : IMethodHook
{
    private static int _hasCheckedForUpdate = 0;

    [OriginalMethod]
    public bool WpfLauncherUpdate()
    {
        return true;
    }

    [HookMethod("WPFLauncher.Update.xw", "h", "WpfLauncherUpdate")]
    public bool WpfLauncherUpdateHook()
    {
        if (Interlocked.Exchange(ref _hasCheckedForUpdate, 1) == 1)
        {
            return false;
        }

        var result = WpfLauncherUpdate();
        var updateInit = new xw();

        var latestVersion = updateInit.g();
        var currectVersion = updateInit.f();
        PluginLog.Debug("Core", "[WPFLauncherUpdateInfo]更新情况:");
        PluginLog.Debug("Core", $" - 当前版本:{currectVersion}");
        PluginLog.Debug("Core", $" - 最新版本:{latestVersion}");
        var text = string.Format("{0}{1}.{2}.{3}.txt", "/MCUpdate_", latestVersion.Major, latestVersion.Minor,
            latestVersion.Build);
        PluginLog.Debug("Core", $"最新版更新日志: https://x19.update.netease.com{text}");
        
        var NeedUpdate = latestVersion > currectVersion;
        if (NeedUpdate)
        {
            PluginLog.Debug("Core", "发现网易我的世界启动器新版本");
            try
            {
                var httpClient = new HttpClient();
                var updateContentBytes = httpClient.GetByteArrayAsync("https://x19.update.netease.com" + text).Result;
                var updateContent = Encoding.GetEncoding("GBK").GetString(updateContentBytes);
                PluginLog.Debug("Core", "获取更新内容...");
                PluginLog.Debug("Core", updateContent);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404"))
            {
                PluginLog.Warn("Core", $"[警告] 更新日志文件未找到: {text}");
            }
            catch (Exception ex)
            {
                PluginLog.Error("Core", $"[错误] 获取更新日志失败: {ex.Message}");
            }

            var isUpdate = uz.q("检测到网易我的世界启动器新版本, 是否更新(请先备份网易我的世界启动器完整目录后再去更新防止hook失效)?\n更新内容:见Windows Console控制台", "",
                "更新", "不更新");
            
            if (isUpdate == MessageBoxResult.OK) 
            {
                return result; // 用户同意更新，返回原方法的逻辑结果
            }

            return false; // 用户不同意更新，返回 false 阻止更新
        }

        PluginLog.Debug("Core", "当前版本已是最新版本");
        return false;
    }
}