using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

/// <summary>One policy for launcher diagnostics. Game output has separate user switches.</summary>
public static class LauncherLogging
{
    private static readonly object Sync = new object();
    private static readonly Logger Logger = LogManager.GetLogger("Mcl.Core.Diagnostics");
    private static readonly string SessionName = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
    private static volatile bool _verboseEnabled;

    public static bool IsVerboseEnabled => _verboseEnabled;

    public static void Configure(bool verbose, bool writeToFile, string rootDirectory)
    {
        lock (Sync)
        {
            _verboseEnabled = verbose;
            var minimum = verbose ? LogLevel.Debug : LogLevel.Info;
            var layout = "${date:format=HH\\:mm\\:ss.fff} | ${level:uppercase=true} | ${message}" +
                         (verbose ? " ${exception:format=tostring}" : " ${exception:format=message}");
            var configuration = new LoggingConfiguration();
            var console = new ColoredConsoleTarget("mcl-console")
            {
                Layout = layout,
                UseDefaultRowHighlightingRules = false
            };
            AddConsoleColors(console);
            configuration.AddTarget(console);
            configuration.LoggingRules.Add(new LoggingRule("*", minimum, console));
            if (writeToFile)
            {
                var file = new FileTarget("mcl-file")
                {
                    FileName = Path.Combine(rootDirectory, "logs", SessionName + ".log"),
                    Layout = layout,
                    KeepFileOpen = false
                };
                configuration.AddTarget(file);
                configuration.LoggingRules.Add(new LoggingRule("*", minimum, file));
            }

            LogManager.Configuration = configuration;
            LogManager.ReconfigExistingLoggers();
        }
    }

    public static void Debug(object message)
    {
        if (_verboseEnabled) Logger.Debug(message);
    }

    public static void Debug(string format, params object[] arguments)
    {
        if (_verboseEnabled) Logger.Debug(format, arguments);
    }

    public static void Debug(Func<string> messageFactory)
    {
        if (_verboseEnabled && Logger.IsDebugEnabled) Logger.Debug(messageFactory());
    }

    public static void Info(object message) => Logger.Info(message);
    public static void Info(string format, params object[] arguments) => Logger.Info(format, arguments);
    public static void Warn(object message) => Logger.Warn(message);
    public static void Error(object message) => Logger.Error(message);
    public static void Error(string format, params object[] arguments) => Logger.Error(format, arguments);
    public static void Error(Exception exception, string message) => Logger.Error(exception, message);

    private static void AddConsoleColors(ColoredConsoleTarget target)
    {
        target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Fatal",
            ForegroundColor = ConsoleOutputColor.White,
            BackgroundColor = ConsoleOutputColor.DarkRed
        });
        target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Error",
            ForegroundColor = ConsoleOutputColor.Red
        });
        target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Warn",
            ForegroundColor = ConsoleOutputColor.Yellow
        });
        target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Info",
            ForegroundColor = ConsoleOutputColor.Green
        });
        target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Debug",
            ForegroundColor = ConsoleOutputColor.DarkCyan
        });
        target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Trace",
            ForegroundColor = ConsoleOutputColor.DarkGray
        });
    }
}