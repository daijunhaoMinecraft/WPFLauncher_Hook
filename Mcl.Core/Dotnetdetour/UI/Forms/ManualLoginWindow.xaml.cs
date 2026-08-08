using System.Windows;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Mcl.Core.Dotnetdetour.Features.Authentication.Providers;

namespace Mcl.Core.Dotnetdetour.UI.Forms
{
    public partial class ManualLoginWindow : Window
    {
        public AccountInfo GeneratedAccount { get; private set; }

        public ManualLoginWindow()
        {
            InitializeComponent();
        }

        private void OnTypeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Input1 == null) return;
            Input1.Text = "";
            Input2Pass.Password = "";
            Input2Visible.Text = "";

            int idx = TypeCombo.SelectedIndex;
            if (idx == 0) // Cookie
            {
                Label1.Content = "Cookie 数据:";
                Label2.Visibility = Input2Container.Visibility = Visibility.Collapsed;
            }
            else if (idx == 1 || idx == 3) // 4399 或 邮箱
            {
                Label1.Content = idx == 3 ? "邮箱账号:" : "用户名:";
                Label2.Visibility = Input2Container.Visibility = Visibility.Visible;
                Label2.Content = "密码:";
            }
            else // Phone
            {
                Label1.Content = "手机号:";
                Label2.Visibility = Input2Container.Visibility = Visibility.Collapsed;
            }
        }

        private void OnShowPassChanged(object sender, RoutedEventArgs e)
        {
            if (ShowPassCheck.IsChecked == true)
            {
                Input2Visible.Text = Input2Pass.Password;
                Input2Visible.Visibility = Visibility.Visible;
                Input2Pass.Visibility = Visibility.Collapsed;
            }
            else
            {
                Input2Pass.Password = Input2Visible.Text;
                Input2Pass.Visibility = Visibility.Visible;
                Input2Visible.Visibility = Visibility.Collapsed;
            }
        }

        private void OnLoginClick(object sender, RoutedEventArgs e)
        {
            GeneratedAccount = new AccountInfo { Name = "临时手动账号" };
            int idx = TypeCombo.SelectedIndex;
            
            if (ShowPassCheck.IsChecked == true) Input2Pass.Password = Input2Visible.Text;

            if (idx == 0)
            {
                if (string.IsNullOrWhiteSpace(Input1.Text)) return;
                if (!CookieValidator.ValidateSauth(Input1.Text, out string err))
                {
                    MessageBox.Show(err, "Cookie 格式错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                GeneratedAccount.Type = AccountType.Cookie;
                GeneratedAccount.CookieData = Input1.Text;
            }
            else if (idx == 1 || idx == 3)
            {
                if (string.IsNullOrWhiteSpace(Input1.Text) || string.IsNullOrWhiteSpace(Input2Pass.Password)) return;
                GeneratedAccount.Type = idx == 3 ? AccountType.Email : AccountType._4399;
                GeneratedAccount.Username = Input1.Text;
                GeneratedAccount.Password = Input2Pass.Password;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Input1.Text)) return;
                GeneratedAccount.Type = AccountType.Phone;
                GeneratedAccount.PhoneNumber = Input1.Text;
            }

            DialogResult = true;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}