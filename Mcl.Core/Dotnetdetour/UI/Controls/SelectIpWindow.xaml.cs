using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Globals;
using Mcl.Core.Dotnetdetour.Utilities.Network;

namespace Mcl.Core.Dotnetdetour.UI.Controls
{
    public partial class SelectIpWindow : Window
    {
        private readonly string IpPrefix = "10.0.0.";
        public string SelectedIp { get; private set; } = string.Empty;

        public SelectIpWindow()
        {
            InitializeComponent();
            TopMostCheck.IsChecked = WpfConfig.IsWindowTopMost;
            Topmost = WpfConfig.IsWindowTopMost;
            LoadCurrentSettings();
        }

        private void OnTopMostChanged(object sender, RoutedEventArgs e) => Topmost = TopMostCheck.IsChecked == true;

        // 仅允许输入数字
        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void LoadCurrentSettings()
        {
            var savedOctet = IpConfigManager.LoadLastOctet();
            if (savedOctet.HasValue)
            {
                LastOctetTextBox.Text = savedOctet.Value.ToString();
                StatusLabel.Text = $"已加载上次配置: {IpPrefix}{savedOctet.Value}";
                StatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));
                StatusLabel.Visibility = Visibility.Visible;
                return;
            }

            if (!string.IsNullOrEmpty(WebRtcVar.MyVirtualIp) && WebRtcVar.MyVirtualIp.StartsWith(IpPrefix))
            {
                var lastPart = WebRtcVar.MyVirtualIp.Substring(IpPrefix.Length);
                if (int.TryParse(lastPart, out var val))
                {
                    LastOctetTextBox.Text = val.ToString();
                    return;
                }
            }
            LastOctetTextBox.Text = "100";
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LastOctetTextBox.Text) || !int.TryParse(LastOctetTextBox.Text, out var lastOctet) || lastOctet < 0 || lastOctet > 255)
            {
                ShowError("范围错误：必须在 0 到 255 的数字");
                return;
            }

            var candidateIp = $"{IpPrefix}{lastOctet}";
            if (WebRtcVar.PlayerList.Any(p => !string.IsNullOrEmpty(p.VirtualIp) && p.VirtualIp == candidateIp))
            {
                ShowError($"冲突：IP {candidateIp} 已被其他玩家占用");
                return;
            }

            if (!IpConfigManager.SaveLastOctet(lastOctet))
            {
                ShowError("保存配置文件失败 (检查磁盘权限)");
                return;
            }

            WebRtcVar.MyVirtualIp = candidateIp;
            SelectedIp = candidateIp;

            StatusLabel.Text = "验证通过 & 配置已保存";
            StatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16));
            StatusLabel.Visibility = Visibility.Visible;

            await Task.Delay(400);
            DialogResult = true;
        }

        private void ShowError(string message)
        {
            StatusLabel.Text = message;
            StatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 52, 56));
            StatusLabel.Visibility = Visibility.Visible;

            // WPF 的优雅抖动动画
            var animation = new ThicknessAnimation
            {
                From = new Thickness(-4, 0, 4, 0),
                To = new Thickness(4, 0, -4, 0),
                Duration = TimeSpan.FromMilliseconds(50),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };
            InputContainer.BeginAnimation(MarginProperty, animation);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}