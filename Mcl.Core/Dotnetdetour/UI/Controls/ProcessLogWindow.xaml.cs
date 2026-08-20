using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WPFLauncher.Manager
{
    public partial class ProcessLogWindow : Window
    {
        private bool _autoScroll = true; // 是否处于自动滚动模式
        private Action _killProcessAction; // 外部注入的结束进程方法

        public ProcessLogWindow(string processName)
        {
            InitializeComponent();
            TitleTextBlock.Text = $"运行日志 - {processName}";
            this.Title = TitleTextBlock.Text;
        }

        /// <summary>
        /// 接收外部传入的结束进程方法
        /// </summary>
        public void SetKillAction(Action killAction)
        {
            _killProcessAction = killAction;
        }

        /// <summary>
        /// 提供给外部写入日志的方法，自动调度到 UI 线程
        /// </summary>
        public void AppendLog(string text, bool isError = false)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                string prefix = isError ? "[StdErr] " : "";
                LogTextBox.AppendText(prefix + text + Environment.NewLine);
            }));
        }

        /// <summary>
        /// 进程结束时由外部调用
        /// </summary>
        public void OnProcessExited(int exitCode)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AppendLog($"\n=======================================================\n进程已退出, 错误代码: {exitCode}\n=======================================================");
                
                // 进程已退出，禁用“结束进程”按钮，并将颜色变灰
                KillProcessButton.IsEnabled = false;
                KillProcessButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
                KillProcessButton.Cursor = System.Windows.Input.Cursors.Arrow;
            }));
        }

        /// <summary>
        /// 结束进程按钮点击事件
        /// </summary>
        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要强制结束该进程吗？这可能会导致游戏数据丢失！", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _killProcessAction?.Invoke();
            }
        }

        /// <summary>
        /// 核心滚动逻辑：判断用户是否在查看历史日志
        /// </summary>
        private void LogScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange > 0)
            {
                if (_autoScroll) LogScroller.ScrollToEnd();
            }
            else if (e.ExtentHeightChange == 0)
            {
                if (LogScroller.VerticalOffset >= LogScroller.ScrollableHeight - 2)
                    _autoScroll = true; // 滚回最底部，恢复自动滚动
                else
                    _autoScroll = false; // 往上滚，暂停自动滚动
            }
        }

        /// <summary>
        /// 导出日志按钮逻辑
        /// </summary>
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
                    File.WriteAllText(saveFileDialog.FileName, LogTextBox.Text);
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