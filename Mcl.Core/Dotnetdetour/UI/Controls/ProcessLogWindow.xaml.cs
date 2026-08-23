using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Mcl.Core.Dotnetdetour.UI.Controls
{
    public partial class ProcessLogWindow : Window
    {
        private bool _autoScroll = true;
        private Action _killProcessAction;

        // 【性能优化】使用队列和后台缓冲，避免高频 AppendText 导致 UI 线程卡死
        private ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private StringBuilder _fullLogBuffer = new StringBuilder(); // 用于导出完整的日志
        private DispatcherTimer _flushTimer;
        
        // 限制 UI TextBox 最大显示的字符数（超出自动截断开头，约保留最新的几千行）
        private const int MaxUiLogLength = 50000;

        public ProcessLogWindow(string processName)
        {
            InitializeComponent();
            TitleTextBlock.Text = $"运行日志 - {processName}";
            this.Title = TitleTextBlock.Text;

            // 初始化定时器，每 100 毫秒批量将队列中的日志刷入 UI
            _flushTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _flushTimer.Tick += FlushTimer_Tick;
            _flushTimer.Start();
        }

        public void SetKillAction(Action killAction)
        {
            _killProcessAction = killAction;
        }

        /// <summary>
        /// 提供给外部写入日志的方法，现已改为非阻塞队列模式
        /// </summary>
        public void AppendLog(string text, bool isError = false)
        {
            string prefix = isError ? "[StdErr] " : "";
            string logLine = prefix + text + Environment.NewLine;
            
            // 压入队列，并记录到全量缓存中
            _logQueue.Enqueue(logLine);
            lock (_fullLogBuffer)
            {
                _fullLogBuffer.Append(logLine);
            }
        }

        /// <summary>
        /// 定时将日志批量输出到 UI
        /// </summary>
        private void FlushTimer_Tick(object sender, EventArgs e)
        {
            if (_logQueue.IsEmpty) return;

            StringBuilder batchBuilder = new StringBuilder();
            while (_logQueue.TryDequeue(out string line))
            {
                batchBuilder.Append(line);
            }

            LogTextBox.AppendText(batchBuilder.ToString());

            // 【防止内存与 UI 崩溃】截断过长的文本
            if (LogTextBox.Text.Length > MaxUiLogLength)
            {
                // 截取末尾的文本，保留最新的日志
                LogTextBox.Text = LogTextBox.Text.Substring(LogTextBox.Text.Length - MaxUiLogLength);
            }

            if (_autoScroll)
            {
                LogScroller.ScrollToEnd();
            }
        }

        public void OnProcessExited(int exitCode)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // 停止定时器，并做最后一次强制刷新
                _flushTimer?.Stop();
                FlushTimer_Tick(null, null);

                string exitMessage = $"\n=======================================================\n进程已退出, 错误代码: {exitCode}\n=======================================================\n";
                LogTextBox.AppendText(exitMessage);
                
                lock (_fullLogBuffer)
                {
                    _fullLogBuffer.Append(exitMessage);
                }

                if (_autoScroll) LogScroller.ScrollToEnd();
                
                KillProcessButton.IsEnabled = false;
                KillProcessButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
                KillProcessButton.Cursor = System.Windows.Input.Cursors.Arrow;
            }));
        }

        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要强制结束该进程吗？这可能会导致游戏数据丢失！", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _killProcessAction?.Invoke();
            }
        }

        private void LogScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange > 0)
            {
                if (_autoScroll) LogScroller.ScrollToEnd();
            }
            else if (e.ExtentHeightChange == 0)
            {
                if (LogScroller.VerticalOffset >= LogScroller.ScrollableHeight - 2)
                    _autoScroll = true;
                else
                    _autoScroll = false;
            }
        }

        private void ExportLog_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"GameLog_{DateTime.Now:yyyyMMdd_HHmmss}.log",
                Title = "导出运行日志"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 【改用全量缓存导出】不损失任何被截断的头部历史记录
                    string logContent;
                    lock (_fullLogBuffer)
                    {
                        logContent = _fullLogBuffer.ToString();
                    }
                    File.WriteAllText(saveFileDialog.FileName, logContent);
                    MessageBox.Show("日志导出成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}