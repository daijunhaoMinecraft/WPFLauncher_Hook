using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Microsoft.Win32;

namespace Mcl.Core.Dotnetdetour.UI.Controls;

public partial class ProcessLogWindow : Window
{
    private const int MaxVisibleCharacters = 50000;
    private readonly BoundedLogBuffer _buffer = new BoundedLogBuffer();
    private readonly DispatcherTimer _flushTimer;
    private Action _killProcessAction;
    private bool _autoScroll = true;
    private volatile bool _closed;

    public ProcessLogWindow(string processName)
    {
        InitializeComponent();
        Title = TitleTextBlock.Text = "运行日志 · " + processName;
        _flushTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            { Interval = TimeSpan.FromMilliseconds(100) };
        _flushTimer.Tick += FlushTimerTick;
        _flushTimer.Start();
        Closed += (sender, args) =>
        {
            _closed = true;
            _flushTimer.Stop();
            _flushTimer.Tick -= FlushTimerTick;
            _buffer.Close();
            _killProcessAction = null;
        };
    }

    public void SetKillAction(Action killAction) => _killProcessAction = killAction;

    public void AppendLog(string text, bool isError = false)
    {
        if (!_closed) _buffer.Append((isError ? "[StdErr] " : "") + text + Environment.NewLine);
    }

    private void FlushTimerTick(object sender, EventArgs args)
    {
        var batch = _buffer.Drain();
        if (batch.Length == 0) return;
        LogTextBox.AppendText(batch);
        if (LogTextBox.Text.Length > MaxVisibleCharacters)
            LogTextBox.Text = LogTextBox.Text.Substring(LogTextBox.Text.Length - MaxVisibleCharacters);
        if (_autoScroll) LogScroller.ScrollToEnd();
    }

    public void OnProcessExited(int exitCode)
    {
        if (_closed || Dispatcher.HasShutdownStarted) return;
        AppendLog($"进程已退出，退出代码：{exitCode}");
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_closed) return;
            FlushTimerTick(null, EventArgs.Empty);
            KillProcessButton.IsEnabled = false;
            _killProcessAction = null;
            // Keep the timer until Closed: redirected output callbacks may arrive after Exited.
        }));
    }

    private void KillProcess_Click(object sender, RoutedEventArgs args)
    {
        if (MessageBox.Show(this, "确定要结束该进程吗？未保存的游戏数据可能丢失。", "结束进程",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            _killProcessAction?.Invoke();
    }

    private void LogScroller_ScrollChanged(object sender, ScrollChangedEventArgs args)
    {
        if (args.ExtentHeightChange == 0) _autoScroll = LogScroller.VerticalOffset >= LogScroller.ScrollableHeight - 2;
        else if (_autoScroll) LogScroller.ScrollToEnd();
    }

    private void ExportLog_Click(object sender, RoutedEventArgs args)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "日志文件 (*.log)|*.log|文本文件 (*.txt)|*.txt",
            FileName = $"GameLog_{DateTime.Now:yyyyMMdd_HHmmss}.log",
            Title = "导出最近日志（最多 2 Mi 字符）"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            File.WriteAllText(dialog.FileName, _buffer.Snapshot());
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, "导出失败：" + exception.Message, "导出日志", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}