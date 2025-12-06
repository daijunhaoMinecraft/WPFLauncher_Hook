using System;
using System.Net.Http;
using System.Windows;
using DotNetTranstor;
using WPFLauncher.Update;
using WPFLauncher.Util;

namespace Mcl.Core.DotNetTranstor.Hookevent;
// 绕过更新 by daijunhao
public class BypassUpdate : IMethodHook
{
    [OriginalMethod]
    public bool bypassWPFLauncherUpdate_original()
    {
        return true;
    }
    [HookMethod("WPFLauncher.Update.xv", "h", "bypassWPFLauncherUpdate_original")]
    public bool bypassWPFLauncherUpdate()
    {
        bool result = bypassWPFLauncherUpdate_original();
        xv updateInit = new xv();
        
        Version latestVersion = updateInit.g();
        Version currectVersion = updateInit.f();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[WPFLauncherUpdateInfo]更新情况:");
        Console.WriteLine($" - 当前版本:{currectVersion}");
        Console.WriteLine($" - 最新版本:{latestVersion}");
        string text = string.Format("{0}{1}.{2}.{3}.txt", new object[]
        {
            "/MCUpdate_",
            latestVersion.Major,
            latestVersion.Minor,
            latestVersion.Build
        });
        Console.WriteLine($"最新版更新日志: https://x19.update.netease.com{text}");
        bool NeedUpdate = latestVersion > currectVersion;
        if (NeedUpdate)
        {
            Console.WriteLine("发现网易我的世界启动器新版本");
            try
            {
                HttpClient httpClient = new HttpClient();
                byte[] updateContentBytes = httpClient.GetByteArrayAsync("https://x19.update.netease.com" + text).Result;
                string updateContent = System.Text.Encoding.GetEncoding("GBK").GetString(updateContentBytes);
                Console.WriteLine("获取更新内容...");
                Console.WriteLine(updateContent);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404"))
            {
                Console.WriteLine($"[警告] 更新日志文件未找到: {text}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[错误] 获取更新日志失败: {ex.Message}");
            }

            MessageBoxResult isUpdate = uy.q($"检测到网易我的世界启动器新版本, 是否更新(请先备份网易我的世界启动器完整目录后再去更新防止hook失效)?\n更新内容:见Windows Console控制台", "", "更新", "不更新", "");
            if (isUpdate == MessageBoxResult.OK)
            {
                return result;
            }
            else
            {
                return false;
            }
        }
        else
        {
            Console.WriteLine("当前版本已是最新版本");
        }
        Console.ForegroundColor = ConsoleColor.White;
        return false;
    }
}