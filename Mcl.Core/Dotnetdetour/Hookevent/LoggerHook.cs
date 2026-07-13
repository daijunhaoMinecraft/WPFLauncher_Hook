using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

// 确保你的项目中可以访问到 Path_Bool 所在的命名空间
// using YourNamespace;

namespace Mcl.Core.Dotnetdetour.Hookevent;

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
            Layout = "${date:format=HH\\:mm\\:ss.fff} | ${level} | ${stacktrace} | ${message} | ${exception:format=tostring}"
        };
        config.AddTarget(consoleTarget);
        config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, consoleTarget));

        // === 2. 检查开关，决定是否输出到本地文件夹 ===
        if (Path_Bool.IsLogOutputFolder)
        {
            FileTarget fileTarget = new FileTarget("logFile");
            
            // 将输出路径重定向到: 当前运行路径/logs
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            
            // 保持原版的按日期分文件夹、按时间命名文件的格式
            // 注意：NLog 的字符串模板需要原样保留
            fileTarget.FileName = Path.Combine(logDirectory, "${shortdate}", "${cached:${longdate:format=yyyy-MM-dd HH\\:mm\\:ss.fff}:cached=true}.log");
            
            // 照搬原代码的日志内容 Layout
            fileTarget.Layout = "${date:format=HH\\:mm\\:ss.fff} | ${level} | ${stacktrace:separator= > } | ${message} | ${exception:format=tostring,StackTrace}";
            
            config.AddTarget(fileTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, fileTarget));
        }

        // === 3. 生效配置 ===
        // 直接将新的 config 赋值给 LogManager。
        // 这会丢弃原版配置中的网易目标目录 (tb.j) 和上传器 (uploader/tc)。
        LogManager.Configuration = config;
    }
}