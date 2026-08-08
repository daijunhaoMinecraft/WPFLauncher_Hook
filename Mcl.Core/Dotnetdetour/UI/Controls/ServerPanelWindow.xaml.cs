using System.Windows;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.UI.Controls
{
    public partial class ServerPanel : Window
    {
        public ServerPanel()
        {
            InitializeComponent();

            // 初始化置顶状态
            TopMostCheck.IsChecked = WpfConfig.IsWindowTopMost;
            Topmost = WpfConfig.IsWindowTopMost;
        }

        private void OnTopMostChanged(object sender, RoutedEventArgs e)
        {
            Topmost = TopMostCheck.IsChecked == true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 此处可添加任何关闭前的处理逻辑（原代码为空）
            Close();
        }
    }
}