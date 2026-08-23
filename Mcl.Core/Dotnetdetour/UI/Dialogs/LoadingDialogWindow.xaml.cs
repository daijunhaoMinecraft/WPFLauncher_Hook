using System.Windows;

namespace Mcl.Core.Dotnetdetour.UI.Dialogs
{
    public partial class LoadingDialogWindow : Window
    {
        public LoadingDialogWindow()
        {
            InitializeComponent();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // 注意：这只会关掉UI界面，无法真正撤回已发送给服务端的备份请求。
            // 它的作用是让玩家不用一直卡在这里。
            this.Close();
        }
    }
}