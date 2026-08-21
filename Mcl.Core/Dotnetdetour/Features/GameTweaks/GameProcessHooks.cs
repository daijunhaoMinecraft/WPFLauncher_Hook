using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using Mcl.Core.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Manager;
using WPFLauncher.Util;
using Application = System.Windows.Application;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

/// <summary>
/// 核心游戏进程拦截与启动 Hook
/// </summary>
public class GameProcessStartupHook : IMethodHook
{
    #region 游戏前置内存优化核心逻辑

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PrivilegeToken
    {
        public int PrivilegeCount;
        public long Luid;
        public int Attributes;
    }

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern uint NtSetSystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength);

    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref long lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref PrivilegeToken NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    /// <summary>
    /// 执行硬核内存清理
    /// </summary>
    private static void OptimizeMemoryBeforeLaunch()
    {
        try
        {
            WpfConfig.DefaultLogger.Info("[内存优化] 正在尝试释放系统物理内存...");

            // 1. 获取当前进程令牌，并赋予操作内存列表的特权 (SeProfileSingleProcessPrivilege)
            using (var identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges))
            {
                PrivilegeToken token = new PrivilegeToken { PrivilegeCount = 1, Attributes = 2 /* SE_PRIVILEGE_ENABLED */ };
                long luid = 0;
                
                if (LookupPrivilegeValue(null, "SeProfileSingleProcessPrivilege", ref luid))
                {
                    token.Luid = luid;
                    AdjustTokenPrivileges(identity.Token, false, ref token, Marshal.SizeOf(token), IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    WpfConfig.DefaultLogger.Error($"[内存优化] 无法查找特权, 错误码: {Marshal.GetLastWin32Error()}");
                    return;
                }
            }

            // 2. 依次发送指令清理系统内存
            // 指令 2: 清空工作集 | 指令 3: 刷新已修改页面 | 指令 4: 清空备用列表(缓存)
            for (int command = 2; command <= 4; command++)
            {
                int cmd = command; // 必须使用局部变量固定指针
                GCHandle handle = GCHandle.Alloc(cmd, GCHandleType.Pinned);
                try
                {
                    // 80 代表 SystemMemoryListInformation
                    uint result = NtSetSystemInformation(80, handle.AddrOfPinnedObject(), Marshal.SizeOf(cmd));
                    if (result != 0)
                    {
                        WpfConfig.DefaultLogger.Info($"[内存优化] 指令 {command} 执行异常，NTSTATUS: {result}");
                    }
                }
                finally
                {
                    handle.Free();
                }
            }

            WpfConfig.DefaultLogger.Info("[内存优化] 内存释放完成！已为游戏腾出最大物理空间。");
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"[内存优化] 执行失败: {ex.Message}");
        }
    }

    #endregion
    
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
        
        bool isJava = fileName.Contains("java.exe") || fileName.Contains("javaw.exe");
        if (isJava)
        {
            if (WpfConfig.UseJavaExe)
            {
                fileName = fileName.Replace("javaw.exe", "java.exe");
            }
            // ================== 合并自定义JVM参数 ==================
            if (!string.IsNullOrWhiteSpace(WpfConfig.CustomJVMArguments))
            {
                args = MergeMinecraftArgs(args, WpfConfig.CustomJVMArguments);
                WpfConfig.DefaultLogger.Info($"[进程] 已注入自定义JVM参数");
            }
        }

        // ================== 游戏启动前：触发内存优化 ==================
        if ((isJava || isBedrock) && WpfConfig.MemoryOptimize)
        {
            OptimizeMemoryBeforeLaunch(); 
        }

        // ================== 判定是否属于需要创建 WPF 日志的白名单进程 ==================
        string exeName = Path.GetFileName(fileName)?.ToLower() ?? "";
        bool useWpfLog = (exeName == "java.exe" || 
                         exeName == "javaw.exe" || 
                         exeName == "minecraft.windows.exe") && WpfConfig.ShowLogInWpf;

        ProcessLogWindow logWindow = null;
        if (useWpfLog)
        {
            // 必须在主 Dispatcher 上创建窗口
            Application.Current.Dispatcher.Invoke(() =>
            {
                logWindow = new ProcessLogWindow(Path.GetFileName(fileName));
                logWindow.Show();
            });
        }

        // ================== 核心组装 ==================
        var processObj = new aqq
        {
            StartInfo =
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,        
                // 仅对白名单进程强制使用 WPF 界面隐藏黑框并重定向
                // 对其他普通进程保留原有逻辑
                RedirectStandardOutput = useWpfLog ? true : !isJava, 
                RedirectStandardError  = useWpfLog ? true : !isJava,  
                CreateNoWindow         = useWpfLog ? true : !isJava 
            },
            Type = startType
        };
        
        if (!string.IsNullOrEmpty(workDirectory)) 
        {
            processObj.StartInfo.WorkingDirectory = workDirectory;
        }

        // 绝对不能删，防止启动器通信时空引用崩溃
        aqr.Instance.c(processObj);

        // ================== 绑定标准输出与错误 ==================
        processObj.OutputDataReceived += (sender, outputArgs) =>
        {
            if (!string.IsNullOrEmpty(outputArgs.Data))
            {
                string cleanMsg = AnsiColorRegex.Replace(outputArgs.Data, string.Empty);
                if (WpfConfig.ShowLogInConsole)
                {
                    Console.WriteLine(cleanMsg);
                }
                if (useWpfLog) logWindow?.AppendLog(cleanMsg, isError: false);
            }
        };
        processObj.ErrorDataReceived += (sender, errorArgs) =>
        {
            if (!string.IsNullOrEmpty(errorArgs.Data))
            {
                string cleanMsg = AnsiColorRegex.Replace(errorArgs.Data, string.Empty);
                if (WpfConfig.ShowLogInConsole)
                {
                    Console.WriteLine($"[StdErr] {cleanMsg}");
                }
                if (useWpfLog) logWindow?.AppendLog(cleanMsg, isError: true);
            }
        };

        // ================== 绑定进程退出与主动结束逻辑 ==================
        if (useWpfLog)
        {
            processObj.EnableRaisingEvents = true; // 允许触发 Exited 事件
            
            // 绑定退出事件：向 WPF 窗口输出退出代码
            processObj.Exited += (sender, e) =>
            {
                int exitCode = -1;
                try { exitCode = processObj.ExitCode; } catch { } // 防止有些进程无法读取退出码
                
                logWindow?.OnProcessExited(exitCode);
                Console.WriteLine($"\n[进程] 进程 {Path.GetFileName(fileName)} 已退出, 错误代码: {exitCode}");
            };

            // 设置关闭进程的方法委托到界面中
            Application.Current.Dispatcher.Invoke(() =>
            {
                logWindow?.SetKillAction(() =>
                {
                    try
                    {
                        if (!processObj.HasExited)
                        {
                            processObj.Kill();
                            WpfConfig.DefaultLogger.Info($"[进程] 已由用户手动强制结束进程: {fileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        WpfConfig.DefaultLogger.Error($"[进程] 强制结束进程失败: {ex.Message}");
                    }
                });
            });
        }

        WpfConfig.DefaultLogger.Info($"[进程] 启动进程: {fileName}");

        return processObj;
    }

    [OriginalMethod]
    public static aqq StartProcessOriginal(string executablePath, string arguments, EventHandler exitHandler, aqo startType, string workDir = null, bool redirectOutput = false, Action<string> outputCallback = null)
    {
        return new aqq();
    }

    [HookMethod("WPFLauncher.Util.wb", "a", "StartProcessOriginal")]
    public static aqq StartProcess(string executablePath, string arguments, EventHandler exitHandler, aqo startType, string workDir = null, bool redirectOutput = false, Action<string> outputCallback = null)
    {
        Action<string> outputCallbackHook = s =>
        {
            // Console.WriteLine(s);
            if (outputCallback != null)
            {
                outputCallback(s);
            }
        };
        Console.WriteLine("[StartGame] 启动信息创建中...");
        
        // 调用原方法实际启动进程
        var processResult = StartProcessOriginal(executablePath, arguments, exitHandler, startType, workDir, true, outputCallbackHook);

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
    
    // ================== 辅助方法：智能合并JVM参数 ==================
    private static string MergeMinecraftArgs(string originalArgs, string customArgs)
    {
        if (string.IsNullOrWhiteSpace(customArgs)) return originalArgs;

        // 解析自定义参数（支持带引号的包含空格的路径）
        var customTokens = SplitArgs(customArgs);
        string newArgs = originalArgs;

        for (int i = 0; i < customTokens.Count; i++)
        {
            string token = customTokens[i];

            if (token.StartsWith("-"))
            {
                // 1. 处理内存参数等特殊单键 (如 -Xmx4G, -Xms2G)
                if (token.StartsWith("-Xmx") || token.StartsWith("-Xms"))
                {
                    string prefix = token.Substring(0, 4); // 提取 "-Xmx"
                    if (Regex.IsMatch(newArgs, $@"\{prefix}\S+"))
                        newArgs = Regex.Replace(newArgs, $@"\{prefix}\S+", token);
                    else
                        newArgs = token + " " + newArgs;
                    continue;
                }

                // 2. 检查是否为 "-key value" 格式
                string nextToken = (i + 1 < customTokens.Count) ? customTokens[i + 1] : null;
                bool hasValue = nextToken != null && !nextToken.StartsWith("-");

                if (hasValue)
                {
                    string escapedKey = Regex.Escape(token);
                    // 正则匹配原参数中的 "-key 旧值"
                    string pattern = $@"{escapedKey}\s+(?:""[^""]*""|\S+)";
                    string replacement = $"{token} {nextToken}";
                    
                    if (Regex.IsMatch(newArgs, pattern))
                        newArgs = Regex.Replace(newArgs, pattern, replacement); // 存在则替换
                    else
                        newArgs = replacement + " " + newArgs; // 不存在则追加到最前面
                        
                    i++; // 已经处理了value，跳过下一个token
                }
                // 3. 独立的开关参数 (如 -XX:+UseG1GC)
                else
                {
                    if (!newArgs.Contains(token))
                        newArgs = token + " " + newArgs;
                }
            }
        }
        return newArgs;
    }

    // ================== 辅助方法：处理带引号的参数分割 ==================
    private static List<string> SplitArgs(string args)
    {
        var result = new List<string>();
        var currentToken = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < args.Length; i++)
        {
            char c = args[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
                currentToken.Append(c);
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (currentToken.Length > 0)
                {
                    result.Add(currentToken.ToString());
                    currentToken.Clear();
                }
            }
            else
            {
                currentToken.Append(c);
            }
        }
        if (currentToken.Length > 0) result.Add(currentToken.ToString());
        return result;
    }
    #endregion
}