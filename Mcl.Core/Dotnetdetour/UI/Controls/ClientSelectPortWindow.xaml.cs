using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Globals;

namespace Mcl.Core.Dotnetdetour.UI.Controls
{
    public partial class ClientSelectPortWindow : Window
    {
        public int SelectedPort => int.TryParse(PortTextBox.Text, out var port) ? port : 25565;

        public ClientSelectPortWindow()
        {
            InitializeComponent();
            TopMostCheck.IsChecked = WpfConfig.IsWindowTopMost;
            Topmost = WpfConfig.IsWindowTopMost;
            if (WebRtcVar.Port > 0 && WebRtcVar.Port <= 65535) PortTextBox.Text = WebRtcVar.Port.ToString();
        }

        private void OnTopMostChanged(object sender, RoutedEventArgs e) => Topmost = TopMostCheck.IsChecked == true;
        private void NumberValidation(object sender, TextCompositionEventArgs e) => e.Handled = !e.Text.All(char.IsDigit);

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PortTextBox.Text) || !int.TryParse(PortTextBox.Text, out var port) || port < 1 || port > 65535)
            {
                StatusLabel.Text = "请输入 1-65535 之间的有效端口";
                StatusLabel.Visibility = Visibility.Visible;
                
                var animation = new ThicknessAnimation { From = new Thickness(-4,0,4,0), To = new Thickness(4,0,-4,0), Duration = TimeSpan.FromMilliseconds(50), AutoReverse = true, RepeatBehavior = new RepeatBehavior(3) };
                PortTextBox.BeginAnimation(MarginProperty, animation);
                return;
            }
            WebRtcVar.Port = port;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}