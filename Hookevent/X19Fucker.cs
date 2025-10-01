using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DotNetTranstor.Tools;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Common;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Network.Message;
using WPFLauncher.Util;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
    internal class X19Fucker : IMethodHook
    {
        // 导入 AllocConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        
        private const string CONFIG_FILE = "config.json";
        
        private class ConfigSettings
        {
            public bool IsBypassGameUpdate_Bedrock { get; set; }
            public bool IsEnableX64mc { get; set; }
            public bool IsStartWebSocket { get; set; }
            public bool IsDebug { get; set; }
            public bool EnableRoomBlacklist { get; set; }
            public int MaxRoomCount { get; set; }
            public int HttpPort { get; set; }
            public string NeteaseUpdateDomain { get; set; }
            public bool AlwaysSaveWorld { get; set; }
            public bool IsCustomIP { get; set;}
        }

        public static void SaveConfig()
        {
            var config = new ConfigSettings
            {
                IsBypassGameUpdate_Bedrock = Path_Bool.IsBypassGameUpdate_Bedrock,
                IsEnableX64mc = Path_Bool.IsEnableX64mc,
                IsStartWebSocket = Path_Bool.IsStartWebSocket,
                IsDebug = Path_Bool.IsDebug,
                EnableRoomBlacklist = Path_Bool.EnableRoomBlacklist,
                MaxRoomCount = Path_Bool.MaxRoomCount,
                HttpPort = Path_Bool.HttpPort,
                NeteaseUpdateDomain = Path_Bool.NeteaseUpdateDomainhttp,
                AlwaysSaveWorld = Path_Bool.AlwaysSaveWorld,
                IsCustomIP = Path_Bool.IsCustomIP
            };

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(CONFIG_FILE, json);
            Console.WriteLine("[配置] 配置已保存到 " + CONFIG_FILE);
        }

        private bool LoadConfig()
        {
            if (!File.Exists(CONFIG_FILE))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(CONFIG_FILE);
                var config = JsonConvert.DeserializeObject<ConfigSettings>(json);

                Path_Bool.IsBypassGameUpdate_Bedrock = config.IsBypassGameUpdate_Bedrock;
                Path_Bool.IsEnableX64mc = config.IsEnableX64mc;
                Path_Bool.IsStartWebSocket = config.IsStartWebSocket;
                Path_Bool.IsDebug = config.IsDebug;
                Path_Bool.EnableRoomBlacklist = config.EnableRoomBlacklist;
                Path_Bool.MaxRoomCount = config.MaxRoomCount;
                Path_Bool.HttpPort = config.HttpPort > 0 ? config.HttpPort : 4601;
                Path_Bool.NeteaseUpdateDomainhttp = config.NeteaseUpdateDomain ?? "";
                Path_Bool.AlwaysSaveWorld = config.AlwaysSaveWorld;
                Path_Bool.IsCustomIP = config.IsCustomIP;

                Console.WriteLine("[配置] 成功加载配置文件");
                Console.WriteLine($"[配置] 绕过基岩版更新: {config.IsBypassGameUpdate_Bedrock}");
                Console.WriteLine($"[配置] X64基岩版: {config.IsEnableX64mc}");
                Console.WriteLine($"[配置] Web服务器: {config.IsStartWebSocket}");
                Console.WriteLine($"[配置] 详细日志: {config.IsDebug}");
                Console.WriteLine($"[配置] 房间黑名单: {config.EnableRoomBlacklist}");
                Console.WriteLine($"[配置] 最大房间数: {config.MaxRoomCount}");
                Console.WriteLine($"[配置] HTTP端口: {Path_Bool.HttpPort}");
                Console.WriteLine($"[配置] 网易更新域名: {Path_Bool.NeteaseUpdateDomainhttp}");
                Console.WriteLine($"[配置] 保存房间提醒: {Path_Bool.AlwaysSaveWorld}:");
                Console.WriteLine($"[配置] 自定义IP(联机大厅-自定义IP进入服务器): {Path_Bool.IsCustomIP}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[配置] 加载配置文件失败: " + ex.Message);
                return false;
            }
        }
        
        //X19_发烧平台绕过Hook
        [OriginalMethod]
        public static void X19_Fever_bypass()
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.Manager.apl", "aj", "X19_Fever_bypass")]
        public bool Fever_False()
        {
            // 分配一个新的控制台
            AllocConsole();
            // 重定向输出流到控制台
            var writer = new StreamWriter(Console.OpenStandardOutput());
            writer.AutoFlush = true;
            Console.SetOut(writer);
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;
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
            // 添加应用程序退出事件处理
            Application.Current.Exit += (sender, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[应用程序] 应用程序正在退出，按Enter键继续...");
                Console.ReadLine();
            };

            // 添加应用程序域卸载事件处理
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[程序] 程序正在退出，按Enter键继续...");
                Console.ReadLine();
            };

            // 检查是否存在配置文件并询问是否加载
            if (File.Exists(CONFIG_FILE))
            {
                MessageBoxResult loadConfigResult = uy.q("检测到上次的配置文件，是否加载?", "", "加载", "不加载", "");
                if (loadConfigResult == MessageBoxResult.OK)
                {
                    if (LoadConfig())
                    {
                        // 如果配置文件加载成功，并且启用了Web服务器，则启动它
                        if (Path_Bool.IsStartWebSocket)
                        {
                            //WebSocketHelper.StartWebSocketServer();
                            var server = new SimpleHttpServer();
                            Task server1Task = Task.Run(() => server.Start("http://127.0.0.1:4601/"));
                            Console.WriteLine("[INFO]Web服务器已启动!");
                            if (string.IsNullOrEmpty(Path_Bool.NeteaseUpdateDomainhttp) || 
                                Path_Bool.NeteaseUpdateDomainhttp == "https://x19.update.netease.com")
                            {
                                Console.WriteLine("[INFO]NeteaseUpdateDomain为默认值,将使用默认的NeteaseUpdateDomain");
                            }
                            else
                            {
                                Console.WriteLine($"[INFO]NeteaseUpdateDomain已修改为:{Path_Bool.NeteaseUpdateDomainhttp}");
                                azd<axg>.Instance.e();
                            }
                        }
                        return false;
                    }
                }
            }

            // 创建主配置窗口
            var configWindow = new Window();
            configWindow.Title = "WPFLauncher配置";
            configWindow.Width = 600;
            configWindow.Height = 600;
            configWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            configWindow.Background = new SolidColorBrush(Colors.WhiteSmoke);

            // 创建主布局
            var mainPanel = new StackPanel();
            mainPanel.Margin = new Thickness(20);
            
            var scrollViewer = new ScrollViewer();
            scrollViewer.Content = mainPanel;
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            configWindow.Content = scrollViewer;

            // 标题
            var titleBlock = new TextBlock();
            titleBlock.Text = "WPFLauncher 配置面板";
            titleBlock.FontSize = 24;
            titleBlock.FontWeight = FontWeights.Bold;
            titleBlock.HorizontalAlignment = HorizontalAlignment.Center;
            titleBlock.Margin = new Thickness(0, 0, 0, 20);
            mainPanel.Children.Add(titleBlock);

            // 创建配置项
            var bypassUpdateCheckbox = new CheckBox();
            bypassUpdateCheckbox.Content = "绕过基岩版更新";
            bypassUpdateCheckbox.IsChecked = Path_Bool.IsBypassGameUpdate_Bedrock;
            bypassUpdateCheckbox.Margin = new Thickness(0, 5, 0, 5);
            bypassUpdateCheckbox.FontSize = 14;
            mainPanel.Children.Add(bypassUpdateCheckbox);

            var x64Checkbox = new CheckBox();
            x64Checkbox.Content = "使用X64版本";
            x64Checkbox.IsChecked = Path_Bool.IsEnableX64mc;
            x64Checkbox.Margin = new Thickness(0, 5, 0, 5);
            x64Checkbox.FontSize = 14;
            mainPanel.Children.Add(x64Checkbox);

            var webServerCheckbox = new CheckBox();
            webServerCheckbox.Content = "启用Web服务器";
            webServerCheckbox.IsChecked = Path_Bool.IsStartWebSocket;
            webServerCheckbox.Margin = new Thickness(0, 5, 0, 5);
            webServerCheckbox.FontSize = 14;
            mainPanel.Children.Add(webServerCheckbox);

            var debugCheckbox = new CheckBox();
            debugCheckbox.Content = "启用详细日志";
            debugCheckbox.IsChecked = Path_Bool.IsDebug;
            debugCheckbox.Margin = new Thickness(0, 5, 0, 5);
            debugCheckbox.FontSize = 14;
            mainPanel.Children.Add(debugCheckbox);

            var blacklistCheckbox = new CheckBox();
            blacklistCheckbox.Content = "启用房间黑名单";
            blacklistCheckbox.IsChecked = Path_Bool.EnableRoomBlacklist;
            blacklistCheckbox.Margin = new Thickness(0, 5, 0, 5);
            blacklistCheckbox.FontSize = 14;
            mainPanel.Children.Add(blacklistCheckbox);
            
            var AlwaysSaveWorldTip = new CheckBox();
            AlwaysSaveWorldTip.Content = "保存房间提醒";
            AlwaysSaveWorldTip.IsChecked = Path_Bool.AlwaysSaveWorld;
            AlwaysSaveWorldTip.Margin = new Thickness(0, 5, 0, 5);
            AlwaysSaveWorldTip.FontSize = 14;
            //AlwaysSaveWorldTip.ToolTip = "启用后将自动解密加密的模组文件";
            mainPanel.Children.Add(AlwaysSaveWorldTip);
            
            var CustomIpCheckBox = new CheckBox();
            CustomIpCheckBox.Content = "联机大厅使用自定义IP加入到不同的服务器中";
            CustomIpCheckBox.IsChecked = Path_Bool.AlwaysSaveWorld;
            CustomIpCheckBox.Margin = new Thickness(0, 5, 0, 5);
            CustomIpCheckBox.FontSize = 14;
            mainPanel.Children.Add(CustomIpCheckBox);

            // 房间数量设置
            var roomPanel = new StackPanel();
            roomPanel.Margin = new Thickness(0, 10, 0, 10);
            
            var roomLabel = new TextBlock();
            roomLabel.Text = "联机大厅最大房间数:";
            roomLabel.Margin = new Thickness(0, 0, 10, 5);
            
            var roomInput = new TextBox();
            roomInput.Width = 100;
            roomInput.Text = Path_Bool.MaxRoomCount.ToString();
            
            roomPanel.Children.Add(roomLabel);
            roomPanel.Children.Add(roomInput);
            mainPanel.Children.Add(roomPanel);

            // 网易更新域名设置
            var domainPanel = new StackPanel();
            domainPanel.Margin = new Thickness(0, 10, 0, 10);
            
            var domainLabel = new TextBlock();
            domainLabel.Text = "自定义网易更新域名:";
            domainLabel.Margin = new Thickness(0, 0, 10, 5);
            
            var domainInput = new TextBox();
            domainInput.Width = 300;
            domainInput.Text = string.IsNullOrEmpty(Path_Bool.NeteaseUpdateDomainhttp) ? 
                "https://x19.update.netease.com" : Path_Bool.NeteaseUpdateDomainhttp;
            domainInput.ToolTip = "默认为https://x19.update.netease.com，留空则使用默认值";
            
            // 添加TextChanged事件，实现自动保存
            // domainInput.TextChanged += (s, e) => {
            //     Path_Bool.NeteaseUpdateDomainhttp = domainInput.Text.Trim();
            //     SaveConfig();
            //     Console.WriteLine($"[配置] 网易更新域名已自动保存: {Path_Bool.NeteaseUpdateDomainhttp}");
            // };
            string TemplatePath = Directory.GetCurrentDirectory() + "/ConfigTemplate";
            string[] jsonFiles = new string[]{};
            if (!Directory.Exists(TemplatePath))
            {
                Directory.CreateDirectory(TemplatePath);
            }
            else
            {
                // 获取该路径下所有的.json文件
                jsonFiles = Directory.GetFiles(TemplatePath, "*.json", SearchOption.AllDirectories);
            }
            domainPanel.Children.Add(domainLabel);
            domainPanel.Children.Add(domainInput);
            mainPanel.Children.Add(domainPanel);

            // 端口设置面板 - 仅在启用Web服务器时可编辑
            var portsPanel = new StackPanel();
            portsPanel.Margin = new Thickness(0, 10, 0, 10);
            
            var portsLabel = new TextBlock();
            portsLabel.Text = "服务器端口设置:";
            portsLabel.Margin = new Thickness(0, 0, 0, 5);
            portsPanel.Children.Add(portsLabel);
            
            // HTTP端口
            var httpPortPanel = new StackPanel();
            httpPortPanel.Orientation = Orientation.Horizontal;
            httpPortPanel.Margin = new Thickness(0, 5, 0, 5);
            
            var httpPortLabel = new TextBlock();
            httpPortLabel.Text = "HTTP端口:";
            httpPortLabel.Margin = new Thickness(0, 0, 10, 0);
            httpPortLabel.VerticalAlignment = VerticalAlignment.Center;
            
            var httpPortInput = new TextBox();
            httpPortInput.Width = 100;
            httpPortInput.Text = Path_Bool.HttpPort.ToString();
            httpPortInput.IsEnabled = webServerCheckbox.IsChecked ?? false;
            
            httpPortPanel.Children.Add(httpPortLabel);
            httpPortPanel.Children.Add(httpPortInput);
            portsPanel.Children.Add(httpPortPanel);
            
            mainPanel.Children.Add(portsPanel);
            
            // 当Web服务器选项发生变化时更新端口控件可用性
            webServerCheckbox.Checked += (s, e) => {
                httpPortInput.IsEnabled = true;
            };
            
            webServerCheckbox.Unchecked += (s, e) => {
                httpPortInput.IsEnabled = false;
            };

            // 模板选择
            var templateLabel = new TextBlock();
            templateLabel.Text = "配置模板:";
            templateLabel.Margin = new Thickness(0, 0, 0, 5);
            mainPanel.Children.Add(templateLabel);

            var templateBox = new ComboBox();
            templateBox.Margin = new Thickness(0, 10, 0, 10);
            templateBox.Width = 200;
            foreach (var jsonFile in jsonFiles)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(jsonFile); // 获取文件名（不包含后缀）
                templateBox.Items.Add(fileNameWithoutExtension);
            } 
            templateBox.Items.Add("默认配置");
            // templateBox.Items.Add("性能优先配置"); 
            // templateBox.Items.Add("开发调试配置");
            if (File.Exists(CONFIG_FILE))
            {
                templateBox.Items.Add("上次使用的配置");
            }
            templateBox.SelectedIndex = 0;
            mainPanel.Children.Add(templateBox);

            // 按钮面板
            var buttonPanel = new StackPanel();
            buttonPanel.Orientation = Orientation.Horizontal;
            buttonPanel.HorizontalAlignment = HorizontalAlignment.Center;
            buttonPanel.Margin = new Thickness(0, 20, 0, 0);

            var saveButton = new Button();
            saveButton.Content = "保存配置";
            saveButton.Padding = new Thickness(20, 5, 20, 5);
            saveButton.Margin = new Thickness(5);
            saveButton.Background = new SolidColorBrush(Colors.ForestGreen);
            saveButton.Foreground = new SolidColorBrush(Colors.White);

            var cancelButton = new Button();
            cancelButton.Content = "取消";
            cancelButton.Padding = new Thickness(20, 5, 20, 5);
            cancelButton.Margin = new Thickness(5);

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            mainPanel.Children.Add(buttonPanel);
            // 事件处理
            templateBox.SelectionChanged += (s, e) => {
                if (templateBox.SelectedItem == null)
                    return;

                string selectedTemplate = templateBox.SelectedItem.ToString();

                if (selectedTemplate == "上次使用的配置")
                {
                    Console.WriteLine($"[LoadConfig]Result:{LoadConfig()}");
                }
                else if (selectedTemplate == "默认配置")
                {
                    Path_Bool.IsBypassGameUpdate_Bedrock = false;
                    Path_Bool.IsEnableX64mc = false;
                    Path_Bool.IsStartWebSocket = false;
                    Path_Bool.IsDebug = false;
                    Path_Bool.EnableRoomBlacklist = false;
                    Path_Bool.MaxRoomCount = 100;
                    Path_Bool.AlwaysSaveWorld = true;
                    Path_Bool.IsCustomIP = false;
                }
                else
                {
                    string json = File.ReadAllText(Path.Combine(TemplatePath, selectedTemplate + ".json"));
                    var config = JsonConvert.DeserializeObject<ConfigSettings>(json);
                    // 更新配置
                    Path_Bool.IsBypassGameUpdate_Bedrock = config.IsBypassGameUpdate_Bedrock;
                    Path_Bool.IsEnableX64mc = config.IsEnableX64mc;
                    Path_Bool.IsStartWebSocket = config.IsStartWebSocket;
                    Path_Bool.IsDebug = config.IsDebug;
                    Path_Bool.EnableRoomBlacklist = config.EnableRoomBlacklist;
                    Path_Bool.MaxRoomCount = config.MaxRoomCount;
                    Path_Bool.HttpPort = config.HttpPort > 0 ? config.HttpPort : 4601;
                    Path_Bool.NeteaseUpdateDomainhttp = config.NeteaseUpdateDomain ?? "";
                    Path_Bool.AlwaysSaveWorld = config.AlwaysSaveWorld;
                    Path_Bool.IsCustomIP = config.IsCustomIP;
                }

                // 同步UI状态
                bypassUpdateCheckbox.IsChecked = Path_Bool.IsBypassGameUpdate_Bedrock;
                x64Checkbox.IsChecked = Path_Bool.IsEnableX64mc;
                webServerCheckbox.IsChecked = Path_Bool.IsStartWebSocket;
                debugCheckbox.IsChecked = Path_Bool.IsDebug;
                blacklistCheckbox.IsChecked = Path_Bool.EnableRoomBlacklist;
                roomInput.Text = Path_Bool.MaxRoomCount.ToString();
                domainInput.Text = Path_Bool.NeteaseUpdateDomainhttp;
                AlwaysSaveWorldTip.IsChecked = Path_Bool.AlwaysSaveWorld;
                CustomIpCheckBox.IsChecked = Path_Bool.IsCustomIP;
            };
            saveButton.Click += (s, e) => {
                if(int.TryParse(roomInput.Text, out int roomCount))
                {
                    int httpPort = Path_Bool.HttpPort;
                    
                    bool isPortValid = true;
                    string errorMessage = "";
                    
                    // 只有当Web服务器启用时才验证和更新端口
                    if (webServerCheckbox.IsChecked ?? false)
                    {
                        if (!int.TryParse(httpPortInput.Text, out httpPort) || httpPort <= 0 || httpPort > 65535)
                        {
                            isPortValid = false;
                            errorMessage = "HTTP端口必须是1-65535之间的有效数字";
                        }
                    }
                    
                    if (isPortValid)
                    {
                        Path_Bool.IsBypassGameUpdate_Bedrock = bypassUpdateCheckbox.IsChecked ?? false;
                        Path_Bool.IsEnableX64mc = x64Checkbox.IsChecked ?? false;
                        Path_Bool.IsStartWebSocket = webServerCheckbox.IsChecked ?? false;
                        Path_Bool.IsDebug = debugCheckbox.IsChecked ?? false;
                        Path_Bool.EnableRoomBlacklist = blacklistCheckbox.IsChecked ?? false;
                        Path_Bool.MaxRoomCount = roomCount;
                        Path_Bool.NeteaseUpdateDomainhttp = domainInput.Text.Trim();
                        Path_Bool.AlwaysSaveWorld = AlwaysSaveWorldTip.IsChecked ?? false;
                        Path_Bool.IsCustomIP = CustomIpCheckBox.IsChecked ?? false;
                        
                        // 更新端口值
                        if (Path_Bool.IsStartWebSocket)
                        {
                            Path_Bool.HttpPort = httpPort;
                            // 更新默认地址
                            Path_Bool.Default_HttpAddress = $"http://127.0.0.1:{Path_Bool.HttpPort}/";
                            Path_Bool.Default_WebSocketAddress = $"ws://127.0.0.1:{Path_Bool.HttpPort}/websocket";
                        }
                        
                        SaveConfig();
                        
                        if(Path_Bool.IsStartWebSocket)
                        {
                            //WebSocketHelper.StartWebSocketServer();
                            var server = new SimpleHttpServer();
                            Task.Run(() => server.Start(Path_Bool.Default_HttpAddress));
                            Console.WriteLine("[INFO]Web服务器已启动!");
                            Console.WriteLine($"[INFO]HTTP服务器地址: {Path_Bool.Default_HttpAddress}");
                            Console.WriteLine($"[INFO]WebSocket服务器地址: {Path_Bool.Default_WebSocketAddress}");
                        }
                        configWindow.Close();
                    }
                    else
                    {
                        MessageBox.Show(errorMessage);
                    }
                }
                else
                {
                    MessageBox.Show("请输入有效的房间数量!");
                }
            };

            cancelButton.Click += (s, e) => configWindow.Close();

            // 显示配置窗口
            configWindow.ShowDialog();
            
            string Get_Mac_Addr = Get_MacAddr();
            string Get_Random_Mac_Addr = GenerateRandomMacAddress();
            string Get_Mac_Addr_Original = Get_Mac_Addr;
            string Get_Random_Mac_Addr_Original = ConvertToOriginalFormat(Get_Random_Mac_Addr);
            if (Get_Mac_Addr.Length == 12)
            {
                Get_Mac_Addr = string.Join(":", Enumerable.Range(0, 6).Select(i => Get_Mac_Addr.Substring(i * 2, 2)));
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[RandomMac]当前Mac地址:{Get_Mac_Addr},原始输出为:{Get_Mac_Addr_Original}\n[RandomMac]成功替换原来的Mac地址为随机Mac地址:{Get_Random_Mac_Addr},替换输出为:{Get_Random_Mac_Addr_Original}");
            Console.ForegroundColor = ConsoleColor.White;
            if (string.IsNullOrEmpty(Path_Bool.NeteaseUpdateDomainhttp) || 
                Path_Bool.NeteaseUpdateDomainhttp == "https://x19.update.netease.com")
            {
                Console.WriteLine("[INFO]NeteaseUpdateDomain为默认值,将使用默认的NeteaseUpdateDomain");
            }
            else
            {
                Console.WriteLine($"[INFO]NeteaseUpdateDomain已修改为:{Path_Bool.NeteaseUpdateDomainhttp}");
                azd<axg>.Instance.e();
            }
            Path_Bool.Mac_Addr = Get_Mac_Addr;
            Path_Bool.Random_Mac_Addr = Get_Random_Mac_Addr_Original;
            azd<axg>.Instance.App.EnableCppGameDebug = true;
            azd<axg>.Instance.App.NetGameFilter = true;
            azd<axg>.Instance.App.EnableNetGameFilterSetting = true;
            azd<axg>.Instance.App.CppGameDebugPath = ta.s + "\\CppGameDebug\\";
            string JavaGamePath = Path.Combine(ta.n, "Game",".minecraft");
            Directory.CreateDirectory(JavaGamePath);
            ta.z = JavaGamePath;
            azd<axg>.Instance.App.JavaGamePath = JavaGamePath;
            // 黑名单无效列表处理
            if (Path_Bool.EnableRoomBlacklist)
            {
                if (!Directory.Exists($"{Path_Bool.wpflauncherRoot}/RoomConfig"))
                {
                    // Create Directory RoomConfig
                    Directory.CreateDirectory($"{Path_Bool.wpflauncherRoot}/RoomConfig");
                    Console.WriteLine("[Warn] 未创建RoomConfig文件夹,已自动创建");
                }
                if (!File.Exists($"{Path_Bool.wpflauncherRoot}/RoomConfig/BlackList.json"))
                {
                    // Init BlackList
                    File.WriteAllText($"{Path_Bool.wpflauncherRoot}/RoomConfig/BlackList.json", "[]");
                    Console.WriteLine("[Warn] 未创建RoomConfig/BlackList.json文件,已自动创建");
                }

                if (!File.Exists($"{Path_Bool.wpflauncherRoot}/RoomConfig/RegexBlackList.json"))
                {
                    // Init RegexBlackList
                    File.WriteAllText($"{Path_Bool.wpflauncherRoot}/RoomConfig/RegexBlackList.json", "[]");
                    Console.WriteLine("[Warn] 未创建RoomConfig/RegexBlackList.json文件,已自动创建");
                }
                // 读取文件
                
            }
            return false;
        }
        
        public static string GenerateRandomMacAddress()
        {
            Random random = new Random();
            byte[] macAddr = new byte[6];
            random.NextBytes(macAddr);

            // 保证MAC地址的第一个字节的最低位为0（即设备地址类型为"单播"）
            macAddr[0] = (byte)(macAddr[0] & (byte)254);

            // 将字节数组格式化为MAC地址
            return string.Join(":", macAddr.Select(b => b.ToString("X2")));
        }
        public static string ConvertToOriginalFormat(string macAddress)
        {
            // 去掉所有冒号
            return macAddress.Replace(":", string.Empty);
        }
        // Token: 0x06006251 RID: 25169 RVA: 0x0014118C File Offset: 0x0013F38C
        public static string Get_MacAddr()
        {
            string text = string.Empty;
            try
            {
                NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface networkInterface in allNetworkInterfaces)
                {
                    if (text == string.Empty)
                    {
                        text = networkInterface.GetPhysicalAddress().ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                td.Default.l(ex, "GetMACAddress");
            }
            return text;
        }
    }
}