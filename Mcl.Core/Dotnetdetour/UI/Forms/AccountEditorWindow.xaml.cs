using System.Windows;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Mcl.Core.Dotnetdetour.Features.Authentication.Providers;

namespace Mcl.Core.Dotnetdetour.UI.Forms
{
    public partial class AccountEditorWindow : Window
    {
        public AccountInfo Account { get; private set; }
        public string OriginalName { get; private set; }
        public bool IsEditMode { get; private set; }

        public AccountEditorWindow(AccountInfo account = null)
        {
            InitializeComponent();
            
            IsEditMode = account != null;
            if (IsEditMode)
            {
                OriginalName = account.Name;
                Account = account.Clone();
                TitleText.Text = "编辑账号";
                Title = "编辑账号";
            }
            else
            {
                Account = new AccountInfo { Type = AccountType.Cookie };
            }

            BindData();
        }

        private void OnTypeChanged(object sender, RoutedEventArgs e)
        {
            if (PanelCookie == null) return; // 防止初始化时报错
            
            PanelCookie.Visibility = Visibility.Collapsed;
            PanelUserPass.Visibility = Visibility.Collapsed;
            PanelPhone.Visibility = Visibility.Collapsed;

            if (RbCookie.IsChecked == true)
            {
                PanelCookie.Visibility = Visibility.Visible;
            }
            else if (Rb4399.IsChecked == true || RbEmail.IsChecked == true)
            {
                UserLabel.Text = RbEmail.IsChecked == true ? "邮箱账号:" : "用户名:";
                PanelUserPass.Visibility = Visibility.Visible;
            }
            else if (RbPhone.IsChecked == true)
            {
                PanelPhone.Visibility = Visibility.Visible;
            }
        }

        private void BindData()
        {
            NameInput.Text = Account.Name;
            NotesInput.Text = Account.Notes;

            if (Account.Type == AccountType.Cookie) { RbCookie.IsChecked = true; CookieInput.Text = Account.CookieData; }
            if (Account.Type == AccountType._4399) { Rb4399.IsChecked = true; UserInput.Text = Account.Username; PassInput.Password = Account.Password; }
            if (Account.Type == AccountType.Email) { RbEmail.IsChecked = true; UserInput.Text = Account.Username; PassInput.Password = Account.Password; }
            if (Account.Type == AccountType.Phone) { RbPhone.IsChecked = true; PhoneInput.Text = Account.PhoneNumber; }
        }

        private void OnShowPassChanged(object sender, RoutedEventArgs e)
        {
            if (ShowPassCheck.IsChecked == true)
            {
                PassInputVisible.Text = PassInput.Password;
                PassInputVisible.Visibility = Visibility.Visible;
                PassInput.Visibility = Visibility.Collapsed;
            }
            else
            {
                PassInput.Password = PassInputVisible.Text;
                PassInput.Visibility = Visibility.Visible;
                PassInputVisible.Visibility = Visibility.Collapsed;
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameInput.Text)) return;

            Account.Name = NameInput.Text.Trim();
            Account.Notes = NotesInput.Text.Trim();

            // 如果处于显示密码状态，需要将 TextBox 的值同步回 PasswordBox
            if (ShowPassCheck.IsChecked == true) PassInput.Password = PassInputVisible.Text;

            if (RbCookie.IsChecked == true)
            {
                if (!CookieValidator.ValidateSauth(CookieInput.Text.Trim(), out string err))
                {
                    MessageBox.Show(err, "Cookie 格式错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Account.Type = AccountType.Cookie;
                Account.CookieData = CookieInput.Text.Trim();
            }
            else if (Rb4399.IsChecked == true || RbEmail.IsChecked == true)
            {
                Account.Type = RbEmail.IsChecked == true ? AccountType.Email : AccountType._4399;
                Account.Username = UserInput.Text.Trim();
                Account.Password = PassInput.Password.Trim();
            }
            else
            {
                Account.Type = AccountType.Phone;
                Account.PhoneNumber = PhoneInput.Text.Trim();
            }

            DialogResult = true;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}