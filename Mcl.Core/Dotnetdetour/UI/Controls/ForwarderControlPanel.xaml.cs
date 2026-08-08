using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Globals;
using WPFLauncher.Manager.LanGame;

namespace Mcl.Core.Dotnetdetour.UI.Controls
{
    public partial class ForwarderControlPanel : Window
    {
        private bool isClosing;

        public ForwarderControlPanel()
        {
            InitializeComponent();

            // 初始化置顶
            TopMostCheck.IsChecked = WpfConfig.IsWindowTopMost;
            Topmost = WpfConfig.IsWindowTopMost;

            LoadSettings();
        }

        private void LoadSettings()
        {
            ModeLabel.Text = WebRtcVar.Mode.ToString() ?? "Unknown";
            PortLabel.Text = WebRtcVar.Port > 0 ? WebRtcVar.Port.ToString() : "未设置";
        }

        private void OnTopMostChanged(object sender, RoutedEventArgs e)
        {
            Topmost = TopMostCheck.IsChecked == true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要停止转发服务吗？", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                StopForwarding();
            }
        }

        private void StopForwarding()
        {
            try
            {
                StopButton.IsEnabled = false;
                StopButton.Content = "停止中...";
                StatusLabel.Text = "正在停止...";
                StatusLabel.Foreground = new SolidColorBrush(Colors.Orange);

                // 这里可以插入实际的停止业务代码（原代码仅演示）
                // ...

                StatusLabel.Text = "已停止";
                StatusLabel.Foreground = new SolidColorBrush(Colors.Red);

                // 延迟500ms后关闭窗口
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    if (!this.IsDisposed())
                        Close();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("停止失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StopButton.IsEnabled = true;
                StopButton.Content = "停止转发";
                StatusLabel.Text = "运行中";
                StatusLabel.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isClosing) return;
            isClosing = true;

            // 原关闭时的清理逻辑
            WebRtcVar.LanGameManager.aya.@as(516, WebRtcVar.LanGameManager.HostID);
            WebRtcVar.LanGameManager.aya.d(atl.f);
            Console.WriteLine("停止转发");
        }

        // 辅助方法，检查窗口是否已释放（避免在 Disposed 之后操作）
        private bool IsDisposed() => false; // WPF 中通常不需要，此处仅作兼容
    }
}