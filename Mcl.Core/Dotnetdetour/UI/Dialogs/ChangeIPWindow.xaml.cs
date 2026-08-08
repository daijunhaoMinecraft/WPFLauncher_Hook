using System;
using System.IO;
using System.Windows;
using Mcl.Core.Dotnetdetour.Models.Config;
using Newtonsoft.Json;
using WPFLauncher.Model.Game;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks
{
    public partial class ChangeIPWindow : Window
    {
        private readonly akv _roomInfo;

        public ChangeIPWindow(akv roomInfo)
        {
            InitializeComponent();
            _roomInfo = roomInfo;

            // 初始化置顶状态
            TopMostCheck.IsChecked = WpfConfig.IsWindowTopMost;
            Topmost = WpfConfig.IsWindowTopMost;

            LoadRoomInfo();
        }

        private void OnTopMostChanged(object sender, RoutedEventArgs e)
        {
            Topmost = TopMostCheck.IsChecked == true;
        }

        private void LoadRoomInfo()
        {
            if (_roomInfo?.CppGameCfg?.room_info != null)
            {
                IpTextBox.Text = _roomInfo.CppGameCfg.room_info.ip;
                PortTextBox.Text = _roomInfo.CppGameCfg.room_info.port.ToString();
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证IP地址
                if (string.IsNullOrWhiteSpace(IpTextBox.Text))
                {
                    MessageBox.Show("IP地址不能为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 验证端口号
                if (!int.TryParse(PortTextBox.Text, out var port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("端口号必须是 1-65535 之间的整数", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 更新房间信息
                _roomInfo.CppGameCfg.room_info.ip = IpTextBox.Text.Trim();
                _roomInfo.CppGameCfg.room_info.port = port;
                
                var sCppGameConfigPath = _roomInfo.CppGameCfg.path;
                WpfConfig.DefaultLogger.Info($"[CustomIP] CppGamePath: {sCppGameConfigPath}");
                
                // 覆盖 temp.config
                File.WriteAllText(Path.Combine(tb.n, "temp", "temp.config"),
                    JsonConvert.SerializeObject(_roomInfo.CppGameCfg));
                
                WpfConfig.DefaultLogger.Info("[CustomIP] Config Saved!");
                
                WpfConfig.IsSelectedIP = true;
                DialogResult = true; // 自动关闭窗口
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            WpfConfig.IsSelectedIP = true;
            DialogResult = false; // 自动关闭窗口
        }
    }
}