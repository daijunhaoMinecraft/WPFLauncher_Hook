using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DotNetTranstor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration.CppConfigure;
using WPFLauncher.Util;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using System.Windows.Media.Effects;
using DotNetTranstor.Hookevent;
using Login.NetEase;
using NLog.Targets;
using WPFLauncher.Common;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Model.Game;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace MicrosoftTranslator.DotNetTranstor.Hookevent
{
    public class BedrockPathWindow_Select : Window
    {
        private TextBox _pathTextBox;
        private ListBox _versionListBox;
        private string _selectedPath;
        private string _configPath = Path.Combine(Directory.GetCurrentDirectory(), "BedrockPath.txt");
        private Action<string> _onVersionSelected;

        public BedrockPathWindow_Select(Action<string> onVersionSelected)
        {
            _onVersionSelected = onVersionSelected;
            InitializeWindow();
            LoadSavedPath();
            ScanVersions();
        }

        private void InitializeWindow()
        {
            Title = "选择基岩版路径";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Colors.WhiteSmoke);
            
            var mainPanel = new StackPanel { Margin = new Thickness(20) };
            
            // 标题
            var titleBlock = new TextBlock
            {
                Text = "基岩版路径选择",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            mainPanel.Children.Add(titleBlock);

            // 路径选择面板
            var pathPanel = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            pathPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            pathPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pathLabel = new TextBlock
            {
                Text = "当前基岩版路径:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            Grid.SetColumn(pathLabel, 0);

            _pathTextBox = new TextBox
            {
                IsReadOnly = true,
                Height = 30,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };
            Grid.SetColumn(_pathTextBox, 1);

            var selectButton = new System.Windows.Controls.Button
            {
                Content = "修改路径",
                Height = 30,
                Padding = new Thickness(15, 0, 15, 0),
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Effect = new DropShadowEffect
                {
                    ShadowDepth = 2,
                    BlurRadius = 4,
                    Opacity = 0.3
                }
            };
            selectButton.Click += SelectButton_Click;
            Grid.SetColumn(selectButton, 2);

            pathPanel.Children.Add(pathLabel);
            pathPanel.Children.Add(_pathTextBox);
            pathPanel.Children.Add(selectButton);
            mainPanel.Children.Add(pathPanel);

            // 版本列表
            var listLabel = new TextBlock
            {
                Text = "可用版本:",
                Margin = new Thickness(0, 0, 0, 10),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            mainPanel.Children.Add(listLabel);

            _versionListBox = new ListBox
            {
                Height = 300,
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };
            _versionListBox.SelectionChanged += VersionListBox_SelectionChanged;
            mainPanel.Children.Add(_versionListBox);

            Content = mainPanel;
        }

        private void LoadSavedPath()
        {
            string path = ta.s;
            if (Directory.Exists(path))
            {
                _pathTextBox.Text = path;
                _selectedPath = path;
                ScanVersions();
                return;
            }

            // 如果sm.w不存在或无效，则尝试从BedrockConfig.txt读取
            if (File.Exists(_configPath))
            {
                string savedPath = File.ReadAllText(_configPath);
                if (Directory.Exists(savedPath))
                {
                    _pathTextBox.Text = savedPath;
                    _selectedPath = savedPath;
                    ScanVersions();
                }
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择基岩版安装路径";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _selectedPath = dialog.SelectedPath;
                    _pathTextBox.Text = _selectedPath;
                    
                    try
                    {
                        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "BedrockPath.txt"), _selectedPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存路径时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                    ScanVersions();
                }
            }
        }

        private void ScanVersions()
        {
            _versionListBox.Items.Clear();
            if (string.IsNullOrEmpty(_selectedPath) || !Directory.Exists(_selectedPath))
            {
                return;
            }

            try
            {
                foreach (var dir in Directory.GetDirectories(_selectedPath))
                {
                    if (File.Exists(Path.Combine(dir, "Minecraft.Windows.exe")))
                    {
                        var versionCard = CreateVersionCard(dir);
                        _versionListBox.Items.Add(versionCard);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"扫描版本时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateVersionCard(string path)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    ShadowDepth = 1,
                    BlurRadius = 4,
                    Opacity = 0.2
                }
            };

            var panel = new StackPanel();
            
            var versionName = new TextBlock
            {
                Text = Path.GetFileName(path),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            panel.Children.Add(versionName);

            var pathText = new TextBlock
            {
                Text = path,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(119, 119, 119))
            };
            panel.Children.Add(pathText);

            card.Child = panel;
            card.Tag = path;

            return card;
        }

        private void VersionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_versionListBox.SelectedItem is Border selectedCard)
            {
                string selectedPath = selectedCard.Tag as string;
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _onVersionSelected?.Invoke(selectedPath);
                    DialogResult = true;
                    Close();
                }
            }
        }
    }

    public class ProcessCreateWindow : IMethodHook
    {
        private static ClientWebSocket _webSocket;
        private static bool _isReceiving;
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static CppGameConfig CppConfigContent = null;
        private static string CppConfigPath = null;
        
        //创建进程窗口
        [OriginalMethod]
        public aqp ProcssStartOriginal(string FileName, string Args, aqn StartType, string WorkDirectory = null)
        {
            return new aqp();
        }

        [HookMethod("WPFLauncher.Manager.aqq", "t", "ProcssStartOriginal")]
        public aqp ProcssStart(string FileName, string Args, aqn StartType, string WorkDirectory = null)
        {
            // 检查是否是基岩版启动
            if (FileName.Contains("Minecraft.Windows.exe"))
            {
                if (Path_Bool.IsCustomIP && Path_Bool.IsSelectedIP == false)
                {
                    Console.WriteLine("[Thread] 线程滞后: 等待用户选择好IP地址");
                    while (!Path_Bool.IsSelectedIP)
                    {
                        
                    }
                }
                // 显示基岩版路径选择窗口
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = new BedrockPathWindow_Select(selectedPath =>
                    {
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            FileName = Path.Combine(selectedPath, "Minecraft.Windows.exe");
                            WorkDirectory = selectedPath;
                            // if (Path_Bool.IsDecryptMod)
                            // {
                            //     DecryptModStep(Args);
                            // }
                        }
                    });
                    window.ShowDialog();
                });
                Console.ForegroundColor = ConsoleColor.Cyan;
                DebugPrint.LogDebug_NoColorSelect($"[SelectBedrock]选择的基岩版:{FileName}");
                Console.ForegroundColor = ConsoleColor.White;
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { Type = "StartBedrockGame", SelectBedrockExePath = FileName }));
            }

            aqp aqp = new aqp
            {
                StartInfo = 
                {
                    FileName = FileName,
                    Arguments = Args,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true,
                    RedirectStandardError = false
                },
                Type = StartType
            };
            if (!string.IsNullOrEmpty(WorkDirectory))
            {
                aqp.StartInfo.WorkingDirectory = WorkDirectory;
            }

            // 添加进程退出事件处理，防止突然关闭CMD窗口
            // aqp.EnableRaisingEvents = true;
            // aqp.Exited += (sender, e) =>
            // {
            //     Console.ForegroundColor = ConsoleColor.Yellow;
            //     DebugPrint.LogDebug_NoColorSelect($"\n[进程] 进程 {FileName} 已退出");
            // };

            // 使用多线程启动游戏
            Task.Run(() =>
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    DebugPrint.LogDebug_NoColorSelect($"[进程] 正在后台线程启动游戏: {FileName}");
                    Console.ForegroundColor = ConsoleColor.White;
                    
                    // 在后台线程中启动游戏
                    aqq.Instance.c(aqp);
                }
                catch (Exception ex)
                {
                    // 异常处理
                    Console.ForegroundColor = ConsoleColor.Red;
                    DebugPrint.LogDebug_NoColorSelect($"[进程] 启动游戏时发生错误: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.White;
                    
                    // 通知UI线程显示错误信息
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"启动游戏时发生错误: {ex.Message}", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });

            return aqp;
        }

        // private void StartReceiving()
        // {
        //     if (_isReceiving || _webSocket == null) return;
        //     DebugPrint.LogDebug_NoColorSelect("[StartGame] 开始接收消息");
        //     _isReceiving = true;
        //     var buffer = new byte[4096];
        //     
        //     Task.Run(async () =>
        //     {
        //         try
        //         {
        //             while (_webSocket.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
        //             {
        //                 DebugPrint.LogDebug_NoColorSelect("[StartGame] 开始接收消息");
        //                 var result = new ArraySegment<byte>(buffer);
        //                 WebSocketReceiveResult receiveResult = await _webSocket.ReceiveAsync(result, _cancellationTokenSource.Token);
        //                 DebugPrint.LogDebug_NoColorSelect("[StartGame] 已接收消息");
        //                 if (receiveResult.MessageType == WebSocketMessageType.Close)
        //                 {
        //                     Console.WriteLine("[WebSocket] 连接已关闭");
        //                     break;
        //                 }
        //
        //                 var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
        //                 Console.WriteLine("[StartGame] 已处理消息");
        //                 HandleWebSocketMessage(message);
        //             }
        //         }
        //         catch (Exception ex)
        //         {
        //             Console.WriteLine($"[WebSocket] 接收消息时发生错误: {ex.Message}");
        //         }
        //         finally
        //         {
        //             _isReceiving = false;
        //         }
        //     });
        // }
        //
        // private void HandleWebSocketMessage(string message)
        // {
        //     JObject message_json = JObject.Parse(message);
        //     string MessageType = message_json["type"].ToString();
        //     Console.WriteLine("[StartGame] 已处理消息,内容为:" + message);
        //     switch (MessageType)
        //     {
        //         case "ChangeCpp":
        //             Console.WriteLine("[WebSocket] 已接收并处理配置更新");
        //             CppConfigContent.world_info.resource_packs = CppConfigContent.world_info.resource_packs.Concat(message_json["resource_packs"].ToObject<List<string>>()).Distinct().ToList();
        //             Console.WriteLine("[WebSocket] 已更新资源包");
        //             CppConfigContent.world_info.behavior_packs = CppConfigContent.world_info.behavior_packs.Concat(message_json["behavior_packs"].ToObject<List<string>>()).Distinct().ToList(); 
        //             Console.WriteLine("[WebSocket] 已更新行为包");
        //             CppConfigContent.web_server_url = message_json["web_server_url"].ToString();
        //             Console.WriteLine("[WebSocket] 已接收并处理配置更新");
        //             //保存CppConfig
        //             File.WriteAllText(CppConfigPath, JsonConvert.SerializeObject(CppConfigContent));
        //             Console.WriteLine("[WebSocket] 已保存CppConfig");
        //             // 处理完消息后关闭连接
        //             CloseWebSocket();
        //             break;
        //     }
        // }
        //
        // public static void SendWebSocketMessage(string message)
        // {
        //     if (_webSocket?.State != WebSocketState.Open)
        //     {
        //         Console.WriteLine("[WebSocket] 无法发送消息：连接未建立或已关闭");
        //         return;
        //     }
        //
        //     try
        //     {
        //         var messageBytes = Encoding.UTF8.GetBytes(message);
        //         var messageSegment = new ArraySegment<byte>(messageBytes);
        //         _webSocket.SendAsync(messageSegment, WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        //         Console.WriteLine($"[WebSocket] 发送消息: {message}");
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"[WebSocket] 发送消息时发生错误: {ex.Message}");
        //     }
        // }
        //
        // public static void CloseWebSocket()
        // {
        //     if (_webSocket != null)
        //     {
        //         try
        //         {
        //             _cancellationTokenSource.Cancel();
        //             if (_webSocket.State == WebSocketState.Open)
        //             {
        //                 _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None).Wait();
        //             }
        //             _webSocket.Dispose();
        //             _webSocket = null;
        //             _isReceiving = false;
        //             Console.WriteLine("[WebSocket] 连接已关闭");
        //         }
        //         catch (Exception ex)
        //         {
        //             Console.WriteLine($"[WebSocket] 关闭连接时发生错误: {ex.Message}");
        //         }
        //     }
        // }
        
        [OriginalMethod]
        public static aqp a_original(string gwa, string gwb, EventHandler gwc, aqk gwd, string gwe = null, bool gwf = false, Action<string> gwg = null)
        {
            return new aqp();
        }
        
        [HookMethod("WPFLauncher.Util.vx", "a", "a_original")]
        public static aqp a(string gwa, string gwb, EventHandler gwc, aqk gwd, string gwe = null, bool gwf = false, Action<string> gwg = null)
        {
            Console.WriteLine("[StartGame]启动信息创建中...");
            aqp result = a_original(gwa, gwb, gwc, gwd, gwe, true, gwg);
            
            // 添加进程退出事件处理，防止CMD窗口关闭
            if (result != null)
            {
                result.EnableRaisingEvents = true;
                result.Exited += (sender, e) =>
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n[进程] 进程 {gwa} 已退出");
                };
                
                // 使用多线程处理游戏启动后的操作
                Task.Run(() =>
                {
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[进程] 正在后台线程处理启动进程: {gwa}");
                        Console.ForegroundColor = ConsoleColor.White;
                        
                        // 这里可以添加额外的启动后处理逻辑
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[进程] 处理启动进程时发生错误: {ex.Message}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                });
            }
            
            return result;
        }
    }
}