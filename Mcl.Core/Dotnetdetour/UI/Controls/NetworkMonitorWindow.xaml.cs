using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Entities;
using Mcl.Core.Dotnetdetour.Models.Globals;

namespace Mcl.Core.Dotnetdetour.UI.Controls
{
    public partial class NetworkMonitorWindow : Window
    {
        public NetworkMonitorWindow()
        {
            InitializeComponent();
            TopMostCheck.IsChecked = WpfConfig.IsWindowTopMost;
            Topmost = WpfConfig.IsWindowTopMost;
            RefreshPlayerData();
            
            // F5 快捷键支持
            this.PreviewKeyDown += (s, e) => { if (e.Key == Key.F5) RefreshPlayerData(); };
        }

        private void OnTopMostChanged(object sender, RoutedEventArgs e) => Topmost = TopMostCheck.IsChecked == true;

        public void RefreshPlayerData()
        {
            // 确保在 UI 线程执行
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(RefreshPlayerData);
                return;
            }

            try
            {
                var players = WebRtcVar.PlayerList.ToArray();
                var localIp = WintunRouterService.Instance?.LocalVirtualIp ?? "未分配";
                
                StatusLabel.Text = $"本地状态：● 运行中 | 本地虚拟 IP: {localIp} | 在线节点数：{players.Length}";
                StatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215));

                // 更新 ListView 数据源
                PlayerListView.ItemsSource = players.Select(p => new
                {
                    Name = p.Name ?? "未知",
                    VirtualIp = p.VirtualIp ?? "-",
                    Status = p.PeerId == WebRtcVar.MyPeerId ? "本机 (我)" : (p.Status ?? "在线"),
                    UserID = p.UserID ?? "-",
                    PeerId = p.PeerId,
                    RawData = p // 保存原始对象用于右键复制
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[刷新玩家列表异常] {ex.Message}");
            }
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshPlayerData();

        private LanGamePlayerInfo GetSelectedPlayer()
        {
            dynamic selected = PlayerListView.SelectedItem;
            return selected?.RawData as LanGamePlayerInfo;
        }

        // ==========================================
        // 核心修复：安全写入剪贴板 (带重试机制)
        // ==========================================
        private void SetClipboardTextSafe(string text)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Clipboard.SetText(text);
                    return; // 写入成功直接返回
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // 剪贴板被其他程序占用，休眠 20 毫秒后重试
                    Thread.Sleep(20);
                }
                catch (Exception ex)
                {
                    // 其他未知异常直接打印日志，防止程序崩溃
                    Console.WriteLine($"[剪贴板异常] {ex.Message}");
                    return;
                }
            }
        }

        private void CopySelectedIp(object sender, RoutedEventArgs e)
        {
            var p = GetSelectedPlayer();
            if (p != null && !string.IsNullOrEmpty(p.VirtualIp))
            {
                SetClipboardTextSafe(p.VirtualIp);
                ShowTooltip($"已复制 IP: {p.VirtualIp}");
            }
        }

        private void CopySelectedPeerId(object sender, RoutedEventArgs e)
        {
            var p = GetSelectedPlayer();
            if (p != null)
            {
                SetClipboardTextSafe(p.PeerId);
                ShowTooltip("已复制 PeerID");
            }
        }

        private void CopySelectedInfo(object sender, RoutedEventArgs e)
        {
            var p = GetSelectedPlayer();
            if (p != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"玩家名称：{p.Name}");
                sb.AppendLine($"User ID:   {p.UserID}");
                sb.AppendLine($"Peer ID:   {p.PeerId}");
                sb.AppendLine($"内网 IP:    {p.VirtualIp}");
                sb.AppendLine($"状  态：    {p.Status}");
                
                SetClipboardTextSafe(sb.ToString());
                ShowTooltip("已复制玩家详细信息");
            }
        }

        private async void ShowTooltip(string message)
        {
            var originalText = StatusLabel.Text;
            var originalBrush = StatusLabel.Foreground;
            StatusLabel.Text = $"✅ {message}";
            StatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16));

            await Task.Delay(2000);
            StatusLabel.Text = originalText;
            StatusLabel.Foreground = originalBrush;
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要停止组网服务并关闭虚拟网卡吗？\n所有连接将会中断。", "确认停止", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    WebRtcVar.StopForwarder();
                    WintunRouterService.Instance?.Stop();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"停止服务时发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Close();
            }
        }
    }
}