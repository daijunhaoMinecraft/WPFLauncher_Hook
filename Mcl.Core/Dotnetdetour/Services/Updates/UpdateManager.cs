using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Windows;
using Mcl.Core.Tools.Cryptography;
using Mcl.Core.Utils.Util;
using Newtonsoft.Json;

namespace Mcl.Core.Updater
{
    /// <summary>
    /// 启动器更新入口。流程移植自 MCLauncherUpdater.exe 的 UpdateManager：
    /// 读取官方 patchlist → 与本机启动器版本比较 → 提示用户 → 下载并解压 7z 补丁 → 重启应用。
    ///
    /// 与原版的两点差异：
    /// 1. 渠道不再硬编码，由 ChannelGet() 检测当前启动器渠道；
    /// 2. 钩子运行在启动器进程内部，无法像独立更新程序那样先杀掉启动器再覆盖文件，
    ///    因此把补丁下载/解压到会话目录后交给 DeferredPatchApplier 在宿主退出后应用，
    ///    且解压与拷贝都会跳过钩子自己的 Mcl.Core.dll。
    /// </summary>
    public static class UpdateManager
    {
        private const string Module = "Update";
        private const string ConfigFileName = "MclUpdateConfig.json";
        private const string DisableUpdaterFileName = "DisableUpdater";
        private const int SessionRetentionDays = 3;
        private const int PatchListTimeoutSeconds = 30;
        private const int NoticeTimeoutMilliseconds = 2000;

        public class UpdateConfig
        {
            /// <summary>禁用 Mcl.Core.dll（插件）自更新检查（旧键，自更新渠道见 IsBuildChannel）。</summary>
            public bool DisableUpdate { get; set; } = false;

            /// <summary>Mcl.Core.dll 自更新渠道：true=Build（开发构建），false=Release（稳定发行版）。</summary>
            public bool IsBuildChannel { get; set; } = true;

            /// <summary>禁用启动器（官方 patchlist）更新检查；启动器渠道由 ChannelGet() 自动识别。</summary>
            public bool DisableLauncherUpdate { get; set; } = false;
        }

        public static UpdateConfig CurrentConfig = new UpdateConfig();

        public static async Task InitializeAsync()
        {
            try
            {
                if (!CheckAdminPrivileges())
                {
                    RestartAsAdmin();
                    Environment.Exit(0);
                    return;
                }

                CleanupOldFiles();
                await LoadOrInitConfigAsync();

                // 启动器更新与 Mcl.Core.dll 自更新相互独立，各自有开关；
                // 多开启动器时用互斥锁保证同一时间只有一个实例进入更新流程。
                using (var mutex = new Mutex(false, @"Local\Mcl.Core.Update"))
                {
                    var acquired = false;
                    try
                    {
                        acquired = mutex.WaitOne(TimeSpan.Zero);
                    }
                    catch (AbandonedMutexException)
                    {
                        acquired = true;
                    }

                    if (!acquired)
                    {
                        PluginLog.Info(Module, "另一个启动器实例正在处理更新，本实例跳过检查");
                        return;
                    }

                    try
                    {
                        if (!CurrentConfig.DisableUpdate &&
                            await HookUpdateManager.CheckAndUpdateAsync(CurrentConfig.IsBuildChannel,
                                ResolveHostExecutablePath(ResolveChannelProfile())))
                        {
                            // Mcl.Core.dll 已替换并安排了重启，结束本次会话。
                            Environment.Exit(0);
                            return;
                        }

                        if (!CurrentConfig.DisableLauncherUpdate)
                        {
                            await CheckLauncherUpdateAsync();
                        }
                    }
                    finally
                    {
                        try
                        {
                            mutex.ReleaseMutex();
                        }
                        catch (Exception ex)
                        {
                            PluginLog.Debug(Module, "释放更新互斥锁失败: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(Module, ex, "更新流程异常终止");
            }
        }

        #region --- 权限与配置管理 ---

        private static bool CheckAdminPrivileges()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                PluginLog.Warn(Module, "检查管理员权限失败: " + ex.Message);
                return false;
            }
        }

        private static void RestartAsAdmin()
        {
            MessageBox.Show("插件更新和运行需要管理员权限！\n点击确定将尝试以管理员身份重新启动。", "权限不足",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "WPFLauncher.exe", UseShellExecute = true, Verb = "runas" });
            }
            catch (Exception ex)
            {
                MessageBox.Show("提权失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static async Task LoadOrInitConfigAsync()
        {
            if (File.Exists(DisableUpdaterFileName))
            {
                // 显式放置 DisableUpdater 表示完全退出更新流程：启动器更新与 Mcl.Core.dll 自更新都跳过。
                CurrentConfig.DisableUpdate = true;
                CurrentConfig.DisableLauncherUpdate = true;
                return;
            }

            if (File.Exists(ConfigFileName))
            {
                LoadConfig();
                return;
            }

            var profile = ResolveChannelProfile();
            await UpdateWindowRunner.RunAsync(() => new UpdateConfigWindow(
                CurrentConfig.DisableUpdate, CurrentConfig.IsBuildChannel,
                CurrentConfig.DisableLauncherUpdate, profile.DisplayName));
            LoadConfig();
        }

        private static void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFileName))
                {
                    return;
                }

                var json = File.ReadAllText(ConfigFileName);
                CurrentConfig = JsonConvert.DeserializeObject<UpdateConfig>(json) ?? new UpdateConfig();
            }
            catch (Exception ex)
            {
                PluginLog.Error(Module, ex, "读取更新配置失败，使用默认配置");
                CurrentConfig = new UpdateConfig();
            }
        }

        public static void SaveConfig()
        {
            File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(CurrentConfig, Formatting.Indented));
        }

        #endregion

        #region --- 官方更新检查 ---

        public static LauncherChannelProfile ResolveChannelProfile()
        {
            return LauncherChannelProfile.FromChannel(Channel.ChannelGet());
        }

        public static string GetPatchRootDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Netease",
                "MCLauncherUpdater",
                "patch");
        }

        /// <summary>读取官方 patchlist；失败时返回空表。</summary>
        public static async Task<Dictionary<string, LauncherPatchInfo>> FetchPatchListAsync(LauncherChannelProfile profile)
        {
            var url = LauncherChannelProfile.BaseUrl + profile.BuildPatchListPath();
            try
            {
                var payload = await Task.Run(() => HttpGetString(url, TimeSpan.FromSeconds(PatchListTimeoutSeconds), null));
                var parsed = LauncherPatchList.Parse(payload);
                PluginLog.Info(Module, $"读取官方版本列表成功: {profile.Channel}，共 {parsed.Count} 个版本");
                return parsed;
            }
            catch (Exception ex)
            {
                PluginLog.Error(Module, ex, "读取官方版本列表失败: " + url);
                return new Dictionary<string, LauncherPatchInfo>();
            }
        }

        /// <summary>读取官方更新说明（GB2312，超时 2 秒，失败时返回空字符串）。</summary>
        public static async Task<string> FetchNoticeAsync(Version version)
        {
            if (version == null)
            {
                return string.Empty;
            }

            var path = $"{LauncherChannelProfile.NoticePrefix}{version.Major}.{version.Minor}.{version.Build}.txt";
            var url = LauncherChannelProfile.BaseUrl + path;
            try
            {
                var notice = await Task.Run(() =>
                    HttpGetString(url, TimeSpan.FromMilliseconds(NoticeTimeoutMilliseconds), Encoding.GetEncoding("gb2312")));
                return (notice ?? string.Empty).Trim();
            }
            catch (Exception ex)
            {
                PluginLog.Debug(Module, "读取更新说明失败: " + ex.Message);
                return string.Empty;
            }
        }

        public static Version GetCurrentLauncherVersion(LauncherChannelProfile profile)
        {
            try
            {
                var productPath = Path.Combine(WpfConfig.LauncherRootDirectory, profile.ProductName + ".exe");
                if (File.Exists(productPath))
                {
                    var productVersion = ReadFileVersion(productPath);
                    if (productVersion != null)
                    {
                        return productVersion;
                    }
                }

                var hostVersion = ReadFileVersion(ResolveHostExecutablePath(profile));
                if (hostVersion != null)
                {
                    return hostVersion;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(Module, ex, "读取当前启动器版本失败");
            }

            return new Version("0.0.0.0");
        }

        public static string ResolveHostExecutablePath(LauncherChannelProfile profile)
        {
            try
            {
                var path = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return path;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warn(Module, "读取宿主进程路径失败: " + ex.Message);
            }

            return Path.Combine(WpfConfig.LauncherRootDirectory, profile.ProductName + ".exe");
        }

        private static async Task CheckLauncherUpdateAsync()
        {
            var profile = ResolveChannelProfile();
            PluginLog.Info(Module, $"当前更新渠道: {profile.Channel}（{profile.DisplayName}）");

            var versions = await FetchPatchListAsync(profile);
            if (versions.Count == 0)
            {
                return;
            }

            var currentVersion = GetCurrentLauncherVersion(profile);
            var plan = LauncherUpdatePlan.Create(currentVersion, versions);
            if (!plan.HasUpdate)
            {
                PluginLog.Info(Module, $"启动器已是最新版本: {currentVersion}");
                return;
            }

            PluginLog.Info(Module,
                $"发现启动器更新: {currentVersion} → {plan.NewestVersion}，" +
                $"共 {plan.PackageCount} 个更新包，约 {plan.TotalDownloadSize / 1024d / 1024d:F1} MB");

            var notice = await FetchNoticeAsync(plan.NewestVersion);
            var installDirectory = WpfConfig.LauncherRootDirectory;
            var hostExecutable = ResolveHostExecutablePath(profile);
            var sessionDirectory = Path.Combine(GetPatchRootDirectory(),
                DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + plan.NewestVersion);

            var applyRequest = await UpdateWindowRunner.RunAsync(
                () => new LauncherUpdateWindow(plan, profile, notice, installDirectory, hostExecutable, sessionDirectory),
                window => window.ApplyRequest);

            if (applyRequest == null)
            {
                PluginLog.Info(Module, "用户选择稍后更新");
                return;
            }

            if (!DeferredPatchApplier.Start(applyRequest, out var scriptPath, out _))
            {
                MessageBox.Show("更新文件已准备完成，但启动应用脚本失败，请手动重启启动器后重试。", "更新失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            PluginLog.Info(Module, "更新脚本已启动，正在重启启动器: " + scriptPath);
            Environment.Exit(0);
        }

        #endregion

        #region --- 辅助 ---

        private static Version ReadFileVersion(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            var text = FileVersionHelper.GetVersion(path);
            return Version.TryParse(text, out var version) ? version : null;
        }

        private static string HttpGetString(string url, TimeSpan timeout, Encoding encoding)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = Math.Max(500, (int)timeout.TotalMilliseconds);
            request.ReadWriteTimeout = Math.Max(500, (int)timeout.TotalMilliseconds);
            request.AllowAutoRedirect = true;
            request.Proxy = null;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (var response = request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream ?? Stream.Null, encoding ?? Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static void CleanupOldFiles()
        {
            // 旧版 DLL 自更新遗留的备份。
            try
            {
                if (File.Exists("Mcl.Core.dll.old"))
                {
                    File.Delete("Mcl.Core.dll.old");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Debug(Module, "清理旧备份失败: " + ex.Message);
            }

            // 清理超期的更新会话（下载包、解压结果和应用日志）。
            try
            {
                var root = GetPatchRootDirectory();
                if (!Directory.Exists(root))
                {
                    return;
                }

                var deadline = DateTime.UtcNow.AddDays(-SessionRetentionDays);
                foreach (var directory in Directory.GetDirectories(root))
                {
                    try
                    {
                        if (Directory.GetCreationTimeUtc(directory) < deadline)
                        {
                            Directory.Delete(directory, true);
                            PluginLog.Debug(Module, "已清理旧更新会话: " + directory);
                        }
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Debug(Module, "清理更新会话失败: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Debug(Module, "扫描更新会话目录失败: " + ex.Message);
            }
        }

        #endregion
    }
}
