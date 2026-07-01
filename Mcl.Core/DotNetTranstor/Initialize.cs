using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DotNetDetour;
using DotNetTranstor.Hookevent;
using DotNetTranstor.Tools;
using Newtonsoft.Json.Linq;
using WPFLauncher.Util;

// 1. 手动补齐 .NET 4.8.1 缺失的 ModuleInitializerAttribute 特性
// 注意：命名空间必须严格是 System.Runtime.CompilerServices
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ModuleInitializerAttribute : Attribute
    {
    }
}

namespace Mcl.Core // 替换为你的项目命名空间
{
    // 2. 编写启动器类
    public static class HookBootstrapper
    {
        // 导入 AllocConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
		        
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll")]
        private static extern uint GetConsoleOutputCP();

        // 标记为 ModuleInitializer
        [ModuleInitializer]
        internal static void InitializeOnLoad()
        {
            if (!File.Exists("DisableConsole"))
            {
            	// 分配一个新的控制台
            	AllocConsole();
            
            	const uint CP_GBK = 936;
            
            	// 1. 强制设置控制台输出代码页为 936 (GBK)
            	SetConsoleOutputCP(CP_GBK);
            
            	// 2. 设置 .NET 控制台输出编码为 GBK
            	Console.OutputEncoding = Encoding.GetEncoding(936);
            
            	// 3. 重定向输出流，并显式指定编码！
            	var writer = new StreamWriter(
            		Console.OpenStandardOutput(),
            		Console.OutputEncoding  // 👈 关键：使用一致的编码
            	);
            	writer.AutoFlush = true;
            	Console.SetOut(writer);
            	Console.CursorVisible = false;
            }
            try
            {
            	MethodHook.Install(null);
            }
            catch (ReflectionTypeLoadException ex)
            {
            	Console.WriteLine("=== ReflectionTypeLoadException: 部分类型加载失败 ===");
            
            	// 输出成功加载的类型（可选）
            	if (ex.Types != null)
            	{
            		var loadedTypes = ex.Types.Where(t => t != null).ToArray();
            		Console.WriteLine($"成功加载 {loadedTypes.Length} 个类型:");
            		foreach (var type in loadedTypes)
            		{
            			Console.WriteLine($"  ✔ {type?.FullName}");
            		}
            	}
            
            	// 输出加载失败的异常信息
            	Console.WriteLine($"\n失败的加载异常 ({ex.LoaderExceptions.Length} 个):");
            	for (int i = 0; i < ex.LoaderExceptions.Length; i++)
            	{
            		var loaderEx = ex.LoaderExceptions[i];
            		Console.WriteLine($"--- 加载异常 #{i + 1} ---");
            		Console.WriteLine(loaderEx.Message);
            
            		// 如果是文件找不到，输出更详细信息
            		if (loaderEx is FileNotFoundException fileEx && !string.IsNullOrEmpty(fileEx.FileName))
            		{
            			Console.WriteLine($"缺少程序集: {fileEx.FileName}");
            		}
            
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
            Path_Bool.ReadRoomBlacklist(); // 原有的特殊读取逻辑

            // 2. 交互逻辑
            if (File.Exists("config.json"))
            {
	            var res = uz.q("检测到配置文件，是否直接加载运行?", "启动选择", "直接加载", "进入设置", "");
	            if (res != MessageBoxResult.OK) ShowConfigWindow();
            }
            else
            {
	            ShowConfigWindow();
            }

            // 如果启用了模组注入，打开 ModsInject 文件夹并提示
            if (Path_Bool.EnableModsInject)
            {
	            string modsInjectPath = Path.Combine(Directory.GetCurrentDirectory(), "ModsInject");
	            if (!Directory.Exists(modsInjectPath))
	            {
		            Directory.CreateDirectory(modsInjectPath);
	            }
	            Console.ForegroundColor = ConsoleColor.Green;
	            Console.WriteLine($"[ModsInject] 模组注入已启用，请将模组文件放入以下文件夹：");
	            Console.WriteLine($"[ModsInject] {modsInjectPath}");
	            Console.ResetColor();
	            System.Diagnostics.Process.Start("explorer.exe", modsInjectPath);
            }

            InitIdentity();

            // 3. 应用运行逻辑
            ApplyRuntimeSettings();
        }
        
        
        private static void InitIdentity()
        {
            string mac = Get_MacAddr();
            string randomMac = GenerateRandomMacAddress();
            Path_Bool.Mac_Addr = mac;
            Path_Bool.Random_Mac_Addr = ConvertToOriginalFormat(randomMac);

            Console.WriteLine($"[Identity] Mac: {mac} -> Random: {randomMac}");
            
            // 路径设置
            // string javaPath = Path.Combine(tb.n, "Game", ".minecraft");
            // Directory.CreateDirectory(javaPath);
            // Path_Bool.JavaGamePath = javaPath;
            // WPFLauncher.Common.azf<WPFLauncher.Manager.Configuration.axi>.Instance.App.JavaGamePath = javaPath;
        }

        // --- 工具函数 (保持不变) ---
        public static string GenerateRandomMacAddress() {
            Random r = new Random();
            byte[] b = new byte[6]; r.NextBytes(b);
            b[0] = (byte)(b[0] & 254);
            return string.Join(":", b.Select(x => x.ToString("X2")));
        }
        public static string ConvertToOriginalFormat(string m) => m.Replace(":", "");
        public static string Get_MacAddr() {
            try { return NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault()?.GetPhysicalAddress().ToString() ?? "000000000000"; }
            catch { return "000000000000"; }
        }
        
        private static void ShowConfigWindow()
        {
            var win = new Window {
                Title = "设置",
                Width = 420, Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Brushes.White,
                FontFamily = new FontFamily("Segoe UI, Microsoft YaHei"),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Topmost = Path_Bool.IsWindowTopMost
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(20, 10, 20, 10) };
            var container = new StackPanel();
            scroll.Content = container;
            Grid.SetRow(scroll, 0);

            var topMostCheck = new CheckBox
            {
                Content = "置顶",
                HorizontalAlignment = HorizontalAlignment.Right,
                IsChecked = Path_Bool.IsWindowTopMost,
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
                container.Children.Add(new TextBlock { 
                    Text = cat, 
                    FontSize = 14, 
                    FontWeight = FontWeights.Bold, 
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444")),
                    Margin = new Thickness(0, 15, 0, 8)
                });

                // 分类卡片区域 (简洁线条风格)
                var card = new Border {
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
                    var field = typeof(Path_Bool).GetField(item.Key);
                    var val = field.GetValue(null);

                    if (item.FieldType == typeof(bool))
                    {
                        var cb = new CheckBox {
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
                        sp.Children.Add(new TextBlock { Text = item.Description, FontSize = 12, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 4) });
                        var tb = new TextBox { 
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
            var footer = new Border { 
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F3F3")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEE")),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };
            var btnSave = new Button {
                Content = "保存并应用配置",
                Width = 160, Height = 36,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")), // Windows 蓝色
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold
            };
            
            // 悬停变色
            btnSave.MouseEnter += (s, e) => btnSave.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#005A9E"));
            btnSave.MouseLeave += (s, e) => btnSave.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));

            btnSave.Click += (s, e) => {
                foreach (var item in ConfigManager.Registry)
                {
                    var field = typeof(Path_Bool).GetField(item.Key);
                    var ctrl = controlMap[item.Key];
                    if (ctrl is CheckBox cb) field.SetValue(null, cb.IsChecked);
                    else if (ctrl is TextBox tb) field.SetValue(null, Convert.ChangeType(tb.Text, item.FieldType));
                }
                bool error = false;
                // 检查基岩版路径合法性
                try
                {
                    Path.GetFullPath(Path_Bool.BedrockPath);
                }
                catch (Exception)
                {
                    MessageBox.Show("请输入正确的基岩版路径!", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    error = true;
                }
                if (!error)
                {
                    ConfigManager.Save();

                    // 如果启用了模组注入，打开 ModsInject 文件夹并提示
                    if (Path_Bool.EnableModsInject)
                    {
                        string modsInjectPath = Path.Combine(Directory.GetCurrentDirectory(), "ModsInject");
                        if (!Directory.Exists(modsInjectPath))
                        {
                            Directory.CreateDirectory(modsInjectPath);
                        }
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[ModsInject] 模组注入已启用，请将模组文件放入以下文件夹：");
                        Console.WriteLine($"[ModsInject] {modsInjectPath}");
                        Console.ResetColor();
                        System.Diagnostics.Process.Start("explorer.exe", modsInjectPath);
                    }

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

        private static void ApplyRuntimeSettings()
        {
            // 这里放你原本在 Save 按钮里的那些初始化逻辑
            if (Path_Bool.IsStartWebSocket)
            {
                Path_Bool.Default_HttpAddress = $"http://127.0.0.1:{Path_Bool.HttpPort}/";
                var server = new SimpleHttpServer(); 
                Task.Run(() => server.Start(Path_Bool.Default_HttpAddress));
                Console.WriteLine($"[Web] 服务器已启动: {Path_Bool.Default_HttpAddress}");
            }
            
            // Mac 地址逻辑
            Path_Bool.Mac_Addr = Get_MacAddr();
            Path_Bool.Random_Mac_Addr = ConvertToOriginalFormat(GenerateRandomMacAddress());
            
            // ... 其余逻辑保持不变 ...
        }

        private static void PrintStatus()
        {
            // Console.ForegroundColor = ConsoleColor.Green;
            // Console.WriteLine("========================================");
            // Console.WriteLine($"  X19Fucker v{Path_Bool.Version}  ");
            // Console.WriteLine("  Status: System Hooked Successfully    ");
            // try
            // {
            //     HttpClient httpClient = new HttpClient();
            //     httpClient.DefaultRequestHeaders.Clear();
            //     HttpResponseMessage messageData = httpClient.GetAsync("https://gitee.com/dai-junhao-123/app-config/raw/master/HookConfig/AppInfo.json").Result;
            //     JObject messageJson = JObject.Parse(messageData.Content.ReadAsStringAsync().Result);
            //     Console.WriteLine($"公告:\n{messageJson["announcement"]}");
            // }
            // catch (Exception e)
            // {
            //     Console.WriteLine($"[ERROR] 获取公告失败:{e} \n {e.StackTrace}");
            // }
            // Console.WriteLine("========================================");
            // Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\u2588\u2588\u2557    \u2588\u2588\u2557\u2588\u2588\u2588\u2588\u2588\u2588\u2557 \u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2557\u2588\u2588\u2557  \u2588\u2588\u2557 \u2588\u2588\u2588\u2588\u2588\u2588\u2557  \u2588\u2588\u2588\u2588\u2588\u2588\u2557 \u2588\u2588\u2557  \u2588\u2588\u2557\n\u2588\u2588\u2551    \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2554\u2550\u2550\u2550\u2550\u255d\u2588\u2588\u2551  \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2554\u2550\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2551 \u2588\u2588\u2554\u255d\n\u2588\u2588\u2551 \u2588\u2557 \u2588\u2588\u2551\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2588\u2588\u2588\u2557  \u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2588\u2588\u2588\u2554\u255d \n\u2588\u2588\u2551\u2588\u2588\u2588\u2557\u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2550\u255d \u2588\u2588\u2554\u2550\u2550\u255d  \u2588\u2588\u2554\u2550\u2550\u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2588\u2588\u2557 \n\u255a\u2588\u2588\u2588\u2554\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2551     \u2588\u2588\u2551     \u2588\u2588\u2551  \u2588\u2588\u2551\u255a\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u255a\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2551  \u2588\u2588\u2557\n \u255a\u2550\u2550\u255d\u255a\u2550\u2550\u255d \u255a\u2550\u255d     \u255a\u2550\u255d     \u255a\u2550\u255d  \u255a\u2550\u255d \u255a\u2550\u2550\u2550\u2550\u2550\u255d  \u255a\u2550\u2550\u2550\u2550\u2550\u255d \u255a\u2550\u255d  \u255a\u2550\u255d\n                                                            ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[INFO]控制台输出成功启动!");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WPFLauncherHook]成功Hook网易我的世界启动器,感谢使用\n当前Hook版本:{Path_Bool.Version}\ngithub链接:https://github.com/daijunhaoMinecraft/WPFLauncher_Hook\nBy:daijunhao");
            try
            {
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = new TimeSpan(0, 0, 3);
                httpClient.DefaultRequestHeaders.Clear();
                HttpResponseMessage messageData = httpClient.GetAsync("https://gitee.com/dai-junhao-123/app-config/raw/master/HookConfig/AppInfo.json").Result;
                JObject messageJson = JObject.Parse(messageData.Content.ReadAsStringAsync().Result);
                Console.WriteLine($"公告:\n{messageJson["announcement"]}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] 获取公告失败:{e} \n {e.StackTrace}");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}