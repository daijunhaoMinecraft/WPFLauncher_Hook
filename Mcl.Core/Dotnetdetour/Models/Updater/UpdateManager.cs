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
using System.Windows.Threading;
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

        // 【修改点1】将 Initialize 改为异步方法，以便等待窗口关闭
        public static async Task InitializeAsync()
        {
            if (!CheckAdminPrivileges())
            {
                RestartAsAdmin();
                Environment.Exit(0);
                return;
            }
            
            CleanupOldFiles();
            
            // 等待配置加载或配置窗口关闭
            await LoadOrInitConfigAsync();

            if (!CurrentConfig.DisableUpdate)
            {
                await CheckUpdateLogicAsync();
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

        // 【修改点2】改为异步方法
        private static async Task LoadOrInitConfigAsync()
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
                // await 挂起，直到配置窗口关闭才继续向下执行
                await RunWpfWindowAsync(() => new UpdateConfigWindow(CurrentConfig.DisableUpdate, CurrentConfig.IsBuildChannel));
                
                // 窗口关闭后，重新读取刚保存的配置（假设 UpdateConfigWindow 里保存了文件）
                if (File.Exists(ConfigFileName))
                {
                    string json = File.ReadAllText(ConfigFileName);
                    CurrentConfig = JsonConvert.DeserializeObject<UpdateConfig>(json) ?? new UpdateConfig();
                }
            }
        }

        public static void SaveConfig()
        {
            File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(CurrentConfig, Formatting.Indented));
        }

        #endregion

        #region --- WPF 窗口跨线程调度 (核心重构区) ---

        // 【修改点3】重构：无返回值的 WPF 窗口异步等待
        private static Task RunWpfWindowAsync(Func<Window> windowFactory)
        {
            return RunWpfWindowAsync(windowFactory, win => true); // 复用下面的泛型方法
        }

        // 【修改点4】重构：有返回值的 WPF 窗口异步等待 (用于提取窗口内的属性)
        private static Task<TResult> RunWpfWindowAsync<TWindow, TResult>(Func<TWindow> windowFactory, Func<TWindow, TResult> getResult) where TWindow : Window
        {
            var tcs = new TaskCompletionSource<TResult>();

            Thread uiThread = new Thread(() =>
            {
                try
                {
                    System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                    
                    if (Application.Current == null)
                    {
                        new Application() { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    }

                    TWindow win = windowFactory();
                    
                    // 当窗口关闭时，提取属性，结束消息循环，并让 Task 完成
                    win.Closed += (s, e) =>
                    {
                        try
                        {
                            TResult result = getResult(win);
                            tcs.TrySetResult(result);
                        }
                        finally
                        {
                            Dispatcher.ExitAllFrames(); // 退出当前线程的 WPF 消息循环
                        }
                    };

                    win.Show();
                    Dispatcher.Run(); // 启动标准的 WPF 消息循环，而不是 ShowDialog
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.IsBackground = true;
            uiThread.Start();

            return tcs.Task;
        }

        #endregion

        #region --- 核心更新逻辑 ---

        private static async Task<string> GetGitHubApiJsonAsync(string url)
        {
            try
            {
                return await SharedHttpClient.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] API直连失败，尝试使用代理: {ex.Message}");
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

                    // 【修改点5】使用带返回值的 RunWpfWindowAsync 优雅提取用户的选择
                    var promptResult = await RunWpfWindowAsync(
                        () => new UpdatePromptWindow("Latest Build", commitMessage, commitUrl),
                        win => new { win.UserAgreed, win.SelectedMirrorIndex }
                    );

                    // 此时代码会等待窗口关闭后才会执行到这里，promptResult 绝对准确
                    if (promptResult.UserAgreed) 
                    {
                        await RunWpfWindowAsync(() => new DownloadProgressWindow(downloadUrl, localFile, promptResult.SelectedMirrorIndex));
                    }
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

                    // 【修改点6】同上，异步等待结果
                    var promptResult = await RunWpfWindowAsync(
                        () => new UpdatePromptWindow(remoteVersion, releaseNotes, releaseUrl),
                        win => new { win.UserAgreed, win.SelectedMirrorIndex }
                    );

                    if (promptResult.UserAgreed) 
                    {
                        await RunWpfWindowAsync(() => new DownloadProgressWindow(downloadUrl, localFile, promptResult.SelectedMirrorIndex));
                    }
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