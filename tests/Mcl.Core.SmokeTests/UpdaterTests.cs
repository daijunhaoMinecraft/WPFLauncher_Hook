using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mcl.Core.Updater;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static void UpdaterTests()
    {
        // 渠道由 ChannelGet() 解析结果映射，不硬编码在更新逻辑里。
        var netease = LauncherChannelProfile.FromChannel("netease");
        Check(netease.PatchListPrefix == string.Empty && netease.ProductName == "WPFLauncher",
            "channel mapping netease");
        Check(netease.BuildPatchListPath() == "/pl/x19_java_patchlist", "patch list path netease");

        var pc4399 = LauncherChannelProfile.FromChannel("4399pc");
        Check(pc4399.PatchListPrefix == "PC4399_" && pc4399.ProductName == "PC4399_WPFLauncher",
            "channel mapping 4399pc");
        Check(pc4399.BuildPatchListPath() == "/pl/PC4399_x19_java_patchlist", "patch list path 4399pc");

        // 官方对 a50 的规则：列表前缀使用 A50SdkCn_，产品名不加前缀。
        var a50 = LauncherChannelProfile.FromChannel("a50_sdk_cn");
        Check(a50.PatchListPrefix == "A50SdkCn_" && a50.ProductName == "WPFLauncher", "channel mapping a50");
        Check(LauncherChannelProfile.FromChannel("unknown").Channel == "netease", "channel fallback");

        // 官方 patchlist 响应没有最外层花括号且带尾随逗号。
        var payload = "\"1.3.0.47160\":{\"size\": 96615566, \"url\": \"https://example/1.7z\", \"md5\": \"abc\"},\n" +
                      "\"1.5.0.10\":{\"size\": 10, \"url\": \"https://example/2.7z\", \"md5\": \"def\"},\n";
        var parsed = LauncherPatchList.Parse(payload);
        Check(parsed.Count == 2 && parsed["1.5.0.10"].Size == 10 && parsed["1.3.0.47160"].Md5 == "abc",
            "patch list parse");
        Check(LauncherPatchList.Parse(null).Count == 0 && LauncherPatchList.Parse("   ").Count == 0,
            "patch list empty payload");

        var versions = new Dictionary<string, LauncherPatchInfo>
        {
            ["1.14.0.47160"] = new LauncherPatchInfo { Size = 100 },
            ["1.15.0.10"] = new LauncherPatchInfo { Size = 200 },
            ["1.15.1.20"] = new LauncherPatchInfo { Size = 300 },
            ["1.15.2.30"] = new LauncherPatchInfo { Size = 400 }
        };

        // 跨系列：先装系列首版整包，再打最新补丁（对齐官方 GetLastMajorMinorPatch）。
        var crossMajor = LauncherUpdatePlan.Create(new Version("1.14.5.1"), versions);
        Check(crossMajor.MajorVersion?.ToString() == "1.15.0.10" && crossMajor.MinorVersion?.ToString() == "1.15.2.30",
            "plan major and minor steps");
        Check(crossMajor.NewestVersion?.ToString() == "1.15.2.30" && crossMajor.PackageCount == 2 &&
              crossMajor.TotalDownloadSize == 600, "plan totals");

        var minorOnly = LauncherUpdatePlan.Create(new Version("1.15.1.20"), versions);
        Check(minorOnly.MajorVersion == null && minorOnly.MinorVersion?.ToString() == "1.15.2.30",
            "plan minor only");

        var sameSeriesBase = LauncherUpdatePlan.Create(new Version("1.15.0.10"), versions);
        Check(sameSeriesBase.MajorVersion == null && sameSeriesBase.MinorVersion?.ToString() == "1.15.2.30",
            "plan skips already-installed series base");

        var upToDate = LauncherUpdatePlan.Create(new Version("1.15.2.30"), versions);
        Check(!upToDate.HasUpdate && upToDate.PackageCount == 0, "plan up to date");

        var singleVersion = LauncherUpdatePlan.Create(new Version("0.9.0.0"),
            new Dictionary<string, LauncherPatchInfo> { ["1.0.0.1"] = new LauncherPatchInfo { Size = 50 } });
        Check(singleVersion.MajorVersion?.ToString() == "1.0.0.1" && singleVersion.MinorVersion == null,
            "plan single version");
        Check(!LauncherUpdatePlan.Create(new Version("1.0.0.0"), null).HasUpdate, "plan empty list");

        // 应用脚本：等待宿主退出、跳过插件文件、安装器静默参数、重启启动器。
        var session = Path.Combine(Path.GetTempPath(), "mcl-apply-" + Guid.NewGuid().ToString("N"));
        var request = new DeferredPatchApplyRequest
        {
            HostProcessId = 4321,
            HostExecutablePath = @"C:\Games\MCLauncher\WPFLauncher.exe",
            LauncherDirectory = @"C:\Games\MCLauncher",
            SessionDirectory = session,
            MajorInstallerPath = session + @"\1.15.0.10\installer.exe",
            MinorPatchDirectory = session + @"\1.15.2.30"
        };
        var script = DeferredPatchApplier.BuildScript(request);
        Check(script.Contains("PID eq 4321"), "apply script waits for host pid");
        Check(script.Contains("robocopy") && script.Contains("/XF Mcl.Core.dll Mcl.Core.pdb"),
            "apply script excludes hook assembly");
        Check(script.Contains("/silent /verisilent /norestart /nocancel"), "apply script installer arguments");
        Check(script.Contains("start \"\" \"C:\\Games\\MCLauncher\\WPFLauncher.exe\""), "apply script restarts launcher");
        Check(script.Contains("copy /Y \"%HOOK%\" \"%BACKUP%\""), "apply script backs up hook assembly");
        Check(!script.Contains("Mcl.Core.dll.old"), "apply script has no legacy rename step");

        Reject(() => DeferredPatchApplier.BuildScript(null), "apply script rejects null request");
        Reject(() => DeferredPatchApplier.BuildScript(new DeferredPatchApplyRequest
        {
            HostProcessId = 1,
            HostExecutablePath = "x.exe",
            LauncherDirectory = @"C:\x",
            SessionDirectory = @"C:\y"
        }), "apply script rejects empty package list");
        Reject(() => DeferredPatchApplier.BuildScript(new DeferredPatchApplyRequest
        {
            HostProcessId = 1,
            HostExecutablePath = string.Empty,
            LauncherDirectory = @"C:\x",
            SessionDirectory = @"C:\y",
            MinorPatchDirectory = @"C:\y\patch"
        }), "apply script rejects missing restart target");

        SevenZipExclusionTest();
        DeferredApplyEndToEndTest();
        RenderUpdateWindowStates();
        HookUpdateManagerTests();
        RenderHookUpdateWindowStates();
        BypassUpdateTests();
        LiveUpdateCheck();
    }

    /// <summary>Mcl.Core.dll 自更新与启动器更新分离：节点、哈希、解析与替换逻辑独立验证。</summary>
    private static void HookUpdateManagerTests()
    {
        Check(HookUpdateManager.BuildFilePath == "build/Mcl.Core.dll",
            "build channel uses public build directory");
        var auto = HookUpdateManager.BuildMirrorUrls("https://github.com/x/y.dll", 0);
        Check(auto.Length == 3 && auto[0] == "https://gh-proxy.com/https://github.com/x/y.dll" &&
              auto[1] == "https://ghproxy.net/https://github.com/x/y.dll" &&
              auto[2] == "https://github.com/x/y.dll", "hook update auto mirror order");
        Check(HookUpdateManager.BuildMirrorUrls("u", 1).Single() == "u", "hook update direct mirror");
        Check(HookUpdateManager.BuildMirrorUrls("u", 2).Single() == "https://gh-proxy.com/u", "hook update gh-proxy mirror");
        Check(HookUpdateManager.BuildMirrorUrls("u", 3).Single() == "https://ghproxy.net/u", "hook update ghproxy.net mirror");

        var area = Path.Combine(_output, "hook-update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(area);
        var binary = Path.Combine(area, "a.bin");
        File.WriteAllBytes(binary, new byte[] { 1, 2, 3, 4, 5 });
        Check(HookUpdateManager.ComputeGitBlobSha1(binary) == "177e962b377df6da68c3a78adeb93c2ace16c7c6",
            "hook update git blob sha1 vector");

        var text = Path.Combine(area, "b.txt");
        File.WriteAllText(text, "hello\n", new UTF8Encoding(false));
        Check(HookUpdateManager.ComputeGitBlobSha1(text) == "ce013625030ba8dba906f756967f9e9ca394464a",
            "hook update git blob sha1 known blob");
        Check(HookUpdateManager.ComputeSha256(text) == "5891b5b522d5df086d0ff0b110fbd9d21bb4fc7163af34d08286a2e846f6be03",
            "hook update sha256 vector");

        // Build 渠道：仓库 sha 与本地一致时不提示更新。
        var localSha = HookUpdateManager.ComputeGitBlobSha1(text);
        var buildPayload = JObject.Parse("{\"sha\":\"" + localSha + "\",\"download_url\":\"https://raw/build.dll\",\"size\":6}");
        Check(HookUpdateManager.ParseBuildPayload(buildPayload, null, text) == null,
            "build channel detects up to date");

        var commits = JArray.Parse(
            "[{\"commit\":{\"message\":\"fix: update flow\"},\"html_url\":\"https://github.com/x/y/commit/1\"}]");
        var buildInfo = HookUpdateManager.ParseBuildPayload(buildPayload, commits, Path.Combine(area, "missing.dll"));
        Check(buildInfo != null && buildInfo.Version == "Latest Build" && buildInfo.Size == 6 &&
              buildInfo.DownloadUrl == "https://raw/build.dll" &&
              buildInfo.HashType == HookUpdateHashKind.GitBlobSha1 &&
              buildInfo.ExpectedHash == localSha && buildInfo.Notes.Contains("fix: update flow") &&
              buildInfo.PageUrl.EndsWith("/commit/1"), "build channel update info parsed");
        const string sourceSha = "0123456789abcdef0123456789abcdef01234567";
        var sourceCommitPayload = JArray.Parse(
            "[{\"commit\":{\"message\":\"feat: source change\\n\\nMCL-SOURCE-COMMIT: " + sourceSha +
            "\"},\"html_url\":\"https://github.com/x/y/commit/artifact\"}]");
        var sourceBuildInfo = HookUpdateManager.ParseBuildPayload(
            buildPayload, sourceCommitPayload, Path.Combine(area, "missing.dll"));
        Check(sourceBuildInfo != null &&
              sourceBuildInfo.Notes == "feat: source change" &&
              sourceBuildInfo.PageUrl ==
              "https://github.com/daijunhaoMinecraft/WPFLauncher_Hook/commit/" + sourceSha,
            "build channel resolves original source commit");
        Check(HookUpdateManager.ParseBuildPayload(new JObject(), commits, text) == null,
            "build channel invalid payload is ignored");

        // Release 渠道：digest 与本地一致时不提示更新，资产名大小写不敏感。
        var releasePayload = JObject.Parse(
            "{\"tag_name\":\"7.0.2\",\"body\":\"- 修复已知问题\",\"html_url\":\"https://github.com/x/y/releases/tag/7.0.2\"," +
            "\"assets\":[{\"name\":\"mcl.core.dll\"," +
            "\"browser_download_url\":\"https://github.com/x/y/releases/download/7.0.2/Mcl.Core.dll\"," +
            "\"size\":6,\"digest\":\"sha256:5891b5b522d5df086d0ff0b110fbd9d21bb4fc7163af34d08286a2e846f6be03\"}]}");
        Check(HookUpdateManager.ParseReleasePayload(releasePayload, text) == null,
            "release channel detects up to date by sha256");
        var releaseInfo = HookUpdateManager.ParseReleasePayload(releasePayload, Path.Combine(area, "missing.dll"));
        Check(releaseInfo != null && releaseInfo.Version == "7.0.2" && releaseInfo.Size == 6 &&
              releaseInfo.HashType == HookUpdateHashKind.Sha256 &&
              !string.IsNullOrEmpty(releaseInfo.ExpectedHash) &&
              releaseInfo.ExpectedHash.StartsWith("5891b5") &&
              releaseInfo.DownloadUrl.Contains("/releases/download/"),
            "release channel update info parsed");
        Check(HookUpdateManager.ParseReleasePayload(JObject.Parse("{\"assets\":[]}"), text) == null,
            "release channel without asset is ignored");
        var digestless = JObject.Parse(
            "{\"tag_name\":\"7.0.3\",\"assets\":[{\"name\":\"Mcl.Core.dll\",\"browser_download_url\":\"https://x/7z\",\"size\":1}]}");
        Check(HookUpdateManager.ParseReleasePayload(digestless, text)?.HashType == HookUpdateHashKind.None,
            "release channel without digest is flagged");

        // 校验与替换（沙箱文件，不接触真实 DLL）。
        var info = new HookUpdateInfo
        {
            HashType = HookUpdateHashKind.Sha256,
            ExpectedHash = "5891b5b522d5df086d0ff0b110fbd9d21bb4fc7163af34d08286a2e846f6be03"
        };
        Check(HookUpdateManager.VerifyHash(text, info, out _), "hook update hash verification passes");
        info.ExpectedHash = "deadbeef";
        Check(!HookUpdateManager.VerifyHash(text, info, out var verifyError) && !string.IsNullOrEmpty(verifyError),
            "hook update hash verification fails on mismatch");

        var target = Path.Combine(area, "Mcl.Core.dll");
        File.WriteAllText(target, "old hook");
        var downloaded = Path.Combine(area, "Mcl.Core.dll.update-download");
        File.WriteAllText(downloaded, "new hook");
        Check(HookUpdateManager.ReplaceHookFile(downloaded, target, out var replaceError) &&
              string.IsNullOrEmpty(replaceError), "hook update replaces file");
        Check(File.ReadAllText(target) == "new hook" && File.ReadAllText(target + ".old") == "old hook",
            "hook update keeps previous hook as .old");

        // 配置键分离：旧 JSON 只带 DisableUpdate/IsBuildChannel 时，启动器更新默认保持开启。
        var legacy = Newtonsoft.Json.JsonConvert.DeserializeObject<Mcl.Core.Updater.UpdateConfig>(
            "{\"DisableUpdate\":true,\"IsBuildChannel\":false}");
        Check(legacy.DisableUpdate && !legacy.IsBuildChannel && !legacy.DisableLauncherUpdate,
            "legacy update config separation defaults");
    }

    /// <summary>渲染 Mcl.Core.dll 自更新窗口的提示/下载/完成/失败状态，供人工复核。</summary>
    private static void RenderHookUpdateWindowStates()
    {
        var path = Path.Combine(_root, "Mcl.Core", "Dotnetdetour", "UI", "Windows", "HookUpdateWindow.xaml");

        var prompt = ParseWindowWithoutCodeBehind(path);
        ((TextBlock)prompt.FindName("VersionText")).Text = "7.0.2 · Mcl.Core.dll 插件更新（与启动器更新相互独立）";
        ((TextBlock)prompt.FindName("SizeText")).Text = "下载大小约 1.29 MB · 校验方式：SHA256（Release 资产 digest）";
        ((FlowDocumentScrollViewer)prompt.FindName("NotesViewer")).Document = MarkdownWpfParser.Parse(
            "## 更新内容\n- 修复房间黑名单踢人后人数不刷新的问题\n- 更新流程拆分为启动器更新与插件自更新\n\n**提示**：更新完成后启动器会自动重启。");
        Render((FrameworkElement)prompt.Content, prompt.Width - 16, prompt.Height - 40, "hook-update-prompt.png");
        prompt.Close();

        var working = ParseWindowWithoutCodeBehind(path);
        ShowPanel(working, "WorkingPanel");
        ((FrameworkElement)working.FindName("PromptExtras")).Visibility = Visibility.Collapsed;
        ((TextBlock)working.FindName("StageTitle")).Text = "正在下载更新包…";
        ((TextBlock)working.FindName("StageDetail")).Text = "Mcl.Core.dll";
        ((ProgressBar)working.FindName("StageProgress")).Value = 62.4;
        ((TextBlock)working.FindName("PercentText")).Text = "62.4%";
        ((TextBlock)working.FindName("TrafficText")).Text = "0.80 MB / 1.29 MB · 6.4 MB/s";
        Render((FrameworkElement)working.Content, working.Width - 16, working.Height - 40, "hook-update-working.png");
        working.Close();

        var ready = ParseWindowWithoutCodeBehind(path);
        ShowPanel(ready, "ReadyPanel");
        ((FrameworkElement)ready.FindName("PromptExtras")).Visibility = Visibility.Collapsed;
        ((TextBlock)ready.FindName("ReadyDetail")).Text = "7.0.2 已在启动器重启后生效。";
        Render((FrameworkElement)ready.Content, ready.Width - 16, ready.Height - 40, "hook-update-ready.png");
        ready.Close();

        var error = ParseWindowWithoutCodeBehind(path);
        ShowPanel(error, "ErrorPanel");
        ((FrameworkElement)error.FindName("PromptExtras")).Visibility = Visibility.Collapsed;
        ((TextBlock)error.FindName("ErrorText")).Text = "下载更新包失败：远程服务器返回错误: (404) Not Found";
        Render((FrameworkElement)error.Content, error.Width - 16, error.Height - 40, "hook-update-error.png");
        error.Close();

        Check(true, "hook update window states rendered");
    }

    /// <summary>宿主自带更新必须无条件阻止，且 Hook 契约（目标类型/方法）不能变化。</summary>
    private static void BypassUpdateTests()
    {
        var hook = new Mcl.Core.Dotnetdetour.Features.AntiCheatBypass.BypassUpdate();
        Check(!hook.WpfLauncherUpdateHook(), "host launcher update blocked by default");
        Check(!hook.WpfLauncherUpdateHook(), "host launcher update stays blocked on repeated checks");

        var method = typeof(Mcl.Core.Dotnetdetour.Features.AntiCheatBypass.BypassUpdate)
            .GetMethod(nameof(Mcl.Core.Dotnetdetour.Features.AntiCheatBypass.BypassUpdate.WpfLauncherUpdateHook));
        var attribute = method?.GetCustomAttribute<Mcl.Core.Dotnetdetour.CoreEngine.Attributes.HookMethodAttribute>();
        Check(attribute != null && attribute.TargetTypeFullName == "WPFLauncher.Update.xw" &&
              attribute.GetTargetMethodName(method) == "h" &&
              attribute.GetOriginalMethodName(method) == "WpfLauncherUpdate",
            "host updater hook contract unchanged");
        Check(!hook.WpfLauncherUpdate(), "original method placeholder never enables updates");
    }

    /// <summary>
    /// 可选联网验证（设置 MCL_LIVE_UPDATE_CHECK=1 才执行）：真实读取官方 patchlist、下载最新补丁并校验
    /// MD5、按排除规则解压。默认关闭，保证常规烟雾测试离线可跑。
    /// </summary>
    private static void LiveUpdateCheck()
    {
        if (Environment.GetEnvironmentVariable("MCL_LIVE_UPDATE_CHECK") != "1")
        {
            return;
        }

        var area = Path.Combine(_output, "live-update");
        Directory.CreateDirectory(area);
        var profile = LauncherChannelProfile.FromChannel("netease");

        string payload;
        using (var client = new System.Net.WebClient())
        {
            client.Proxy = null;
            payload = client.DownloadString(LauncherChannelProfile.BaseUrl + profile.BuildPatchListPath());
        }

        var versions = LauncherPatchList.Parse(payload);
        Check(versions.Count > 100, "live patch list parsed");

        var plan = LauncherUpdatePlan.Create(new Version("1.15.26.11160"), versions);
        Check(plan.HasUpdate && plan.NewestVersion != null, "live plan finds an update");

        var patch = plan.MinorPatch ?? plan.MajorPatch;
        Check(patch != null && patch.Size > 0 && !string.IsNullOrEmpty(patch.Md5), "live patch metadata");

        var archive = Path.Combine(area, plan.NewestVersion + ".7z");
        var result = LauncherPatchDownloader.Download(patch.Url, archive, patch.Size, patch.Md5, null, CancellationToken.None);
        Check(result == LauncherPatchDownloader.Ok, "live download with md5 verification");

        var sevenZip = Path.Combine(_root, "MCLauncherDebug", "ext", "7z", "7z.exe");
        if (!File.Exists(sevenZip))
        {
            Console.WriteLine("Skip live extraction: " + sevenZip + " not found");
            return;
        }

        var target = Path.Combine(area, "extract");
        var code = SevenZipExtractor.Extract(sevenZip, archive, target,
            new[] { DeferredPatchApplier.HookAssemblyName, DeferredPatchApplier.HookSymbolName }, CancellationToken.None);
        Check(code == SevenZipExtractor.Ok, "live extraction exit code");
        Check(!Directory.EnumerateFiles(target, "Mcl.Core.dll", SearchOption.AllDirectories).Any(),
            "live extraction skips hook assembly");
        Check(File.Exists(Path.Combine(target, "WPFLauncher.exe")), "live extraction keeps launcher");

        // Mcl.Core.dll 自更新（Release 渠道）：真实读取最新发行版、下载资产并校验 digest。
        string releaseText;
        using (var client = new System.Net.WebClient())
        {
            client.Proxy = null;
            client.Headers.Add("User-Agent", "Mcl.Core.SmokeTests");
            releaseText = client.DownloadString(
                $"https://api.github.com/repos/{HookUpdateManager.RepoOwner}/{HookUpdateManager.RepoName}/releases/latest");
        }

        var releaseInfo = HookUpdateManager.ParseReleasePayload(JObject.Parse(releaseText),
            Path.Combine(area, "missing-hook.dll"));
        Check(releaseInfo != null && !string.IsNullOrEmpty(releaseInfo.DownloadUrl), "live hook release listed");

        var hookDownload = Path.Combine(area, "Mcl.Core.live.dll");
        var mirrorUrls = HookUpdateManager.BuildMirrorUrls(releaseInfo.DownloadUrl, 0);
        Check(HookUpdateManager.Download(mirrorUrls, hookDownload, releaseInfo.Size, null, CancellationToken.None,
                  out var hookError), "live hook download: " + hookError);
        Check(HookUpdateManager.VerifyHash(hookDownload, releaseInfo, out var hookHashError),
            "live hook hash: " + hookHashError);
    }

    /// <summary>渲染更新窗口的提示/下载/完成/失败四种状态，供人工复核 WinUI 风格布局。</summary>
    private static void RenderUpdateWindowStates()
    {
        var path = Path.Combine(_root, "Mcl.Core", "Dotnetdetour", "UI", "Windows", "LauncherUpdateWindow.xaml");

        var prompt = ParseWindowWithoutCodeBehind(path);
        ((TextBlock)prompt.FindName("ChannelText")).Text = "更新渠道：网易官方 · 启动器与插件共用官方更新源";
        ((TextBlock)prompt.FindName("CurrentVersionText")).Text = "1.15.26.11160";
        ((TextBlock)prompt.FindName("NewestVersionText")).Text = "1.15.30.18895";
        ((TextBlock)prompt.FindName("NotesText")).Text = "修复已知问题，优化用户体验。";
        ((TextBlock)prompt.FindName("SizeText")).Text = "下载大小约 113.1 MB · 共 1 个更新包（已排除插件文件）";
        Render((FrameworkElement)prompt.Content, prompt.Width - 16, prompt.Height - 40, "update-prompt.png");
        prompt.Close();

        var working = ParseWindowWithoutCodeBehind(path);
        ShowPanel(working, "WorkingPanel");
        ((TextBlock)working.FindName("StageTitle")).Text = "正在下载更新包 1/1";
        ((TextBlock)working.FindName("StageDetail")).Text = "1.15.30.18895";
        ((ProgressBar)working.FindName("StageProgress")).Value = 43.5;
        ((TextBlock)working.FindName("PercentText")).Text = "43.5%";
        ((TextBlock)working.FindName("TrafficText")).Text = "49.2 MB / 113.1 MB · 6.4 MB/s";
        ((Border)working.FindName("StepDownloadChip")).Background = (Brush)working.FindResource("AccentBrush");
        ((TextBlock)working.FindName("StepDownloadText")).Foreground = (Brush)working.FindResource("OnAccentBrush");
        Render((FrameworkElement)working.Content, working.Width - 16, working.Height - 40, "update-working.png");
        working.Close();

        var ready = ParseWindowWithoutCodeBehind(path);
        ShowPanel(ready, "ReadyPanel");
        ((TextBlock)ready.FindName("ReadyDetail")).Text = "新版本 1.15.30.18895 将在启动器重启后生效。";
        Render((FrameworkElement)ready.Content, ready.Width - 16, ready.Height - 40, "update-ready.png");
        ready.Close();

        var error = ParseWindowWithoutCodeBehind(path);
        ShowPanel(error, "ErrorPanel");
        ((TextBlock)error.FindName("ErrorText")).Text = "下载更新包 1.15.30.18895 失败：网络异常，请检查网络后重试";
        Render((FrameworkElement)error.Content, error.Width - 16, error.Height - 40, "update-error.png");
        error.Close();

        Check(true, "update window states rendered");
    }

    private static void ShowPanel(Window window, string panelName)
    {
        foreach (var name in new[] { "PromptPanel", "WorkingPanel", "ReadyPanel", "ErrorPanel" })
        {
            ((FrameworkElement)window.FindName(name)).Visibility =
                name == panelName ? Visibility.Visible : Visibility.Collapsed;
        }

        foreach (var name in new[] { "PromptButtons", "WorkButtons", "ErrorButtons" })
        {
            ((FrameworkElement)window.FindName(name)).Visibility = Visibility.Collapsed;
        }

        var buttons = panelName == "WorkingPanel" ? "WorkButtons" : panelName == "ErrorPanel" ? "ErrorButtons" : null;
        if (buttons != null)
        {
            ((FrameworkElement)window.FindName(buttons)).Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// 沙箱验证“宿主退出后应用补丁”的真实脚本：等待假宿主退出 → robocopy 跳过 Mcl.Core.dll →
    /// 恢复钩子 DLL → 重启目标进程。
    /// </summary>
    private static void DeferredApplyEndToEndTest()
    {
        var sandbox = Path.Combine(_output, "apply-" + Guid.NewGuid().ToString("N"));
        var install = Path.Combine(sandbox, "install");
        var session = Path.Combine(sandbox, "session");
        var staging = Path.Combine(session, "1.15.2.30");
        Directory.CreateDirectory(install);
        Directory.CreateDirectory(Path.Combine(staging, "Updater"));
        File.WriteAllText(Path.Combine(install, "Mcl.Core.dll"), "hook assembly");
        File.WriteAllText(Path.Combine(install, "old.txt"), "old file");
        File.WriteAllText(Path.Combine(staging, "Mcl.Core.dll"), "official assembly");
        File.WriteAllText(Path.Combine(staging, "Mcl.Core.pdb"), "official symbols");
        File.WriteAllText(Path.Combine(staging, "WPFLauncher.exe"), "launcher v2");
        File.WriteAllText(Path.Combine(staging, "Updater", "MCLauncherUpdater.exe"), "updater v2");

        var restartTarget = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows",
            "System32", "where.exe");

        using (var host = Process.Start(new ProcessStartInfo("cmd.exe", "/c ping -n 4 127.0.0.1 >nul")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            var request = new DeferredPatchApplyRequest
            {
                HostProcessId = host.Id,
                HostExecutablePath = restartTarget,
                LauncherDirectory = install,
                SessionDirectory = session,
                MinorPatchDirectory = staging
            };

            Check(DeferredPatchApplier.Start(request, out var scriptPath, out var logPath), "apply runner starts");
            Check(File.Exists(scriptPath), "apply script written to session");

            var deadline = DateTime.UtcNow.AddSeconds(60);
            var finished = false;
            while (DateTime.UtcNow < deadline)
            {
                if (File.Exists(logPath) && File.ReadAllText(logPath).Contains("apply finished"))
                {
                    finished = true;
                    break;
                }

                Thread.Sleep(200);
            }

            Check(finished, "apply script finished after host exit");
            host.WaitForExit();
        }

        var log = File.ReadAllText(Path.Combine(session, DeferredPatchApplier.LogFileName));
        Check(log.Contains("host exited"), "apply log records host exit");
        Check(log.Contains("robocopy exit code"), "apply log records copy result");
        Check(File.ReadAllText(Path.Combine(install, "Mcl.Core.dll")) == "hook assembly",
            "apply keeps the hook assembly");
        Check(!File.Exists(Path.Combine(install, "Mcl.Core.pdb")), "apply skips the official symbol file");
        Check(File.ReadAllText(Path.Combine(install, "WPFLauncher.exe")) == "launcher v2",
            "apply copies launcher files");
        Check(File.ReadAllText(Path.Combine(install, "Updater", "MCLauncherUpdater.exe")) == "updater v2",
            "apply copies subdirectories");
    }

    /// <summary>用启动器自带 7z.exe 验证真实解压路径会跳过 Mcl.Core.dll（任意层级）。</summary>
    private static void SevenZipExclusionTest()
    {
        var sevenZip = Path.Combine(_root, "MCLauncherDebug", "ext", "7z", "7z.exe");
        if (!File.Exists(sevenZip))
        {
            Console.WriteLine("Skip 7z exclusion check: " + sevenZip + " not found");
            return;
        }

        var work = Path.Combine(_output, "sevenzip-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(work, "src");
        var nested = Path.Combine(source, "nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(source, "Mcl.Core.dll"), "official launcher library");
        File.WriteAllText(Path.Combine(nested, "Mcl.Core.dll"), "nested official library");
        File.WriteAllText(Path.Combine(source, "WPFLauncher.exe"), "launcher stub");
        File.WriteAllText(Path.Combine(nested, "data.txt"), "data");

        var archive = Path.Combine(work, "patch.7z");
        RunProcess(sevenZip, "a \"" + archive + "\" \"" + source + "\\*\" -r", work);

        var target = Path.Combine(work, "out");
        var result = SevenZipExtractor.Extract(sevenZip, archive, target,
            new[] { DeferredPatchApplier.HookAssemblyName, DeferredPatchApplier.HookSymbolName }, CancellationToken.None);

        Check(result == SevenZipExtractor.Ok, "7z extraction exit code");
        Check(!Directory.EnumerateFiles(target, "Mcl.Core.dll", SearchOption.AllDirectories).Any(),
            "7z extraction skips hook assembly everywhere");
        Check(File.Exists(Path.Combine(target, "WPFLauncher.exe")), "7z extraction keeps launcher");
        Check(File.Exists(Path.Combine(target, "nested", "data.txt")), "7z extraction keeps nested files");

        var missing = SevenZipExtractor.Extract(sevenZip, Path.Combine(work, "missing.7z"), target,
            Array.Empty<string>(), CancellationToken.None);
        Check(missing == SevenZipExtractor.ArchiveNotFound, "7z extraction reports missing archive");
    }

    private static void RunProcess(string fileName, string arguments, string workingDirectory)
    {
        using (var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        }))
        {
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("7z test setup failed with exit code " + process.ExitCode);
            }
        }
    }
}
