using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Windows;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Mcl.Core.Updater
{
    public enum HookUpdateHashKind
    {
        None,
        GitBlobSha1,
        Sha256
    }

    /// <summary>GitHub 上发现的 Mcl.Core.dll 更新。</summary>
    public sealed class HookUpdateInfo
    {
        public string Version { get; set; }

        public string Notes { get; set; }

        public string PageUrl { get; set; }

        public string DownloadUrl { get; set; }

        public long Size { get; set; }

        public string ExpectedHash { get; set; }

        public HookUpdateHashKind HashType { get; set; }
    }

    /// <summary>
    /// Mcl.Core.dll（钩子自身）更新，与启动器更新完全分离：
    /// - 渠道：GitHub，Build（仓库构建产物）/ Release（最新发行版），由 UpdateConfig.IsBuildChannel 决定；
    /// - 校验：Build 用 git blob SHA1 对比仓库内容，Release 用资产 digest 的 SHA256；
    /// - 应用：下载并替换 Mcl.Core.dll 后调度重启，启动器官方补丁更新由 <see cref="UpdateManager"/> 负责。
    /// </summary>
    public static class HookUpdateManager
    {
        private const string Module = "Update";
        private const string SourceCommitMarker = "MCL-SOURCE-COMMIT:";
        public const string RepoOwner = "daijunhaoMinecraft";
        public const string RepoName = "WPFLauncher_Hook";
        public const string BuildRef = "build-artifacts";
        public const string BuildFilePath = "build/Mcl.Core.dll";
        public const string HookFileName = "Mcl.Core.dll";

        private static readonly HttpClient SharedHttpClient = CreateHttpClient();

        public static string GetHookFilePath()
        {
            return Path.Combine(WpfConfig.LauncherRootDirectory, HookFileName);
        }

        /// <summary>
        /// 检查并（用户同意后）更新 Mcl.Core.dll；返回 true 表示已替换文件并安排了重启，调用方应尽快退出宿主。
        /// 宿主可执行文件路径由调用方提供，确保与启动器更新逻辑完全解耦。
        /// </summary>
        public static async Task<bool> CheckAndUpdateAsync(bool isBuildChannel, string hostExecutablePath)
        {
            try
            {
                var hookPath = GetHookFilePath();
                var info = isBuildChannel
                    ? await GetBuildUpdateAsync(hookPath)
                    : await GetReleaseUpdateAsync(hookPath);
                if (info == null)
                {
                    return false;
                }

                PluginLog.Info(Module, $"发现 Mcl.Core.dll 更新: {info.Version}（{info.HashType}）");
                var updated = await UpdateWindowRunner.RunAsync(
                    () => new HookUpdateWindow(info, hookPath, hostExecutablePath),
                    window => window.Updated);

                if (!updated)
                {
                    PluginLog.Info(Module, "用户未更新 Mcl.Core.dll");
                    return false;
                }

                if (!ScheduleRestartAfterExit(hostExecutablePath, out var error))
                {
                    PluginLog.Warn(Module, "调度启动器重启失败: " + error);
                    return false;
                }

                PluginLog.Info(Module, "Mcl.Core.dll 已更新，正在重启启动器");
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Error(Module, ex, "Mcl.Core.dll 自更新失败");
                return false;
            }
        }

        #region --- 检查（Build / Release） ---

        public static async Task<string> GetGitHubJsonTextAsync(string url)
        {
            try
            {
                return await SharedHttpClient.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                PluginLog.Warn(Module, $"GitHub 直连失败（{ex.Message}），尝试 gh-proxy");
                return await SharedHttpClient.GetStringAsync("https://gh-proxy.com/" + url);
            }
        }

        private static async Task<HookUpdateInfo> GetBuildUpdateAsync(string hookPath)
        {
            var fileText = await GetGitHubJsonTextAsync(
                $"https://api.github.com/repos/{RepoOwner}/{RepoName}/contents/{BuildFilePath}?ref={BuildRef}");

            var commitsText = await GetGitHubJsonTextAsync(
                $"https://api.github.com/repos/{RepoOwner}/{RepoName}/commits?path={BuildFilePath}&sha={BuildRef}&per_page=1");

            return ParseBuildPayload(JObject.Parse(fileText), JArray.Parse(commitsText), hookPath);
        }

        private static async Task<HookUpdateInfo> GetReleaseUpdateAsync(string hookPath)
        {
            var releaseText = await GetGitHubJsonTextAsync(
                $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");
            return ParseReleasePayload(JObject.Parse(releaseText), hookPath);
        }

        /// <summary>解析 Build 渠道响应；返回 null 表示已是最新或响应无效。</summary>
        public static HookUpdateInfo ParseBuildPayload(JObject fileInfo, JArray commits, string hookPath)
        {
            var remoteSha = fileInfo?["sha"]?.ToString();
            var downloadUrl = fileInfo?["download_url"]?.ToString();
            if (string.IsNullOrEmpty(remoteSha) || string.IsNullOrEmpty(downloadUrl))
            {
                PluginLog.Error(Module, "Build 渠道响应缺少 sha/download_url");
                return null;
            }

            if (File.Exists(hookPath) &&
                string.Equals(ComputeGitBlobSha1(hookPath), remoteSha, StringComparison.OrdinalIgnoreCase))
            {
                PluginLog.Info(Module, "Mcl.Core.dll 已是最新 Build（与仓库内容一致）");
                return null;
            }

            var firstCommit = commits?.FirstOrDefault();
            var artifactCommitSha = firstCommit?["sha"]?.ToString();
            // Artifact commits carry the original source message and commit SHA.
            var commitMessage = firstCommit?["commit"]?["message"]?.ToString() ?? string.Empty;
            var notes = commitMessage;
            var pageUrl = firstCommit?["html_url"]?.ToString() ??
                          $"https://github.com/{RepoOwner}/{RepoName}";
            var markerIndex = commitMessage.IndexOf(SourceCommitMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                var markerValue = commitMessage.Substring(markerIndex + SourceCommitMarker.Length).Trim();
                var sourceSha = markerValue
                    .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
                if (IsGitCommitSha(sourceSha))
                {
                    notes = commitMessage.Substring(0, markerIndex).TrimEnd();
                    pageUrl = $"https://github.com/{RepoOwner}/{RepoName}/commit/{sourceSha}";
                }
            }

            if (IsGitCommitSha(artifactCommitSha))
            {
                downloadUrl =
                    $"https://raw.githubusercontent.com/{RepoOwner}/{RepoName}/{artifactCommitSha}/{BuildFilePath}";
            }

            return new HookUpdateInfo
            {
                Version = "Latest Build",
                Notes = notes,
                PageUrl = pageUrl,
                DownloadUrl = downloadUrl,
                Size = fileInfo["size"]?.ToObject<long>() ?? 0,
                ExpectedHash = remoteSha,
                HashType = HookUpdateHashKind.GitBlobSha1
            };
        }

        private static bool IsGitCommitSha(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.Length == 40 &&
                   value.All(character =>
                       (character >= '0' && character <= '9') ||
                       (character >= 'a' && character <= 'f') ||
                       (character >= 'A' && character <= 'F'));
        }

        /// <summary>解析 Release 渠道响应；返回 null 表示已是最新、缺少资产或响应无效。</summary>
        public static HookUpdateInfo ParseReleasePayload(JObject release, string hookPath)
        {
            var asset = release?["assets"]?.FirstOrDefault(item =>
                string.Equals(item["name"]?.ToString(), HookFileName, StringComparison.OrdinalIgnoreCase));
            if (asset == null)
            {
                PluginLog.Warn(Module, "最新 Release 中没有 Mcl.Core.dll 资产");
                return null;
            }

            var downloadUrl = asset["browser_download_url"]?.ToString();
            if (string.IsNullOrEmpty(downloadUrl))
            {
                PluginLog.Error(Module, "Release 资产缺少下载地址");
                return null;
            }

            var digest = asset["digest"]?.ToString();
            var hash = !string.IsNullOrEmpty(digest) &&
                       digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
                ? digest.Substring("sha256:".Length).ToLowerInvariant()
                : null;

            if (!string.IsNullOrEmpty(hash) && File.Exists(hookPath) &&
                string.Equals(ComputeSha256(hookPath), hash, StringComparison.OrdinalIgnoreCase))
            {
                PluginLog.Info(Module, "Mcl.Core.dll 已是最新 Release");
                return null;
            }

            if (string.IsNullOrEmpty(hash))
            {
                PluginLog.Warn(Module, "Release 资产没有 sha256 digest，本次下载无法校验");
            }

            return new HookUpdateInfo
            {
                Version = release["tag_name"]?.ToString() ?? "Latest Release",
                Notes = release["body"]?.ToString() ?? string.Empty,
                PageUrl = release["html_url"]?.ToString() ??
                          $"https://github.com/{RepoOwner}/{RepoName}/releases",
                DownloadUrl = downloadUrl,
                Size = asset["size"]?.ToObject<long>() ?? 0,
                ExpectedHash = hash,
                HashType = string.IsNullOrEmpty(hash) ? HookUpdateHashKind.None : HookUpdateHashKind.Sha256
            };
        }

        #endregion

        #region --- 下载 / 校验 / 替换 / 重启 ---

        /// <summary>节点选择：0=自动（gh-proxy → ghproxy.net → 直连），1=直连，2=gh-proxy，3=ghproxy.net。</summary>
        public static string[] BuildMirrorUrls(string originalUrl, int mirrorChoice)
        {
            switch (mirrorChoice)
            {
                case 1:
                    return new[] { originalUrl };
                case 2:
                    return new[] { "https://gh-proxy.com/" + originalUrl };
                case 3:
                    return new[] { "https://ghproxy.net/" + originalUrl };
                default:
                    return new[]
                    {
                        "https://gh-proxy.com/" + originalUrl,
                        "https://ghproxy.net/" + originalUrl,
                        originalUrl
                    };
            }
        }

        public static bool Download(string[] urls, string targetPath, long expectedSize,
            Action<long, long> progress, CancellationToken cancellationToken, out string error)
        {
            error = null;
            foreach (var url in urls)
            {
                try
                {
                    using (var response = SharedHttpClient
                               .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                               .GetAwaiter().GetResult())
                    {
                        response.EnsureSuccessStatusCode();
                        var total = response.Content.Headers.ContentLength ?? expectedSize;
                        using (var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                        using (var file = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var buffer = new byte[81920];
                            long written = 0;
                            int read;
                            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                file.Write(buffer, 0, read);
                                written += read;
                                progress?.Invoke(written, total);
                            }
                        }
                    }

                    return true;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    PluginLog.Warn(Module, $"更新下载失败（{url}）: {ex.Message}");
                }
            }

            return false;
        }

        public static bool VerifyHash(string filePath, HookUpdateInfo info, out string error)
        {
            error = null;
            if (info == null || info.HashType == HookUpdateHashKind.None || string.IsNullOrEmpty(info.ExpectedHash))
            {
                return true;
            }

            var actual = info.HashType == HookUpdateHashKind.GitBlobSha1
                ? ComputeGitBlobSha1(filePath)
                : ComputeSha256(filePath);
            if (string.Equals(actual, info.ExpectedHash, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            error = $"校验失败（期望 {info.ExpectedHash}，实际 {actual}）";
            PluginLog.Error(Module, "Mcl.Core.dll 更新包 " + error);
            return false;
        }

        /// <summary>用下载好的文件替换正在使用的 Mcl.Core.dll；失败时尽量回滚，旧文件留待下次启动清理。</summary>
        public static bool ReplaceHookFile(string downloadedPath, string hookPath, out string error)
        {
            error = null;
            var oldPath = hookPath + ".old";
            try
            {
                if (File.Exists(oldPath))
                {
                    File.SetAttributes(oldPath, FileAttributes.Normal);
                    File.Delete(oldPath);
                }

                if (File.Exists(hookPath))
                {
                    File.SetAttributes(hookPath, FileAttributes.Normal);
                    File.Move(hookPath, oldPath);
                }

                try
                {
                    File.Move(downloadedPath, hookPath);
                }
                catch
                {
                    if (!File.Exists(hookPath) && File.Exists(oldPath))
                    {
                        File.Move(oldPath, hookPath);
                    }

                    throw;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                PluginLog.Error(Module, ex, "替换 Mcl.Core.dll 失败");
                return false;
            }
        }

        /// <summary>宿主退出后由独立 cmd 重新启动启动器（等待约 2 秒，避免和新实例抢文件/单实例锁）。</summary>
        public static bool ScheduleRestartAfterExit(string hostExecutablePath, out string error)
        {
            error = null;
            try
            {
                var arguments = "/c ping -n 3 127.0.0.1 >nul & start \"\" \"" + hostExecutablePath + "\"";
                Process.Start(new ProcessStartInfo("cmd.exe", arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        #endregion

        #region --- 哈希与客户端 ---

        /// <summary>git 对象哈希：sha1("blob {长度}\0" + 内容)，与 GitHub contents API 的 sha 一致。</summary>
        public static string ComputeGitBlobSha1(string filePath)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            var headerBytes = Encoding.UTF8.GetBytes($"blob {fileBytes.Length}\0");
            var store = new byte[headerBytes.Length + fileBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, store, 0, headerBytes.Length);
            Buffer.BlockCopy(fileBytes, 0, store, headerBytes.Length, fileBytes.Length);
            using (var sha1 = SHA1.Create())
            {
                return BitConverter.ToString(sha1.ComputeHash(store)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        public static string ComputeSha256(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/114.0.0.0");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.Timeout = TimeSpan.FromMinutes(10);
            return client;
        }

        #endregion
    }
}
