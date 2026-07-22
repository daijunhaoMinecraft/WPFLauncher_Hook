using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

// 确保你的项目中可以访问到 WpfConfig 所在的命名空间
// using YourNamespace;

namespace Mcl.Core.Dotnetdetour.HookList;

public class LoggerHook : IMethodHook
{
    // 目标类名: WPFLauncher.Util.te, 目标方法名: a
    [HookMethod("WPFLauncher.Util.te", "a")]
    public void Hook_a(object instance)
    {
        // 1. 先执行原版逻辑，让其内部的 this.b() 执行旧文件清理工作
        Original_a(instance);

        // 2. 接管并覆盖 NLog 配置
        SetupCustomNLog();
    }

    [OriginalMethod]
    public void Original_a(object instance)
    {
    }

    private void SetupCustomNLog()
    {
        LoggingConfiguration config = new LoggingConfiguration();

        // === 1. 强制配置控制台输出 ===
        ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget("console")
        {
            // 照搬原代码的 Layout
            Layout = "${date:format=HH\\:mm\\:ss.fff} | ${level} | ${stacktrace} | ${message} | ${exception:format=tostring}",
            
            // 关闭默认颜色规则，完全使用我们自定义的规则
            UseDefaultRowHighlightingRules = false 
        };

        // --- 开始添加自定义颜色规则 (高优先级的写在前面) ---

        // Fatal: 致命错误 -> 白字，暗红背景 (极度醒目)
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule()
        {
            Condition = "level == LogLevel.Fatal",
            ForegroundColor = ConsoleOutputColor.White,
            BackgroundColor = ConsoleOutputColor.DarkRed
        });

        // Error: 错误 -> 红字 (醒目)
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule()
        {
            Condition = "level == LogLevel.Error",
            ForegroundColor = ConsoleOutputColor.Red
        });

        // Warn: 警告 -> 黄字 (警示)
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule()
        {
            Condition = "level == LogLevel.Warn",
            ForegroundColor = ConsoleOutputColor.Yellow
        });

        // Info: 正常信息 -> 绿字 (代表一切正常，如果觉得绿色太亮可以改成 ConsoleOutputColor.White)
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule()
        {
            Condition = "level == LogLevel.Info",
            ForegroundColor = ConsoleOutputColor.Green 
        });

        // Debug: 调试信息 -> 深青色 (区分于普通信息，且不过于抢眼)
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule()
        {
            Condition = "level == LogLevel.Debug",
            ForegroundColor = ConsoleOutputColor.DarkCyan
        });

        // Trace: 追踪信息 -> 深灰色 (最低优先级，视觉弱化)
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule()
        {
            Condition = "level == LogLevel.Trace",
            ForegroundColor = ConsoleOutputColor.DarkGray
        });

        // --- 颜色规则添加结束 ---

        config.AddTarget(consoleTarget);
        // 这里你原本写的是 Debug 级别以上输出到控制台
        config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, consoleTarget));

        // === 2. 检查开关，决定是否输出到本地文件夹 ===
        if (WpfConfig.IsLogOutputFolder) // 假设 WpfConfig 是你的配置类
        {
            FileTarget fileTarget = new FileTarget("logFile");
            
            // 将输出路径重定向到: 当前运行路径/logs
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            
            // 保持原版的按日期分文件夹、按时间命名文件的格式
            fileTarget.FileName = Path.Combine(logDirectory, "${shortdate}", "${cached:${longdate:format=yyyy-MM-dd HH\\:mm\\:ss.fff}:cached=true}.log");
            
            // 照搬原代码的日志内容 Layout
            fileTarget.Layout = "${date:format=HH\\:mm\\:ss.fff} | ${level} | ${stacktrace:separator= > } | ${message} | ${exception:format=tostring,StackTrace}";
            
            config.AddTarget(fileTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, fileTarget));
        }

        // === 3. 生效配置 ===
        // 直接将新的 config 赋值给 LogManager
        LogManager.Configuration = config;
    }
}