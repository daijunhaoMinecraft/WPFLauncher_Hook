using System;
using System.IO;
using System.Text;
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
            Target console;
            if (ConsoleEnvironment.HasInteractiveOutput())
            {
                var colored = new ColoredConsoleTarget("mcl-console")
                {
                    Layout = layout,
                    UseDefaultRowHighlightingRules = false
                };
                if (ConsoleEnvironment.IsUtf8Requested)
                    colored.Encoding = ConsoleEnvironment.SelectOutputEncoding(true);
                AddConsoleColors(colored);
                console = colored;
            }
            else if (ConsoleEnvironment.IsUtf8Requested && ConsoleEnvironment.HasOutputDestination())
            {
                // A redirected handle (for example an IDE debugger pipe) has no console screen buffer, so the
                // built-in console targets fail to initialize when an Encoding is requested because setting
                // Console.OutputEncoding throws. Emit UTF-8 bytes directly to the standard output instead.
                console = new Utf8ConsoleTarget { Name = "mcl-console", Layout = layout };
            }
            else
            {
                console = new ConsoleTarget("mcl-console") { Layout = layout };
            }

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

    /// <summary>
    /// Plain-text console target for redirected, non-interactive handles. It encodes the rendered line as
    /// UTF-8 and writes it straight to the standard output stream, avoiding Console.OutputEncoding and any
    /// console color calls that a redirected debugger pipe cannot service.
    /// </summary>
    private sealed class Utf8ConsoleTarget : TargetWithLayout
    {
        private static readonly object WriteSync = new object();
        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        protected override void Write(LogEventInfo logEvent)
        {
            var bytes = Utf8.GetBytes(Layout.Render(logEvent) + Environment.NewLine);
            lock (WriteSync)
            {
                var output = Console.OpenStandardOutput();
                output.Write(bytes, 0, bytes.Length);
                output.Flush();
            }
        }
    }
}