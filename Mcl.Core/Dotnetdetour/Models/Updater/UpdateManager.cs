using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Mcl.Core.Dotnetdetour.UI.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcl.Core.Updater
{
    public static class UpdateManager
    {
        private const string RepoOwner = "daijunhaoMinecraft";
        private const string RepoName = "WPFLauncher_Hook";
        private const string BuildFilePath = "Mcl.Core/bin/x86/Debug/net48/Mcl.Core.dll";
        private const string ConfigFileName = "MclUpdateConfig.json";

        public static readonly HttpClient SharedHttpClient;

        public class UpdateConfig
        {
            public bool DisableUpdate { get; set; } = false;
            public bool IsBuildChannel { get; set; } = true;
        }

        public static UpdateConfig CurrentConfig = new UpdateConfig();

        static UpdateManager()
        {
            SharedHttpClient = new HttpClient();
            SharedHttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/114.0.0.0");
            SharedHttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        }

        public static void Initialize()
        {
            if (!CheckAdminPrivileges())
            {
                RestartAsAdmin();
                Environment.Exit(0);
                return;
            }

            CleanupOldFiles();
            LoadOrInitConfig();

            if (!CurrentConfig.DisableUpdate)
            {
                Task.Run(async () => await CheckUpdateLogicAsync());
            }
        }

        #region --- 权限与配置管理 ---

        private static bool CheckAdminPrivileges()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RestartAsAdmin()
        {
            MessageBox.Show("插件更新和运行需要管理员权限！\n点击确定将尝试以管理员身份重新启动。", "权限不足", MessageBoxButton.OK, MessageBoxImage.Warning);
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "WPFLauncher.exe", UseShellExecute = true, Verb = "runas" });
            }
            catch (Exception ex)
            {
                MessageBox.Show("提权失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void LoadOrInitConfig()
        {
            if (File.Exists("DisableUpdater"))
            {
                CurrentConfig.DisableUpdate = true;
                return;
            }

            if (File.Exists(ConfigFileName))
            {
                string json = File.ReadAllText(ConfigFileName);
                CurrentConfig = JsonConvert.DeserializeObject<UpdateConfig>(json) ?? new UpdateConfig();
            }
            else
            {
                RunWpfWindow(() => new UpdateConfigWindow(CurrentConfig.DisableUpdate, CurrentConfig.IsBuildChannel));
            }
        }

        public static void SaveConfig()
        {
            File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(CurrentConfig, Formatting.Indented));
        }

        #endregion

        #region --- WPF 窗口跨线程调度 ---

        private static void RunWpfWindow(Func<Window> windowFactory)
        {
            Thread uiThread = new Thread(() =>
            {
                // 确保有可用的 WPF Application 上下文
                if (Application.Current == null)
                    new Application();

                Window win = windowFactory();
                win.ShowDialog();
            });
            uiThread.SetApartmentState(ApartmentState.STA); // 必须是 STA 才能显示 WPF UI
            uiThread.IsBackground = true;
            uiThread.Start();
            uiThread.Join();
        }

        #endregion

        #region --- 核心更新逻辑 ---

        // 增加了一个带有代理重试机制的 API 请求包装器
        private static async Task<string> GetGitHubApiJsonAsync(string url)
        {
            try
            {
                // 优先尝试直连 API
                return await SharedHttpClient.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] API直连失败，尝试使用代理: {ex.Message}");
                // 如果被墙，使用镜像站中转 API 请求
                return await SharedHttpClient.GetStringAsync($"https://gh-proxy.com/{url}");
            }
        }

        private static async Task CheckUpdateLogicAsync()
        {
            try
            {
                if (CurrentConfig.IsBuildChannel)
                {
                    string fileJson = await GetGitHubApiJsonAsync($"https://api.github.com/repos/{RepoOwner}/{RepoName}/contents/{BuildFilePath}?ref=main");
                    JObject fileInfo = JObject.Parse(fileJson);
                    string remoteSha = fileInfo["sha"].ToString();
                    string downloadUrl = fileInfo["download_url"].ToString();
                    string localFile = "Mcl.Core.dll";

                    if (File.Exists(localFile) && CalculateGitBlobSha1(localFile) == remoteSha) return;

                    string commitJson = await GetGitHubApiJsonAsync($"https://api.github.com/repos/{RepoOwner}/{RepoName}/commits?path={BuildFilePath}&per_page=1");
                    JArray commitArray = JArray.Parse(commitJson);
                    string commitMessage = commitArray[0]["commit"]["message"].ToString();
                    string commitUrl = commitArray[0]["html_url"].ToString();

                    bool agreed = false;
                    int mirrorChoice = 0;
                    RunWpfWindow(() => 
                    {
                        var win = new UpdatePromptWindow("Latest Build", commitMessage, commitUrl);
                        win.Closed += (s, e) => { agreed = win.UserAgreed; mirrorChoice = win.SelectedMirrorIndex; };
                        return win;
                    });

                    // 【核心修改】不再在后台静默下载，而是拉起独立下载进度窗口
                    if (agreed) RunWpfWindow(() => new DownloadProgressWindow(downloadUrl, localFile, mirrorChoice));
                }
                else
                {
                    string releaseJson = await GetGitHubApiJsonAsync($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");
                    JObject releaseInfo = JObject.Parse(releaseJson);
                    
                    string remoteVersion = releaseInfo["tag_name"].ToString();
                    string releaseNotes = releaseInfo["body"].ToString();
                    string releaseUrl = releaseInfo["html_url"].ToString();
                    
                    JToken targetAsset = releaseInfo["assets"]?.FirstOrDefault(a => a["name"]?.ToString().Equals("Mcl.Core.dll", StringComparison.OrdinalIgnoreCase) == true);
                    if (targetAsset == null) return;

                    string downloadUrl = targetAsset["browser_download_url"]?.ToString();
                    string remoteDigest = targetAsset["digest"]?.ToString();
                    string remoteHash = !string.IsNullOrEmpty(remoteDigest) && remoteDigest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? remoteDigest.Substring(7).ToLowerInvariant() : "";

                    string localFile = "Mcl.Core.dll";
                    if (File.Exists(localFile) && !string.IsNullOrEmpty(remoteHash))
                    {
                        if (CalculateFileSHA256(localFile).Equals(remoteHash, StringComparison.OrdinalIgnoreCase)) return;
                    }

                    bool agreed = false;
                    int mirrorChoice = 0;
                    RunWpfWindow(() => 
                    {
                        var win = new UpdatePromptWindow(remoteVersion, releaseNotes, releaseUrl);
                        win.Closed += (s, e) => { agreed = win.UserAgreed; mirrorChoice = win.SelectedMirrorIndex; };
                        return win;
                    });

                    // 【核心修改】不再在后台静默下载，而是拉起独立下载进度窗口
                    if (agreed) RunWpfWindow(() => new DownloadProgressWindow(downloadUrl, localFile, mirrorChoice));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] 更新检查失败: {ex.Message}");
            }
        }

        private static async Task PerformDownloadAndRestartAsync(string originalDownloadUrl, string targetFileName, int mirrorChoice)
        {
            string[] downloadLines;

            // 0:自动多线轮询 1:直连 2:gh-proxy 3:ghproxy.net
            switch (mirrorChoice)
            {
                case 1: downloadLines = new[] { originalDownloadUrl }; break;
                case 2: downloadLines = new[] { $"https://gh-proxy.com/{originalDownloadUrl}" }; break;
                case 3: downloadLines = new[] { $"https://ghproxy.net/{originalDownloadUrl}" }; break;
                default: 
                    downloadLines = new[] {
                        $"https://gh-proxy.com/{originalDownloadUrl}", 
                        $"https://ghproxy.net/{originalDownloadUrl}",
                        originalDownloadUrl
                    }; 
                    break;
            }

            bool downloadSuccess = false;
            Exception lastException = null;

            foreach (var downloadUrl in downloadLines)
            {
                try
                {
                    using (var response = await SharedHttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var fileStream = new FileStream(targetFileName + ".temp", FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                    }
                    downloadSuccess = true;
                    break; // 只要有一条线路成功，直接跳出
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            if (!downloadSuccess)
            {
                MessageBox.Show($"更新包下载失败，所选线路无法连通，请重试或更换节点！\n错误信息: {lastException?.Message}", 
                    "下载失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (File.Exists(targetFileName)) File.Move(targetFileName, targetFileName + ".old");
                File.Move(targetFileName + ".temp", targetFileName);

                Process.Start("WPFLauncher.exe");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("更新替换失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string CalculateGitBlobSha1(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            byte[] headerBytes = Encoding.UTF8.GetBytes($"blob {fileBytes.Length}\0");
            byte[] store = new byte[headerBytes.Length + fileBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, store, 0, headerBytes.Length);
            Buffer.BlockCopy(fileBytes, 0, store, headerBytes.Length, fileBytes.Length);
            using (SHA1 sha1 = SHA1.Create()) return BitConverter.ToString(sha1.ComputeHash(store)).Replace("-", "").ToLower();
        }
        
        private static string CalculateFileSHA256(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    return BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static void CleanupOldFiles()
        {
            try { if (File.Exists("Mcl.Core.dll.old")) File.Delete("Mcl.Core.dll.old"); } catch { }
        }

        #endregion
    }
}