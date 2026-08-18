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
            Console.WriteLine("=== ReflectionTypeLoadException: 部分类型加载失败 ===");

            // 输出成功加载的类型（可选）
            if (ex.Types != null)
            {
                var loadedTypes = ex.Types.Where(t => t != null).ToArray();
                Console.WriteLine($"成功加载 {loadedTypes.Length} 个类型:");
                foreach (var type in loadedTypes) Console.WriteLine($"  ✔ {type?.FullName}");
            }

            // 输出加载失败的异常信息
            Console.WriteLine($"\n失败的加载异常 ({ex.LoaderExceptions.Length} 个):");
            for (var i = 0; i < ex.LoaderExceptions.Length; i++)
            {
                var loaderEx = ex.LoaderExceptions[i];
                Console.WriteLine($"--- 加载异常 #{i + 1} ---");
                Console.WriteLine(loaderEx.Message);

                // 如果是文件找不到，输出更详细信息
                if (loaderEx is FileNotFoundException fileEx && !string.IsNullOrEmpty(fileEx.FileName))
                    Console.WriteLine($"缺少程序集: {fileEx.FileName}");

                // 输出完整堆栈（可选）
                Console.WriteLine(loaderEx.StackTrace);
            }
        }
        catch (Exception ex)
        {
            // 其他非 ReflectionTypeLoadException 的异常
            Console.WriteLine("=== 未处理异常 ===");
            Console.WriteLine(ex);
        }

        PrintStatus();

        // 1. 初始化加载
        ConfigManager.Load();
        WpfConfig.ReadRoomBlacklist(); // 原有的特殊读取逻辑

        // 2. 交互逻辑
        if (!File.Exists("ApplyConfig") || !File.Exists("config.json"))
        {
            if (File.Exists("config.json"))
            {
                var res = uz.q("检测到配置文件，是否直接加载运行?", "启动选择", "直接加载", "进入设置");
                if (res != MessageBoxResult.OK) ShowConfigWindow();
            }
            else
            {
                ShowConfigWindow();
            }
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
        var win = new System.Windows.Window
        {
            Title = "设置",
            Width = 420, Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = Brushes.White,
            FontFamily = new FontFamily("Segoe UI, Microsoft YaHei"),
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.SingleBorderWindow,
            Topmost = WpfConfig.IsWindowTopMost
        };

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });

        var scroll = new ScrollViewer
            { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(20, 10, 20, 10) };
        var container = new StackPanel();
        scroll.Content = container;
        Grid.SetRow(scroll, 0);

        var topMostCheck = new CheckBox
        {
            Content = "置顶",
            HorizontalAlignment = HorizontalAlignment.Right,
            IsChecked = WpfConfig.IsWindowTopMost,
            Margin = new Thickness(0, 0, 0, 8)
        };
        topMostCheck.Checked += (s, e) => win.Topmost = true;
        topMostCheck.Unchecked += (s, e) => win.Topmost = false;
        container.Children.Add(topMostCheck);

        var controlMap = new Dictionary<string, FrameworkElement>();
        var categories = ConfigManager.Registry.Select(x => x.Category).Distinct();

        foreach (var cat in categories)
        {
            // 分类标题
            container.Children.Add(new TextBlock
            {
                Text = cat,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444")),
                Margin = new Thickness(0, 15, 0, 8)
            });

            // 分类卡片区域 (简洁线条风格)
            var card = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEE")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFAFA")),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var cardContent = new StackPanel();
            foreach (var item in ConfigManager.Registry.Where(x => x.Category == cat))
            {
                var field = typeof(WpfConfig).GetField(item.Key);
                var val = field.GetValue(null);

                if (item.FieldType == typeof(bool))
                {
                    var cb = new CheckBox
                    {
                        Content = item.Description,
                        IsChecked = (bool)val,
                        Margin = new Thickness(0, 6, 0, 6),
                        FontSize = 13
                    };
                    cardContent.Children.Add(cb);
                    controlMap[item.Key] = cb;
                }
                else
                {
                    var sp = new StackPanel { Margin = new Thickness(0, 6, 0, 6) };
                    sp.Children.Add(new TextBlock
                    {
                        Text = item.Description, FontSize = 12, Foreground = Brushes.Gray,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                    var tb = new TextBox
                    {
                        Text = val?.ToString(),
                        Height = 28,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Padding = new Thickness(5, 0, 5, 0),
                        Background = Brushes.White,
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"))
                    };
                    sp.Children.Add(tb);
                    cardContent.Children.Add(sp);
                    controlMap[item.Key] = tb;
                }
            }

            card.Child = cardContent;
            container.Children.Add(card);
        }

        // 底部保存按钮区
        var footer = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F3F3")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEE")),
            BorderThickness = new Thickness(0, 1, 0, 0)
        };
        var btnSave = new Button
        {
            Content = "保存并应用配置",
            Width = 160, Height = 36,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")), // Windows 蓝色
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontWeight = FontWeights.SemiBold
        };

        // 悬停变色
        btnSave.MouseEnter += (s, e) =>
            btnSave.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#005A9E"));
        btnSave.MouseLeave += (s, e) =>
            btnSave.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));

        btnSave.Click += (s, e) =>
        {
            foreach (var item in ConfigManager.Registry)
            {
                var field = typeof(WpfConfig).GetField(item.Key);
                var ctrl = controlMap[item.Key];
                if (ctrl is CheckBox cb) field.SetValue(null, cb.IsChecked);
                else if (ctrl is TextBox tb) field.SetValue(null, Convert.ChangeType(tb.Text, item.FieldType));
            }

            var error = false;

            // ================== 新增：验证自定义JVM参数 ==================
            string customJvm = WpfConfig.CustomJVMArguments?.Trim();
            if (!string.IsNullOrEmpty(customJvm))
            {
                // 1. 检查引号是否成对闭合
                if (customJvm.Count(c => c == '"') % 2 != 0)
                {
                    MessageBox.Show("自定义JVM参数中的引号不闭合，请检查是否漏掉了双引号!", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    error = true;
                }
                // 2. 检查格式是否正确 (JVM 参数一般必须以 - 开头)
                else if (!customJvm.StartsWith("-"))
                {
                    MessageBox.Show("自定义JVM参数格式错误！合法的参数必须以 '-' 开头 (例如: -Xmx2G 或 -key value)", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    error = true;
                }
            }

            // 检查基岩版路径合法性
            if (!error)
            {
                try
                {
                    Path.GetFullPath(WpfConfig.BedrockPath);
                }
                catch (Exception)
                {
                    MessageBox.Show("请输入正确的基岩版路径!", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    error = true;
                }
            }
    
            // 检查网易服务器地址
            if (!error)
            {
                try
                {
                    WpfConfig.ServerListUri = new Uri(WpfConfig.ServerListUrl);
                }
                catch (Exception)
                {
                    MessageBox.Show("请输入正确的网易服务器地址", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    error = true;
                }
            }

            if (!error)
            {
                ConfigManager.Save();
                win.Close();
            }
        };

        footer.Child = btnSave;
        Grid.SetRow(footer, 1);

        mainGrid.Children.Add(scroll);
        mainGrid.Children.Add(footer);
        win.Content = mainGrid;
        win.ShowDialog();
    }
    
    public static void ApplyRuntimeSettings()
    {
        // ==========================================
        // Web服务器 生命周期管理
        // ==========================================
        if (WpfConfig.IsStartWebSocket)
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
                        WpfConfig.DefaultLogger.Info($"[Web] 旧服务器已关闭 (端口: {_currentServerPort})");
                    }
                    catch (Exception ex)
                    {
                        WpfConfig.DefaultLogger.Error($"[Web] 关闭旧服务器失败: {ex.Message}");
                    }
                }

                // 启动新服务器
                WpfConfig.Default_HttpAddress = $"http://127.0.0.1:{WpfConfig.HttpPort}/";
                _runningServer = new SimpleHttpServer();
                _currentServerPort = WpfConfig.HttpPort; // 记录最新端口

                Task.Run(() => _runningServer.Start(WpfConfig.Default_HttpAddress));
                WpfConfig.DefaultLogger.Info($"[Web] 服务器已启动: {WpfConfig.Default_HttpAddress}");
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
                    WpfConfig.DefaultLogger.Info($"[Web] 服务器已被用户关闭");
                }
                catch (Exception ex)
                {
                    WpfConfig.DefaultLogger.Error($"[Web] 关闭服务器失败: {ex.Message}");
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
        WpfConfig.Mac_Addr = Get_MacAddr();
        WpfConfig.Random_Mac_Addr = ConvertToOriginalFormat(GenerateRandomMacAddress());
        
        // ==========================================
        // 模组注入逻辑
        // ==========================================
        if (WpfConfig.EnableModsInject)
        {
            var modsInjectPath = Path.Combine(Directory.GetCurrentDirectory(), "ModsInject");
            if (!Directory.Exists(modsInjectPath)) Directory.CreateDirectory(modsInjectPath);
            WpfConfig.DefaultLogger.Info("[ModsInject] 模组注入已启用，请将模组文件放入以下文件夹：");
            WpfConfig.DefaultLogger.Info($"[ModsInject] {modsInjectPath}");
            Process.Start("explorer.exe", modsInjectPath);
        }

        if (WpfConfig.ShowCustomServer)
        {
            _ = LoadRemoteServersAsync();
        }
        else
        {
            WpfConfig.CustomRecentList.Clear();
        }
        
        try
        {
            WpfConfig.ServerListUri = new Uri(WpfConfig.ServerListUrl);
        }
        catch (Exception)
        {
            WpfConfig.DefaultLogger.Error("错误的网易服务器地址, 将会改成默认地址");
        }

        if (string.IsNullOrWhiteSpace(WpfConfig.LanGameNicknameFilterString))
        {
            // 为空时直接给空列表，避免残留旧数据
            WpfConfig.LanGameNicknameFilter = new List<string>();
        }
        else
        {
            WpfConfig.LanGameNicknameFilter = WpfConfig.LanGameNicknameFilterString
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) // 去掉空字符串
                .Select(s => s.Trim())                                      // 去掉首尾空格
                .Where(s => !string.IsNullOrEmpty(s))                       // 再去掉纯空白项
                .ToList();
        }
        
        // 获取 Minecraft 版本列表
        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Clear();
    }

    private static void PrintStatus()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(
            "\u2588\u2588\u2557    \u2588\u2588\u2557\u2588\u2588\u2588\u2588\u2588\u2588\u2557 \u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2557\u2588\u2588\u2557  \u2588\u2588\u2557 \u2588\u2588\u2588\u2588\u2588\u2588\u2557  \u2588\u2588\u2588\u2588\u2588\u2588\u2557 \u2588\u2588\u2557  \u2588\u2588\u2557\n\u2588\u2588\u2551    \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2554\u2550\u2550\u2550\u2550\u255d\u2588\u2588\u2551  \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2554\u2550\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2551 \u2588\u2588\u2554\u255d\n\u2588\u2588\u2551 \u2588\u2557 \u2588\u2588\u2551\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2588\u2588\u2588\u2557  \u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2588\u2588\u2588\u2554\u255d \n\u2588\u2588\u2551\u2588\u2588\u2588\u2557\u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2550\u255d \u2588\u2588\u2554\u2550\u2550\u255d  \u2588\u2588\u2554\u2550\u2550\u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2588\u2588\u2557 \n\u255a\u2588\u2588\u2588\u2554\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2551     \u2588\u2588\u2551     \u2588\u2588\u2551  \u2588\u2588\u2551\u255a\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u255a\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2551  \u2588\u2588\u2557\n \u255a\u2550\u2550\u255d\u255a\u2550\u2550\u255d \u255a\u2550\u255d     \u255a\u2550\u255d     \u255a\u2550\u255d  \u255a\u2550\u255d \u255a\u2550\u2550\u2550\u2550\u2550\u255d  \u255a\u2550\u2550\u2550\u2550\u2550\u255d \u255a\u2550\u255d  \u255a\u2550\u255d\n                                                            ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[INFO]控制台输出成功启动!");
        Console.WriteLine(
            $"[WPFLauncherHook]成功Hook网易我的世界启动器,感谢使用\n当前Hook版本:{WpfConfig.Version}\ngithub链接:https://github.com/daijunhaoMinecraft/WPFLauncher_Hook\nBy:daijunhao (QQ: 3352133106)");
        try
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(0, 0, 3);
            httpClient.DefaultRequestHeaders.Clear();
            var messageData = httpClient
                .GetAsync("https://gitee.com/dai-junhao-123/app-config/raw/master/HookConfig/AppInfo.json").Result;
            var messageJson = JObject.Parse(messageData.Content.ReadAsStringAsync().Result);
            Console.WriteLine($"公告:\n{messageJson["announcement"]}");
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] 获取公告失败:{e} \n {e.StackTrace}");
        }

        Console.ResetColor();
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
                    WpfConfig.CustomRecentList.Clear();

                    foreach (var response in remoteList)
                    {
                        // 使用 entity_id 作为键，与 InitCustomServers 保持一致
                        string key = response.NetGameEntity?.EntityId;
                        if (!string.IsNullOrEmpty(key))
                        {
                            WpfConfig.CustomRecentList.Add(
                                new Tuple<string, NetGameResponse>(key, response)
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 失败时可回退到本地硬编码列表，保证程序继续运行
                Console.WriteLine($"获取远程服务器列表失败：{ex.Message}");
                // 如有需要，可调用 InitCustomServers() 作为备用
            }
        }
    }
}