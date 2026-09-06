using System;
using System.Collections.Generic;
using System.Text;

namespace Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

/// <summary>A bounded, thread-safe tail with a separately bounded queue for UI delivery.</summary>
public sealed class BoundedLogBuffer
{
    public const int DefaultHistoryLimit = 2 * 1024 * 1024;
    public const int DefaultPendingLimit = 128 * 1024;
    private readonly object _sync = new object();
    private readonly Queue<string> _history = new Queue<string>();
    private readonly Queue<string> _pending = new Queue<string>();
    private readonly int _historyLimit;
    private readonly int _pendingLimit;
    private int _historyLength;
    private int _pendingLength;
    private int _droppedLines;
    private bool _closed;

    public BoundedLogBuffer(int historyLimit = DefaultHistoryLimit, int pendingLimit = DefaultPendingLimit)
    {
        if (historyLimit <= 0) throw new ArgumentOutOfRangeException(nameof(historyLimit));
        if (pendingLimit <= 0) throw new ArgumentOutOfRangeException(nameof(pendingLimit));
        _historyLimit = historyLimit;
        _pendingLimit = pendingLimit;
    }

    public int HistoryLength
    {
        get
        {
            lock (_sync) return _historyLength;
        }
    }

    public int PendingLength
    {
        get
        {
            lock (_sync) return _pendingLength;
        }
    }

    public void Append(string line)
    {
        if (string.IsNullOrEmpty(line)) return;
        lock (_sync)
        {
            if (_closed) return;
            var limit = Math.Min(16 * 1024, Math.Min(_historyLimit, _pendingLimit));
            if (line.Length > limit) line = line.Substring(line.Length - limit);
            _history.Enqueue(line);
            _historyLength += line.Length;
            while (_historyLength > _historyLimit) _historyLength -= _history.Dequeue().Length;
            _pending.Enqueue(line);
            _pendingLength += line.Length;
            while (_pendingLength > _pendingLimit)
            {
                _pendingLength -= _pending.Dequeue().Length;
                _droppedLines++;
            }
        }
    }

    public string Drain(int maximumCharacters = 32 * 1024)
    {
        if (maximumCharacters <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCharacters));
        lock (_sync)
        {
            var batch = new StringBuilder();
            if (_droppedLines > 0)
            {
                batch.AppendLine($"[日志窗口] 输出过快，已跳过 {_droppedLines} 行显示；导出包含保留的最近历史。");
                _droppedLines = 0;
            }

            while (_pending.Count > 0 && batch.Length < maximumCharacters)
            {
                var line = _pending.Dequeue();
                batch.Append(line);
                _pendingLength -= line.Length;
            }

            return batch.ToString();
        }
    }

    public string Snapshot()
    {
        lock (_sync) return string.Concat(_history);
    }

    public void Close()
    {
        lock (_sync)
        {
            _closed = true;
            _pending.Clear();
            _history.Clear();
            _pendingLength = _historyLength = _droppedLines = 0;
        }
    }
}