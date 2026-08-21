using System.Windows;
using Mcl.Core.Updater;

namespace Mcl.Core.Dotnetdetour.UI.Windows
{
    public partial class UpdateConfigWindow : Window
    {
        public UpdateConfigWindow(bool isDisable, bool isBuild)
        {
            InitializeComponent();
            ChkDisable.IsChecked = isDisable;
            CmbChannel.SelectedIndex = isBuild ? 0 : 1;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            UpdateManager.CurrentConfig.DisableUpdate = ChkDisable.IsChecked == true;
            UpdateManager.CurrentConfig.IsBuildChannel = CmbChannel.SelectedIndex == 0;
            UpdateManager.SaveConfig();
            this.Close();
        }
    }
}