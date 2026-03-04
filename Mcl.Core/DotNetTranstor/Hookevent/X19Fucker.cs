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
using Newtonsoft.Json;
using DotNetTranstor.Tools;
using Newtonsoft.Json.Linq;
using WPFLauncher.Common;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Util;

namespace DotNetTranstor.Hookevent
{
    internal class X19Fucker : IMethodHook
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [OriginalMethod]
        public static void X19_Fever_bypass() { }

        [HookMethod("WPFLauncher.Manager.apm", "aj", "X19_Fever_bypass")]
        public bool Fever_False()
        {
            AllocConsole();
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

            // 3. 应用运行逻辑
            ApplyRuntimeSettings();
            return false;
        }

        private void InitIdentity()
        {
            string mac = Get_MacAddr();
            string randomMac = GenerateRandomMacAddress();
            Path_Bool.Mac_Addr = mac;
            Path_Bool.Random_Mac_Addr = ConvertToOriginalFormat(randomMac);

            Console.WriteLine($"[Identity] Mac: {mac} -> Random: {randomMac}");
            
            // 路径设置
            string javaPath = Path.Combine(tb.n, "Game", ".minecraft");
            Directory.CreateDirectory(javaPath);
            Path_Bool.JavaGamePath = javaPath;
            aze<axh>.Instance.App.JavaGamePath = javaPath;
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
        
        private void ShowConfigWindow()
        {
            var win = new Window {
                Title = "设置",
                Width = 420, Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Brushes.White,
                FontFamily = new FontFamily("Segoe UI, Microsoft YaHei"),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(20, 10, 20, 10) };
            var container = new StackPanel();
            scroll.Content = container;
            Grid.SetRow(scroll, 0);

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
                ConfigManager.Save();
                // ApplyRuntimeSettings();
                win.Close();
            };

            footer.Child = btnSave;
            Grid.SetRow(footer, 1);

            mainGrid.Children.Add(scroll);
            mainGrid.Children.Add(footer);
            win.Content = mainGrid;
            win.ShowDialog();
        }

        private void ApplyRuntimeSettings()
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

        private void PrintStatus()
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