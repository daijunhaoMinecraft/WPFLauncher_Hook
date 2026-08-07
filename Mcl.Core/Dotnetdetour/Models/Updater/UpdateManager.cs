using System;
using System.Diagnostics;
using System.Drawing; // 必须的，用于 WinForms 的 Size 和 Point
using System.IO;
using System.Linq;
using System.Net.Http; // 引入 HttpClient
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks; // 引入 Task
using System.Windows.Forms; // 全局使用 WinForms 作为 UI 基础
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

        // 全局单例 HttpClient (官方推荐的最佳实践，避免耗尽 Socket 端口)
        private static readonly HttpClient _httpClient;

        // 配置模型
        public class UpdateConfig
        {
            public bool DisableUpdate { get; set; } = false;
            public bool IsBuildChannel { get; set; } = true;
        }

        public static UpdateConfig CurrentConfig = new UpdateConfig();

        // 静态构造函数：初始化 HttpClient 的请求头
        static UpdateManager()
        {
            _httpClient = new HttpClient();
            // 伪装成标准的 Windows Chrome 浏览器，防止 GitHub API 报 403 拦截
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json"); // 指定 API V3
        }

        public static void Initialize()
        {
            // 1. 检查管理员权限
            if (!CheckAdminPrivileges())
            {
                RestartAsAdmin();
                Environment.Exit(0);
                return;
            }

            // 2. 清理旧文件
            CleanupOldFiles();

            // 3. 加载配置
            LoadOrInitConfig();

            // 4. 如果没有禁用更新，则在后台使用 Task 异步检查更新
            if (!CurrentConfig.DisableUpdate)
            {
                Task.Run(async () => await CheckUpdateLogicAsync());
            }
            else
            {
                Console.WriteLine("[Updater] 用户已禁用更新。");
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
            System.Windows.Forms.MessageBox.Show("插件更新和运行需要管理员权限！\n点击确定将尝试以管理员身份重新启动。", 
                            "权限不足", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "WPFLauncher.exe",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("提权失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                ShowConfigUI();
            }
        }

        public static void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(CurrentConfig, Formatting.Indented);
            File.WriteAllText(ConfigFileName, json);
        }

        #endregion

        #region --- 纯C# WinForms UI (配置页 & 提示页) ---

        public static void ShowConfigUI()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                Thread uiThread = new Thread(ShowConfigUI) { IsBackground = true };
                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.Start();
                uiThread.Join();
                return;
            }

            Form form = new Form
            {
                Text = "Mcl.Core 初始配置",
                Size = new Size(300, 220),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false, TopMost = true
            };

            CheckBox chkDisable = new CheckBox
            {
                Text = "禁用更新检查", Location = new Point(20, 20), AutoSize = true,
                Checked = CurrentConfig.DisableUpdate
            };

            Label lblChannel = new Label { Text = "选择更新渠道:", Location = new Point(20, 60), AutoSize = true };
            ComboBox cmbChannel = new ComboBox
            {
                Location = new Point(120, 56), Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbChannel.Items.AddRange(new object[] { "Build版 (开发)", "Release版 (稳定)" });
            cmbChannel.SelectedIndex = CurrentConfig.IsBuildChannel ? 0 : 1;

            Button btnSave = new Button { Text = "保存并继续", Location = new Point(80, 120), Size = new Size(120, 35) };
            
            form.Controls.AddRange(new System.Windows.Forms.Control[] { chkDisable, lblChannel, cmbChannel, btnSave });

            btnSave.Click += (s, e) =>
            {
                CurrentConfig.DisableUpdate = chkDisable.Checked;
                CurrentConfig.IsBuildChannel = cmbChannel.SelectedIndex == 0;
                SaveConfig();
                form.Close();
            };

            if (System.Windows.Forms.Application.MessageLoop)
                form.ShowDialog();
            else
                System.Windows.Forms.Application.Run(form);
        }

        private static bool ShowUpdatePromptUI(string version, string message, string commitUrl)
        {
            bool userAgreed = false;
            Thread uiThread = new Thread(() =>
            {
                Form form = new Form
                {
                    Text = "发现新版本", Size = new Size(400, 250),
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false, MinimizeBox = false, TopMost = true
                };

                Label lblInfo = new Label { Text = $"发现新版本：{version}\n是否立即更新？", Location = new Point(20, 20), AutoSize = true };
                
                TextBox txtMessage = new TextBox
                {
                    Text = message, Location = new Point(20, 60), Size = new Size(340, 60),
                    Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical
                };

                LinkLabel lnkCommit = new LinkLabel { Text = "点击此处查看 GitHub 上的改动详情", Location = new Point(20, 130), AutoSize = true };
                lnkCommit.LinkClicked += (s, e) => Process.Start(commitUrl);

                Button btnYes = new Button { Text = "更新并重启", Location = new Point(60, 170), Size = new Size(100, 30) };
                Button btnNo = new Button { Text = "暂不更新", Location = new Point(220, 170), Size = new Size(100, 30) };

                btnYes.Click += (s, e) => { userAgreed = true; form.Close(); };
                btnNo.Click += (s, e) => { userAgreed = false; form.Close(); };

                form.Controls.AddRange(new System.Windows.Forms.Control[] { lblInfo, txtMessage, lnkCommit, btnYes, btnNo });
                
                if (System.Windows.Forms.Application.MessageLoop)
                    form.ShowDialog();
                else
                    System.Windows.Forms.Application.Run(form);
            });

            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            uiThread.Join();
            return userAgreed;
        }

        #endregion

        #region --- 核心更新逻辑与 HttpClient ---

        // 使用 async/await 改造的更新检查逻辑
        private static async Task CheckUpdateLogicAsync()
        {
            try
            {
                if (CurrentConfig.IsBuildChannel)
                {
                    // 1. 获取文件 Hash 判断是否变化 (HttpClient GetStringAsync 会自动处理 UTF-8 编码)
                    string fileApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/contents/{BuildFilePath}?ref=main";
                    string fileJson = await _httpClient.GetStringAsync(fileApiUrl);
                    JObject fileInfo = JObject.Parse(fileJson);
                    
                    string remoteSha = fileInfo["sha"].ToString();
                    string downloadUrl = fileInfo["download_url"].ToString();

                    string localFile = "Mcl.Core.dll";
                    if (File.Exists(localFile) && CalculateGitBlobSha1(localFile) == remoteSha)
                    {
                        Console.WriteLine("[Updater] 当前已经是最新 Build 版。");
                        return;
                    }
                    
                    // 2. 获取该文件的最新 Commit 记录，获取更新日志 (不再乱码了)
                    string commitApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/commits?path={BuildFilePath}&per_page=1";
                    string commitJson = await _httpClient.GetStringAsync(commitApiUrl);
                    JArray commitArray = JArray.Parse(commitJson);
                    
                    string commitMessage = commitArray[0]["commit"]["message"].ToString();
                    string commitUrl = commitArray[0]["html_url"].ToString();

                    // 3. 询问用户
                    if (ShowUpdatePromptUI("Latest Build", commitMessage, commitUrl))
                    {
                        await PerformDownloadAndRestartAsync(downloadUrl, localFile);
                    }
                }
                else
                {
                    // ==========================================
                    // Release
                    // ==========================================
                    string apiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
                    string releaseJson = await _httpClient.GetStringAsync(apiUrl);
                    JObject releaseInfo = JObject.Parse(releaseJson);
                    
                    // 获取基本信息
                    string remoteVersion = releaseInfo["tag_name"].ToString();
                    string releaseNotes = releaseInfo["body"].ToString();
                    string releaseUrl = releaseInfo["html_url"].ToString();
                    
                    // 1. 遍历 assets 查找指定名称的附件节点
                    JToken assets = releaseInfo["assets"];
                    JToken targetAsset = assets?.FirstOrDefault(a => a["name"]?.ToString().Equals("Mcl.Core.dll", StringComparison.OrdinalIgnoreCase) == true);

                    // 容错处理：如果 Release 中未找到目标文件
                    if (targetAsset == null)
                    {
                        Console.WriteLine("[Updater] 错误：未在 Release 附件列表中找到 Mcl.Core.dll");
                        return;
                    }

                    // 2. 提取下载链接与远程 Hash
                    string downloadUrl = targetAsset["browser_download_url"]?.ToString();
                    string remoteDigest = targetAsset["digest"]?.ToString();
                    string remoteHash = "";

                    if (!string.IsNullOrEmpty(remoteDigest) && remoteDigest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                    {
                        remoteHash = remoteDigest.Substring(7).ToLowerInvariant();
                    }

                    string localFile = "Mcl.Core.dll";

                    // 3. 本地比对逻辑
                    if (File.Exists(localFile) && !string.IsNullOrEmpty(remoteHash))
                    {
                        string localHash = CalculateFileSHA256(localFile);
    
                        // 如果 Hash 完美一致，说明不需要更新
                        if (localHash.Equals(remoteHash, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("[Updater] 当前已经是最新 Release 版。");
                            return; 
                        }
                    }

                    // 4. Hash 不一致（或本地文件不存在），弹窗询问用户
                    if (ShowUpdatePromptUI(remoteVersion + " (稳定版)", releaseNotes, releaseUrl))
                    {
                        // 用户同意后才开始下载
                        await PerformDownloadAndRestartAsync(downloadUrl, localFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] 更新检查失败: {ex.Message}");
            }
        }

        // 使用 HttpClient 进行异步下载的逻辑
        private static async Task PerformDownloadAndRestartAsync(string downloadUrl, string targetFileName)
        {
            Console.WriteLine("[Updater] 开始下载新版本...");
            try
            {
                // 使用 HttpCompletionOption.ResponseHeadersRead 可以提高大文件下载的响应速度并节省内存
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    using (var fileStream = new FileStream(targetFileName + ".temp", FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }

                // 文件替换黑科技
                if (File.Exists(targetFileName)) File.Move(targetFileName, targetFileName + ".old");
                File.Move(targetFileName + ".temp", targetFileName);

                Console.WriteLine("[Updater] 更新成功，正在重启 WPFLauncher.exe...");
                Process.Start("WPFLauncher.exe");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("更新替换失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        
        // 计算标准的 SHA256 Hash (用于 Release 版比对)
        private static string CalculateFileSHA256(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(fileStream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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