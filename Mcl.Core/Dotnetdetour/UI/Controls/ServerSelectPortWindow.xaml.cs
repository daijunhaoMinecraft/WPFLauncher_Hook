using System.Windows;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Globals;

namespace Mcl.Core.Dotnetdetour.UI.Controls
{
    public partial class ServerSelectPortWindow : Window
    {
        public ServerSelectPortWindow()
        {
            InitializeComponent();
            TopMostCheck.IsChecked = WpfConfig.IsWindowTopMost;
            Topmost = WpfConfig.IsWindowTopMost;
        }

        private void OnTopMostChanged(object sender, RoutedEventArgs e) => Topmost = TopMostCheck.IsChecked == true;

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PortTextBox.Text, out var port))
            {
                WebRtcVar.Ip = IpTextBox.Text;
                WebRtcVar.Port = port;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("端口无效！");
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}