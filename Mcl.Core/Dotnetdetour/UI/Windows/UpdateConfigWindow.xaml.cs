using System.Windows;
using Mcl.Core.Updater;

namespace Mcl.Core.Dotnetdetour.UI.Windows
{
    public partial class UpdateConfigWindow : Window
    {
        public UpdateConfigWindow(bool isDisableHookUpdate, bool isBuildChannel, bool isDisableLauncherUpdate,
            string channelDisplayName)
        {
            InitializeComponent();
            ChkDisableHook.IsChecked = isDisableHookUpdate;
            CmbChannel.SelectedIndex = isBuildChannel ? 0 : 1;
            ChkDisableLauncher.IsChecked = isDisableLauncherUpdate;
            ChannelText.Text = "启动器更新渠道：" +
                               (string.IsNullOrWhiteSpace(channelDisplayName) ? "自动识别" : channelDisplayName);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            UpdateManager.CurrentConfig.DisableUpdate = ChkDisableHook.IsChecked == true;
            UpdateManager.CurrentConfig.IsBuildChannel = CmbChannel.SelectedIndex == 0;
            UpdateManager.CurrentConfig.DisableLauncherUpdate = ChkDisableLauncher.IsChecked == true;
            UpdateManager.SaveConfig();
            this.Close();
        }
    }
}
