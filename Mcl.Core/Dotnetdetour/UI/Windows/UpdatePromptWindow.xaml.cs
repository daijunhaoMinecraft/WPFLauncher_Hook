using System.Diagnostics;
using System.Windows;
using Mcl.Core.Updater;

namespace Mcl.Core.Dotnetdetour.UI.Windows
{
    public partial class UpdatePromptWindow : Window
    {
        public bool UserAgreed { get; private set; } = false;
        
        // 暴露给外部管理器获取当前选择的下载节点 (0:自动, 1:直连, 2:gh-proxy, 3:ghproxy.net)
        public int SelectedMirrorIndex => CmbMirror.SelectedIndex;
        
        private readonly string _commitUrl;

        public UpdatePromptWindow(string version, string markdownLog, string commitUrl)
        {
            InitializeComponent();
            _commitUrl = commitUrl;
            
            TxtTitle.Text = $"发现新版本：{version}";
            LogViewer.Document = MarkdownWpfParser.Parse(markdownLog);
        }

        private void BtnViewChanges_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_commitUrl)) return;
            
            string urlToOpen = _commitUrl;
            
            // 如果用户选了镜像站，连查看网页也走镜像站！
            if (CmbMirror.SelectedIndex == 2) urlToOpen = "https://gh-proxy.com/" + _commitUrl;
            else if (CmbMirror.SelectedIndex == 3) urlToOpen = "https://ghproxy.net/" + _commitUrl;

            try
            {
                Process.Start(new ProcessStartInfo(urlToOpen) { UseShellExecute = true });
            }
            catch { /* 忽略没装浏览器的报错 */ }
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            UserAgreed = true;
            this.Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            UserAgreed = false;
            this.Close();
        }
    }
}