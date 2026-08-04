using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using Mcl.Core.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Manager;
using WPFLauncher.Util;
using Application = System.Windows.Application;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;


namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

/// <summary>
/// 基岩版路径选择窗口 UI 类
/// </summary>
public class BedrockPathSelectWindow : Window
{
    private readonly Action<string> _onVersionSelected;
    private TextBox _pathTextBox;
    private ListBox _versionListBox;
    private string _selectedPath;

    public BedrockPathSelectWindow(Action<string> onVersionSelected)
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
        Topmost = WpfConfig.IsWindowTopMost;

        var mainPanel = new StackPanel { Margin = new Thickness(20) };
        var titleBar = new Grid { Margin = new Thickness(0, 0, 0, 20) };
        
        titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // 标题
        var titleBlock = new TextBlock
        {
            Text = "基岩版路径选择",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(40, 0, 0, 0),
            Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
        };
        Grid.SetColumn(titleBlock, 0);

        // 置顶复选框
        var topMostCheck = new System.Windows.Controls.CheckBox
        {
            Content = "置顶",
            VerticalAlignment = VerticalAlignment.Center,
            IsChecked = WpfConfig.IsWindowTopMost
        };
        topMostCheck.Checked += (s, e) => Topmost = true;
        topMostCheck.Unchecked += (s, e) => Topmost = false;
        Grid.SetColumn(topMostCheck, 1);

        titleBar.Children.Add(titleBlock);
        titleBar.Children.Add(topMostCheck);
        mainPanel.Children.Add(titleBar);

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
            Effect = new DropShadowEffect { ShadowDepth = 2, BlurRadius = 4, Opacity = 0.3 }
        };
        selectButton.Click += SelectButton_Click;
        Grid.SetColumn(selectButton, 2);

        pathPanel.Children.Add(pathLabel);
        pathPanel.Children.Add(_pathTextBox);
        pathPanel.Children.Add(selectButton);
        mainPanel.Children.Add(pathPanel);

        // 可用版本列表
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
        if (Directory.Exists(tb.s))
        {
            _selectedPath = tb.s;
        }
        else if (Directory.Exists(WpfConfig.BedrockPath))
        {
            _selectedPath = WpfConfig.BedrockPath;
        }

        if (!string.IsNullOrEmpty(_selectedPath))
        {
            _pathTextBox.Text = _selectedPath;
            ScanVersions();
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "请选择基岩版安装路径" };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _selectedPath = dialog.SelectedPath;
            _pathTextBox.Text = _selectedPath;

            try
            {
                WpfConfig.BedrockPath = _selectedPath;
                ConfigManager.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存路径时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            ScanVersions();
        }
    }

    private void ScanVersions()
    {
        _versionListBox.Items.Clear();
        if (string.IsNullOrEmpty(_selectedPath) || !Directory.Exists(_selectedPath)) return;

        try
        {
            foreach (var dir in Directory.GetDirectories(_selectedPath))
            {
                if (File.Exists(Path.Combine(dir, "Minecraft.Windows.exe")))
                {
                    _versionListBox.Items.Add(CreateVersionCard(dir));
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
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(path),
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 5),
            Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
        });
        panel.Children.Add(new TextBlock
        {
            Text = path,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(119, 119, 119))
        });

        return new Border
        {
            Background = new SolidColorBrush(Colors.White),
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 0, 5),
            Padding = new Thickness(10),
            Effect = new DropShadowEffect { ShadowDepth = 1, BlurRadius = 4, Opacity = 0.2 },
            Child = panel,
            Tag = path
        };
    }

    private void VersionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_versionListBox.SelectedItem is Border { Tag: string selectedPath } && !string.IsNullOrEmpty(selectedPath))
        {
            _onVersionSelected?.Invoke(selectedPath);
            DialogResult = true;
            Close();
        }
    }
}

/// <summary>
/// 核心游戏进程拦截与启动 Hook
/// </summary>
public class GameProcessStartupHook : IMethodHook
{
    // 用于剔除控制台输出中的 ANSI 颜色转义字符 (例如 \e[38;2;255;135;0m)
    private static readonly Regex AnsiColorRegex = new(@"\x1B\[[0-9;]*[a-zA-Z]", RegexOptions.Compiled);

    [OriginalMethod]
    public aqq ProcessStartOriginal(string fileName, string args, aqo startType, string workDirectory = null)
    {
        return new aqq();
    }
    
    [HookMethod("WPFLauncher.Manager.aqr", "t", "ProcessStartOriginal")]
    public aqq ProcessStart(string fileName, string args, aqo startType, string workDirectory = null)
    {
        bool isBedrock = fileName.Contains("Minecraft.Windows.exe");

        if (isBedrock)
        {
            HandleBedrockPreStart(ref fileName, ref workDirectory);
        }
        else
        {
            // Java 版：将无头 javaw 替换为带控制台的 java，用于弹窗防崩溃并显示日志
            fileName = fileName.Replace("javaw.exe", "java.exe");
        }

        // ================== 核心组装：根据版本分离配置 ==================
        var processObj = new aqq
        {
            StartInfo =
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,        
                // 基岩版开启重定向以截获日志；Java版关闭以防止网易DLL崩溃
                RedirectStandardOutput = isBedrock, 
                RedirectStandardError = isBedrock,  
                // 基岩版不弹窗(后台)；Java版弹窗(显示独立黑底日志窗口)
                CreateNoWindow = isBedrock
            },
            Type = startType
        };
        
        if (!string.IsNullOrEmpty(workDirectory)) 
        {
            processObj.StartInfo.WorkingDirectory = workDirectory;
        }

        // ================== 进程注册 (必须同步执行) ==================
        // 绝对不能删，防止启动器通信时空引用崩溃
        aqr.Instance.c(processObj);

        // ================== 基岩版专有后置处理 (绑定后台日志) ==================
        if (isBedrock)
        {
            AttachBedrockLogger(processObj);
        }

        WpfConfig.DefaultLogger.Info($"[进程] 拦截并重组启动参数完毕: {fileName}");

        return processObj;
    }

    [OriginalMethod]
    public static aqq StartProcessOriginal(string executablePath, string arguments, EventHandler exitHandler, aqk startType, string workDir = null, bool redirectOutput = false, Action<string> outputCallback = null)
    {
        return new aqq();
    }

    [HookMethod("WPFLauncher.Util.vy", "a", "StartProcessOriginal")]
    public static aqq StartProcess(string executablePath, string arguments, EventHandler exitHandler, aqk startType, string workDir = null, bool redirectOutput = false, Action<string> outputCallback = null)
    {
        Console.WriteLine("[StartGame] 启动信息创建中...");
        
        // 调用原方法实际启动进程
        var processResult = StartProcessOriginal(executablePath, arguments, exitHandler, startType, workDir, true, outputCallback);

        if (processResult != null)
        {
            processResult.EnableRaisingEvents = true;
            processResult.Exited += (sender, e) => { Console.WriteLine($"\n[进程] 进程 {executablePath} 已退出"); };
        }

        return processResult;
    }
    
    [OriginalMethod]
    internal void OutputProcess(object sender, DataReceivedEventArgs receivedData)
    {
    }

    #region 私有辅助方法

    /// <summary>
    /// 处理基岩版启动前的前置逻辑（IP等待、选件、自定义服务器）
    /// </summary>
    private void HandleBedrockPreStart(ref string fileName, ref string workDirectory)
    {
        if (WpfConfig.IsCustomIP && !WpfConfig.IsSelectedIP)
        {
            Console.WriteLine("[Thread] 线程滞后: 等待用户选择好IP地址...");
            while (!WpfConfig.IsSelectedIP) Thread.Sleep(100);
        }

        if (WpfConfig.EnableCustomBedrockSelect)
        {
            // 1. 暂存到局部变量，避开 ref 限制
            string tempFileName = fileName;
            string tempWorkDir = workDirectory;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new BedrockPathSelectWindow(selectedPath =>
                {
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        // 2. 修改局部变量
                        tempFileName = Path.Combine(selectedPath, "Minecraft.Windows.exe");
                        tempWorkDir = selectedPath;
                    }
                });
                window.ShowDialog();
            });

            // 3. 将可能被修改过的值重新赋回给 ref 形参
            fileName = tempFileName;
            workDirectory = tempWorkDir;
        }
            
        WpfConfig.DefaultLogger.Info($"[SelectBedrock] 选择的基岩版: {fileName}");
        WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { Type = "StartBedrockGame", SelectBedrockExePath = fileName }));
            
        if (WpfConfig.IsJoinCustomServer)
        {
            PatchCustomServerConfig();
        }
    }

    /// <summary>
    /// 注入并修改 CppGame 配置文件用于自定义服务器
    /// </summary>
    private void PatchCustomServerConfig()
    {
        try
        {
            string configPath = Path.Combine(tb.n, "temp", "temp.config");
            WpfConfig.DefaultLogger.Info($"[CustomServer] 正在修改 CppGamePath: {configPath}");
            
            string readEncryptConfig = File.ReadAllText(configPath);
            JObject jsonConfig = JObject.Parse(X19SignHelper.Decrypt(readEncryptConfig));
            
            jsonConfig["room_info"]["item_ids"][0] = "4668698705152194374";
            
            File.WriteAllText(configPath, JsonConvert.SerializeObject(jsonConfig, Formatting.None));
            WpfConfig.DefaultLogger.Info("[CustomServer] 配置文件修改并保存成功！");
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"[CustomServer] 修改失败: {ex}");
            throw;
        }
    }

    /// <summary>
    /// 为基岩版绑定后台日志截取器并清理 ANSI 转义符
    /// </summary>
    private void AttachBedrockLogger(aqq processObj)
    {
        // 绑定标准输出与错误，同时正则清洗数据
        processObj.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
                Console.WriteLine($"[Bedrock-StdOut] {AnsiColorRegex.Replace(args.Data, string.Empty)}");
        };
        processObj.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
                Console.WriteLine($"[Bedrock-StdErr] {AnsiColorRegex.Replace(args.Data, string.Empty)}");
        };

        // 后台轮询等待进程被上层启动，挂载流读取
        Task.Run(() =>
        {
            while (true)
            {
                try
                {
                    _ = processObj.Id; // 当未Start前调用Id会抛出异常
                    break;
                }
                catch (InvalidOperationException)
                {
                    Thread.Sleep(50);
                }
                catch (Exception)
                {
                    return; // 进程对象已失效
                }
            }

            try { processObj.BeginOutputReadLine(); } catch (InvalidOperationException) { }
            try { processObj.BeginErrorReadLine(); } catch (InvalidOperationException) { }
            
            WpfConfig.DefaultLogger.Info("[进程] 基岩版日志后台流监听器挂载成功。");
        });
    }

    #endregion
}