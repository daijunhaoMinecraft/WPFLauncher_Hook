using System.Windows;
using System.Windows.Input;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;

namespace Mcl.Core.Dotnetdetour.UI.Forms
{
    public partial class AccountSelectionWindow : Window
    {
        public AccountInfo SelectedAccount { get; private set; }
        public bool UseOriginalLogin { get; private set; }

        public AccountSelectionWindow()
        {
            InitializeComponent();
            RefreshAccountList();
        }

        private void RefreshAccountList()
        {
            var accounts = AccountManager.GetAllSorted();
            AccountListView.ItemsSource = accounts;
        }

        private void OnAccountSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            bool hasSelection = AccountListView.SelectedItem != null;
            LoginButton.IsEnabled = EditButton.IsEnabled = DeleteButton.IsEnabled = hasSelection;

            if (hasSelection)
            {
                var acc = (AccountInfo)AccountListView.SelectedItem;
                DetailTitleText.Text = $"{acc.Name} [{acc.TypeDisplay}]";
                DetailContentText.Text = acc.Type switch 
                {
                    AccountType.Cookie => "类型: Cookie 数据",
                    AccountType.Phone => $"手机号: {acc.PhoneNumber}",
                    AccountType.Email => $"邮箱账号: {acc.Username}",
                    _ => $"账号: {acc.Username}"
                };
            }
            else
            {
                DetailTitleText.Text = "选择一个账号查看详情";
                DetailContentText.Text = "暂无详细信息";
            }
        }

        private void OnListViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AccountListView.SelectedItem != null) PerformLogin(sender, e);
        }

        private void PerformLogin(object sender, RoutedEventArgs e)
        {
            if (AccountListView.SelectedItem == null) return;
            SelectedAccount = (AccountInfo)AccountListView.SelectedItem;
            AccountManager.MarkUsed(SelectedAccount);
            this.DialogResult = true; 
        }

        private void OnManualClick(object sender, RoutedEventArgs e)
        {
            // 修复 1：调用 ManualLoginWindow，去掉 using
            var manualWindow = new ManualLoginWindow { Owner = this };
            if (manualWindow.ShowDialog() == true)
            {
                SelectedAccount = manualWindow.GeneratedAccount;
                this.DialogResult = true;
            }
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            // 修复 2：调用 AccountEditorWindow，去掉 using
            var dialog = new AccountEditorWindow { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                AccountManager.Add(dialog.Account);
                RefreshAccountList();
            }
        }

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (AccountListView.SelectedItem == null) return;
            var acc = (AccountInfo)AccountListView.SelectedItem;
            
            // 修复 3：调用 AccountEditorWindow，去掉 using
            var dialog = new AccountEditorWindow(acc) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                AccountManager.Update(dialog.OriginalName, dialog.Account);
                RefreshAccountList();
            }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (AccountListView.SelectedItem == null) return;
            var acc = (AccountInfo)AccountListView.SelectedItem;
            
            // 修复 WPF 版本的 MessageBox
            var result = MessageBox.Show($"确定要删除账号 \"{acc.Name}\" 吗？", "确认删除", 
                                         MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.OK)
            {
                AccountManager.Delete(acc.Name);
                RefreshAccountList();
            }
        }

        private void OnOriginalLoginClick(object sender, RoutedEventArgs e)
        {
            UseOriginalLogin = true;
            this.DialogResult = true;
        }
    }
}