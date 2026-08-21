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
using System.Threading;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

/// <summary>
/// 核心游戏进程拦截与启动 Hook
/// </summary>
public class GameProcessStartupHook : IMethodHook
{
    // 用于剔除控制台输出中的 ANSI 颜色转义字符
    private static readonly Regex AnsiColorRegex = new(@"\x1B\[[0-9;]*[a-zA-Z]", RegexOptions.Compiled);

    // 【核心改进】使用 AsyncLocal 在调用链中传递上下文。
    // 只要是 wb.a 调用的，不管中间经过多少 await 异步操作，该值都为 true
    private static readonly AsyncLocal<bool> _isInvokedByWbA = new AsyncLocal<bool>();

    #region Hook: WPFLauncher.Util.wb.a (外层调用)

    [OriginalMethod]
    public static aqq StartProcessOriginal(string executablePath, string arguments, EventHandler exitHandler, aqo startType, string workDir = null, bool redirectOutput = false, Action<string> outputCallback = null)
    {
        return new aqq();
    }

    [HookMethod("WPFLauncher.Util.wb", "a", "StartProcessOriginal")]
    public static aqq StartProcess(string executablePath, string arguments, EventHandler exitHandler, aqo startType, string workDir = null, bool redirectOutput = false, Action<string> outputCallback = null)
    {
        // 标记：当前作用域下的后续调用链，都是由 wb.a 触发的
        _isInvokedByWbA.Value = true;

        try
        {
            Action<string> outputCallbackHook = s =>
            {
                if (outputCallback != null) outputCallback(s);
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
        finally
        {
            // 退出时清除标记，避免污染其他独立线程池的调用
            _isInvokedByWbA.Value = false; 
        }
    }

    #endregion

    #region Hook: WPFLauncher.Manager.aqr.t (内层核心进程启动)

    [OriginalMethod]
    public aqq ProcessStartOriginal(string fileName, string args, aqo startType, string workDirectory = null)
    {
        return new aqq();
    }
    
    [HookMethod("WPFLauncher.Manager.aqr", "t", "ProcessStartOriginal")]
    public aqq ProcessStart(string fileName, string args, aqo startType, string workDirectory = null)
    {
        // 1. 文件路径预处理
        fileName = fileName?.Replace("\"", "") ?? string.Empty;
        bool isBedrock = fileName.Contains("Minecraft.Windows.exe");
        bool isJava = fileName.Contains("java.exe") || fileName.Contains("javaw.exe");

        // 2. 基岩版及 Java 版特化处理
        if (isBedrock)
        {
            HandleBedrockPreStart(ref fileName, ref workDirectory);
        }
        else if (isJava)
        {
            if (WpfConfig.UseJavaExe)
            {
                fileName = fileName.Replace("javaw.exe", "java.exe");
            }
            if (!string.IsNullOrWhiteSpace(WpfConfig.CustomJVMArguments))
            {
                args = MergeMinecraftArgs(args, WpfConfig.CustomJVMArguments);
                WpfConfig.DefaultLogger.Info($"[进程] 已注入自定义JVM参数");
            }
        }
        

        // =================================================================
        // 3. 【核心判定逻辑重构】：
        // 只要当前处于 wb.a 的调用链路内，且开启了 WPF 日志，就启用日志
        // =================================================================
        bool useWpfLog = _isInvokedByWbA.Value && WpfConfig.ShowLogInWpf;
        
        // 4. 游戏启动前：触发内存优化
        if (_isInvokedByWbA.Value)
        {
            if (WpfConfig.MemoryOptimize)
            {
                OptimizeMemoryBeforeLaunch(); 
            }
        }

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

        // 5. 核心进程对象组装
        var processObj = new aqq
        {
            StartInfo =
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,        
                RedirectStandardOutput = useWpfLog || !isJava, 
                RedirectStandardError  = useWpfLog || !isJava,  
                CreateNoWindow         = useWpfLog || !isJava 
            },
            Type = startType
        };
        
        if (!string.IsNullOrEmpty(workDirectory)) 
        {
            processObj.StartInfo.WorkingDirectory = workDirectory;
        }

        // 绝对不能删，防止启动器通信时空引用崩溃
        aqr.Instance.c(processObj);

        // 6. 绑定标准输出与错误
        processObj.OutputDataReceived += (sender, outputArgs) =>
        {
            if (!string.IsNullOrEmpty(outputArgs.Data))
            {
                string cleanMsg = AnsiColorRegex.Replace(outputArgs.Data, string.Empty);
                if (WpfConfig.ShowLogInConsole) Console.WriteLine(cleanMsg);
                if (useWpfLog) logWindow?.AppendLog(cleanMsg, isError: false);
            }
        };
        processObj.ErrorDataReceived += (sender, errorArgs) =>
        {
            if (!string.IsNullOrEmpty(errorArgs.Data))
            {
                string cleanMsg = AnsiColorRegex.Replace(errorArgs.Data, string.Empty);
                if (WpfConfig.ShowLogInConsole) Console.WriteLine($"[StdErr] {cleanMsg}");
                if (useWpfLog) logWindow?.AppendLog(cleanMsg, isError: true);
            }
        };

        // 7. 绑定进程退出与主动结束逻辑
        if (useWpfLog)
        {
            processObj.EnableRaisingEvents = true; 
            
            processObj.Exited += (sender, e) =>
            {
                int exitCode = -1;
                try { exitCode = processObj.ExitCode; } catch { }
                
                logWindow?.OnProcessExited(exitCode);
                Console.WriteLine($"\n[进程] 进程 {Path.GetFileName(fileName)} 已退出, 错误代码: {exitCode}");
            };

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

    #endregion

    [OriginalMethod]
    internal void OutputProcess(object sender, DataReceivedEventArgs receivedData)
    {
    }

    #region 私有辅助方法

    private void HandleBedrockPreStart(ref string fileName, ref string workDirectory)
    {
        if (WpfConfig.EnableCustomBedrockSelect)
        {
            string tempFileName = fileName;
            string tempWorkDir = workDirectory;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new BedrockPathSelectWindow(selectedPath =>
                {
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        tempFileName = Path.Combine(selectedPath, "Minecraft.Windows.exe");
                        tempWorkDir = selectedPath;
                    }
                });
                window.ShowDialog();
            });

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
    
    private static string MergeMinecraftArgs(string originalArgs, string customArgs)
    {
        if (string.IsNullOrWhiteSpace(customArgs)) return originalArgs;

        var customTokens = SplitArgs(customArgs);
        string newArgs = originalArgs;

        for (int i = 0; i < customTokens.Count; i++)
        {
            string token = customTokens[i];
            if (token.StartsWith("-"))
            {
                if (token.StartsWith("-Xmx") || token.StartsWith("-Xms"))
                {
                    string prefix = token.Substring(0, 4); 
                    if (Regex.IsMatch(newArgs, $@"\{prefix}\S+"))
                        newArgs = Regex.Replace(newArgs, $@"\{prefix}\S+", token);
                    else
                        newArgs = token + " " + newArgs;
                    continue;
                }

                string nextToken = (i + 1 < customTokens.Count) ? customTokens[i + 1] : null;
                bool hasValue = nextToken != null && !nextToken.StartsWith("-");

                if (hasValue)
                {
                    string escapedKey = Regex.Escape(token);
                    string pattern = $@"{escapedKey}\s+(?:""[^""]*""|\S+)";
                    string replacement = $"{token} {nextToken}";
                    
                    if (Regex.IsMatch(newArgs, pattern))
                        newArgs = Regex.Replace(newArgs, pattern, replacement); 
                    else
                        newArgs = replacement + " " + newArgs; 
                        
                    i++;
                }
                else
                {
                    if (!newArgs.Contains(token))
                        newArgs = token + " " + newArgs;
                }
            }
        }
        return newArgs;
    }

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

    private static void OptimizeMemoryBeforeLaunch()
    {
        try
        {
            WpfConfig.DefaultLogger.Info("[内存优化] 正在尝试释放系统物理内存...");

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

            for (int command = 2; command <= 4; command++)
            {
                int cmd = command; 
                GCHandle handle = GCHandle.Alloc(cmd, GCHandleType.Pinned);
                try
                {
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
}