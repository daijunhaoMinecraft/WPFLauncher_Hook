using System;
using System.IO;
using System.Text;
using NLog;
using NLog.Targets;

namespace Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

/// <summary>File-only NLog target that keeps authentication fields out of persisted diagnostics.</summary>
internal sealed class RedactingFileTarget : TargetWithLayout
{
    private readonly object _writeLock = new object();
    private static readonly Encoding Utf8 = new UTF8Encoding(false);
    private string _resolvedFileName;

    public string FileName { get; set; }

    protected override void InitializeTarget()
    {
        base.InitializeTarget();
        if (string.IsNullOrWhiteSpace(FileName))
            throw new InvalidOperationException("A log file path is required.");

        _resolvedFileName = Path.GetFullPath(FileName);
        var directory = Path.GetDirectoryName(_resolvedFileName);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    }

    protected override void Write(LogEventInfo logEvent)
    {
        if (logEvent == null) return;
        var rendered = Layout?.Render(logEvent) ?? logEvent.FormattedMessage;
        var safeLine = LogSanitizer.RedactSensitiveAuthValues(rendered);
        lock (_writeLock)
        {
            using (var stream = new FileStream(_resolvedFileName, FileMode.Append, FileAccess.Write,
                       FileShare.ReadWrite | FileShare.Delete))
            using (var writer = new StreamWriter(stream, Utf8))
                writer.WriteLine(safeLine);
        }
    }
}
