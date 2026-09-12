using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Base;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Entity;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

public class InitHook : IMethodHook
{
    private static SimpleHttpServer _runningServer = null;
    private static int _currentServerPort = -1;
    
    // 导入 AllocConsole 函数
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);

    [DllImport("kernel32.dll")]
    private static extern uint GetConsoleOutputCP();

    [OriginalMethod]
    public void InitMpay(Action<int> startAction)
    {
    }


    [HookMethod("WPFLauncher.Manager.arf", "a", "InitMpay")]
    public void InitMpayHook(Action<int> startAction)
    {
        try
        {
            MethodHook.Install();
        }
        catch (ReflectionTypeLoadException ex)
        {
            PluginLog.Error("Hook", "=== ReflectionTypeLoadException: 部分类型加载失败 ===");

            // 输出成功加载的类型（可选）
            if (ex.Types != null)
            {
                var loadedTypes = ex.Types.Where(t => t != null).ToArray();
                PluginLog.Debug("Hook", $"成功加载 {loadedTypes.Length} 个类型:");
                foreach (var type in loadedTypes) PluginLog.Debug("Hook", $"  ✔ {type?.FullName}");
            }

            // 输出加载失败的异常信息
            PluginLog.Error("Hook", $"\n失败的加载异常 ({ex.LoaderExceptions.Length} 个):");
            for (var i = 0; i < ex.LoaderExceptions.Length; i++)
            {
                var loaderEx = ex.LoaderExceptions[i];
                PluginLog.Error("Hook", $"--- 加载异常 #{i + 1} ---");
                PluginLog.Error("Hook", loaderEx.Message);

                // 如果是文件找不到，输出更详细信息
                if (loaderEx is FileNotFoundException fileEx && !string.IsNullOrEmpty(fileEx.FileName))
                    PluginLog.Error("Hook", $"缺少程序集: {fileEx.FileName}");

                // 输出完整堆栈（可选）
                PluginLog.Debug("Hook", loaderEx.StackTrace);
            }
        }
        catch (Exception ex)
        {
            // 其他非 ReflectionTypeLoadException 的异常
            PluginLog.Error("Hook", "=== 未处理异常 ===");
            PluginLog.Error("Hook", ex);
        }

        PrintStatus();

        // Configuration is loaded before hook installation in the bootstrapper.
        WpfConfig.ReadRoomBlacklist();
        WpfConfig.ReadRegexBlacklist();

        // 2. 交互逻辑
        if ((!File.Exists("ApplyConfig") || !File.Exists(ConfigManager.ConfigFilePath)) && !WpfConfig.TestMode)
        {
            if (File.Exists(ConfigManager.ConfigFilePath))
            {
                var res = uz.q("检测到配置文件，是否直接加载运行?", "启动选择", "直接加载", "进入设置");
                if (res != MessageBoxResult.OK) ShowConfigWindow();
            }
            else
            {
                ShowConfigWindow();
            }
        }

        if (WpfConfig.TestMode)
        {
            WpfConfig.DefaultLogger.Warn("警告: 当前处于测试模式, 可能会有一些错误");
        }
        // 3. 应用运行逻辑
        ApplyRuntimeSettings();
        
        InitMpay(startAction);
    }

    // --- 工具函数 (保持不变) ---
    public static string GenerateRandomMacAddress()
    {
        var r = new Random();
        var b = new byte[6];
        r.NextBytes(b);
        b[0] = (byte)(b[0] & 254);
        return string.Join(":", b.Select(x => x.ToString("X2")));
    }

    public static string ConvertToOriginalFormat(string m)
    {
        return m.Replace(":", "");
    }

    public static string Get_MacAddr()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault()?.GetPhysicalAddress().ToString() ?? "000000000000";
        }
        catch
        {
            return "000000000000";
        }
    }

    public static void ShowConfigWindow()
    {
        var window = new Window
        {
            Title = "MCL 设置", Width = 880, Height = 720, MinWidth = 660, MinHeight = 480,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = WpfConfig.KeepWindowsOnTop
        };
        Mcl.Core.Dotnetdetour.UI.Themes.FluentTheme.Apply(window);
        window.Content = new Mcl.Core.Dotnetdetour.UI.Controls.SettingsPanel(() => window.Close());
        window.ShowDialog();
    }

    public static void ApplyRuntimeSettings()
    {
        Mcl.Core.Dotnetdetour.Utilities.Diagnostics.LauncherLogging.Configure(
            WpfConfig.EnableVerboseLogging, WpfConfig.WriteLauncherLogsToFile, WpfConfig.LauncherRootDirectory);
        // ==========================================
        // Web服务器 生命周期管理
        // ==========================================
        if (WpfConfig.EnableWebServer)
        {
            // 如果服务器还没启，或者端口发生了改变，才需要重新启动
            if (_runningServer == null || _currentServerPort != WpfConfig.HttpPort)
            {
                // 如果存在旧服务器，先把它关掉释放端口
                if (_runningServer != null)
                {
                    try
                    {
                        _runningServer.Stop();
                        PluginLog.Info("Hook", $"[Web] 旧服务器已关闭 (端口: {_currentServerPort})");
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error("Hook", $"[Web] 关闭旧服务器失败: {ex.Message}");
                    }
                }

                // 启动新服务器
                WpfConfig.DefaultHttpAddress = $"http://127.0.0.1:{WpfConfig.HttpPort}/";
                _runningServer = new SimpleHttpServer();
                _currentServerPort = WpfConfig.HttpPort; // 记录最新端口

                Task.Run(() => _runningServer.Start(WpfConfig.DefaultHttpAddress));
                PluginLog.Info("Hook", $"[Web] 服务器已启动: {WpfConfig.DefaultHttpAddress}");
            }
        }
        else
        {
            // 如果用户在设置里关闭了服务器，但后台还在运行，则立刻关闭它
            if (_runningServer != null)
            {
                try
                {
                    _runningServer.Stop(); 
                    PluginLog.Info("Hook", $"[Web] 服务器已被用户关闭");
                }
                catch (Exception ex)
                {
                    PluginLog.Error("Hook", $"[Web] 关闭服务器失败: {ex.Message}");
                }
                finally
                {
                    // 清空状态
                    _runningServer = null;
                    _currentServerPort = -1;
                }
            }
        }

        // ==========================================
        // Mac 地址逻辑
        // ==========================================
        WpfConfig.MacAddress = Get_MacAddr();
        WpfConfig.RandomMacAddress = ConvertToOriginalFormat(GenerateRandomMacAddress());
        
        // ==========================================
        // 模组注入逻辑
        // ==========================================
        if (WpfConfig.EnableModInjection)
        {
            var modsInjectPath = Path.Combine(Directory.GetCurrentDirectory(), "ModsInject");
            if (!Directory.Exists(modsInjectPath)) Directory.CreateDirectory(modsInjectPath);
            PluginLog.Info("Hook", "[ModsInject] 模组注入已启用，请将模组文件放入以下文件夹：");
            PluginLog.Info("Hook", $"[ModsInject] {modsInjectPath}");
            Process.Start("explorer.exe", modsInjectPath);
        }

        if (WpfConfig.ShowCustomServers)
        {
            _ = LoadRemoteServersAsync();
        }
        else
        {
            WpfConfig.CustomRecentServers.Clear();
        }
        
        try
        {
            WpfConfig.ServerListUri = new Uri(WpfConfig.ServerListUrl);
        }
        catch (Exception)
        {
            WpfConfig.ServerListUri = new Uri("https://x19.update.netease.com/serverlist/release.json");
            PluginLog.Error("Hook", "错误的网易服务器地址，已改用默认地址。");
        }

        if (string.IsNullOrWhiteSpace(WpfConfig.LanNicknameFilterKeywords))
        {
            // 为空时直接给空列表，避免残留旧数据
            WpfConfig.LanNicknameFilters = new List<string>();
        }
        else
        {
            WpfConfig.LanNicknameFilters = WpfConfig.LanNicknameFilterKeywords
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) // 去掉空字符串
                .Select(s => s.Trim())                                      // 去掉首尾空格
                .Where(s => !string.IsNullOrEmpty(s))                       // 再去掉纯空白项
                .ToList();
        }
        

    }

    private const string StartupLogo = "\u2588\u2588\u2557    \u2588\u2588\u2557\u2588\u2588\u2588\u2588\u2588\u2588\u2557 \u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2557\u2588\u2588\u2557  \u2588\u2588\u2557 \u2588\u2588\u2588\u2588\u2588\u2588\u2557  \u2588\u2588\u2588\u2588\u2588\u2588\u2557 \u2588\u2588\u2557  \u2588\u2588\u2557\n\u2588\u2588\u2551    \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2554\u2550\u2550\u2550\u2550\u255d\u2588\u2588\u2551  \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2554\u2550\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2551 \u2588\u2588\u2554\u255d\n\u2588\u2588\u2551 \u2588\u2557 \u2588\u2588\u2551\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2588\u2588\u2588\u2557  \u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2588\u2588\u2588\u2554\u255d \n\u2588\u2588\u2551\u2588\u2588\u2588\u2557\u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2550\u255d \u2588\u2588\u2554\u2550\u2550\u255d  \u2588\u2588\u2554\u2550\u2550\u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2588\u2588\u2557 \n\u255a\u2588\u2588\u2588\u2554\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2551     \u2588\u2588\u2551     \u2588\u2588\u2551  \u2588\u2588\u2551\u255a\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u255a\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2551  \u2588\u2588\u2557\n \u255a\u2550\u2550\u255d\u255a\u2550\u2550\u255d \u255a\u2550\u255d     \u255a\u2550\u255d     \u255a\u2550\u255d  \u255a\u2550\u255d \u255a\u2550\u2550\u2550\u2550\u2550\u255d  \u255a\u2550\u2550\u2550\u2550\u2550\u255d \u255a\u2550\u255d  \u255a\u2550\u255d\n                                                            ";

    private static void PrintStatus()
    {
        if (WpfConfig.ShowStartupLogo)
            ConsoleOutput.Write("\n" + StartupLogo + Environment.NewLine, ConsoleColor.Cyan);

        PluginLog.Info("Core", "Console initialized.");
        PluginLog.Info("Core", "Plugin hook initialized: version={0}", WpfConfig.Version);
        PluginLog.Info("Core", "工具作者: daijunhao(QQ: 3352133106), 本项目仅供学习交流使用, 严禁用于非法用途/商业/倒卖等多类用途使用", WpfConfig.Version);
        PluginLog.Info("Core", "项目地址: https://github.com/daijunhaoMinecraft/WPFLauncher_Hook");
        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(3);
                var messageData = httpClient.GetAsync(
                    "https://gitee.com/dai-junhao-123/app-config/raw/master/HookConfig/AppInfo.json").Result;
                var messageJson = JObject.Parse(messageData.Content.ReadAsStringAsync().Result);
                PluginLog.InfoContent("Core", "Announcement", "\n" + messageJson["announcement"]?.ToString());
            }
        }
        catch (Exception exception)
        {
            PluginLog.Error("Core", exception, "Announcement request failed.");
        }
    }

    public static async Task LoadRemoteServersAsync()
    {
        using (var client = new HttpClient())
        {
            try
            {
                string json = await client.GetStringAsync("https://raw.giteeusercontent.com/dai-junhao-123/app-config/raw/master/HookConfig/CustomServer.json");
                
                // 直接反序列化为 List<NetGameResponse>
                var remoteList = JsonConvert.DeserializeObject<List<NetGameResponse>>(json);

                if (remoteList != null)
                {
                    // 清空原有自定义列表（可选，按需决定）
                    WpfConfig.CustomRecentServers.Clear();

                    foreach (var response in remoteList)
                    {
                        // 使用 entity_id 作为键，与 InitCustomServers 保持一致
                        string key = response.NetGameEntity?.EntityId;
                        if (!string.IsNullOrEmpty(key))
                        {
                            WpfConfig.CustomRecentServers.Add(
                                new Tuple<string, NetGameResponse>(key, response)
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 失败时可回退到本地硬编码列表，保证程序继续运行
                PluginLog.Error("Hook", $"获取远程服务器列表失败：{ex.Message}");
                // 如有需要，可调用 InitCustomServers() 作为备用
            }
        }
    }
}