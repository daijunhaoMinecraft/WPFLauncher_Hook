using System;
using System.Collections.Concurrent;
using System.Globalization;
using NLog;

namespace Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

/// <summary>Conservative plugin logging facade: English prefix, safe values and repeated-failure suppression.</summary>
public static class PluginLog
{
    private const int SuppressionWindowSeconds = 5;
    private static readonly Logger Logger = LogManager.GetLogger("Mcl.Plugin");
    private static readonly ConcurrentDictionary<string, SuppressionState> Suppressed =
        new ConcurrentDictionary<string, SuppressionState>();

    public static void Debug(string module, object message) => Write(LogLevel.Debug, module, message?.ToString(), null, false);
    public static void Debug(string module, string format, params object[] args) =>
        Write(LogLevel.Debug, module, Format(format, args), null, false);
    public static void Debug(string module, Func<string> messageFactory)
    {
        if (messageFactory == null || !Logger.IsDebugEnabled) return;
        Write(LogLevel.Debug, module, messageFactory(), null, false);
    }

    public static void Info(string module, object message) => Write(LogLevel.Info, module, message?.ToString(), null, false);
    public static void Info(string module, string format, params object[] args) =>
        Write(LogLevel.Info, module, Format(format, args), null, false);
    public static void InfoContent(string module, string label, string content, int maxLength = 512) =>
        Write(LogLevel.Info, module, label + ": " + content, null, false, true, maxLength);
    public static void DebugContent(string module, string label, string content, int maxLength = 512) =>
        Write(LogLevel.Debug, module, label + ": " + content, null, false, true, maxLength);
    public static void Warn(string module, object message) => Write(LogLevel.Warn, module, message?.ToString(), null, true);
    public static void Warn(string module, string format, params object[] args) =>
        Write(LogLevel.Warn, module, Format(format, args), null, true);
    public static void Error(string module, object message) => Write(LogLevel.Error, module, message?.ToString(), null, true);
    public static void Error(string module, Exception exception) => Write(LogLevel.Error, module, exception?.Message, exception, true);
    public static void Error(string module, string format, params object[] args) =>
        Write(LogLevel.Error, module, Format(format, args), null, true);
    public static void Error(string module, Exception exception, string message) =>
        Write(LogLevel.Error, module, message, exception, true);
    public static void Error(string module, Exception exception, string format, params object[] args) =>
        Write(LogLevel.Error, module, Format(format, args), exception, true);
    public static void Fatal(string module, Exception exception, string message) =>
        Write(LogLevel.Fatal, module, message, exception, false);

    public static void Credential(string module, string type, string value) =>
        Debug(module, "Credential summary: " + LogSanitizer.CredentialSummary(type, value));

    private static void Write(LogLevel level, string module, string message, Exception exception, bool suppressRepeats,
        bool allowUnicode = false, int maxLength = 512)
    {
        if (level == LogLevel.Debug && !Logger.IsDebugEnabled) return;
        var exceptionSuffix = exception == null
            ? string.Empty
            : $" exception={exception.GetType().Name}: {exception.Message}";
        var formatted = LogSanitizer.Format(module,
            LogSanitizer.StripLegacyPrefixes(message) + exceptionSuffix, maxLength, allowUnicode);
        var suppressedCount = 0;
        if (suppressRepeats && ShouldSuppress(level, formatted, out suppressedCount)) return;
        if (suppressedCount > 0) formatted += $" suppressed={suppressedCount}";
        // Keep raw exception objects out of plugin events. Their messages can contain
        // credentials or multi-line host text; the sanitized summary above is enough.
        Logger.Log(level, formatted);
    }

    private static bool ShouldSuppress(LogLevel level, string key, out int suppressedCount)
    {
        suppressedCount = 0;
        var now = DateTime.UtcNow;
        var state = Suppressed.GetOrAdd(level.Name + "|" + key, item => new SuppressionState());
        lock (state)
        {
            if (!state.HasEmitted)
            {
                state.HasEmitted = true;
                state.LastSeen = now;
                return false;
            }
            if ((now - state.LastSeen).TotalSeconds < SuppressionWindowSeconds)
            {
                state.SuppressedCount++;
                return true;
            }
            suppressedCount = state.SuppressedCount;
            state.SuppressedCount = 0;
            state.LastSeen = now;
            return false;
        }
    }

    private static string Format(string format, object[] args)
    {
        if (format == null) return string.Empty;
        try { return string.Format(CultureInfo.InvariantCulture, format, args ?? new object[0]); }
        catch (FormatException) { return format; }
    }

    private sealed class SuppressionState
    {
        public bool HasEmitted;
        public DateTime LastSeen;
        public int SuppressedCount;
    }
}
