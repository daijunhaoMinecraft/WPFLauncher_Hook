using System.Windows;
using System.Windows.Input;

namespace Mcl.Core.Dotnetdetour.UI.Dialogs
{
    public partial class InputDialogWindow : Window
    {
        // 向外暴露用户输入的文本
        public string InputText { get; private set; }

        public InputDialogWindow(string title, string defaultText = "")
        {
            InitializeComponent();
            TxtTitle.Text = title;
            InputTextBox.Text = defaultText;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载时自动选中所有文字，方便用户直接修改
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            ConfirmSelection();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // 支持回车确认和ESC取消
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ConfirmSelection();
            else if (e.Key == Key.Escape) BtnCancel_Click(null, null);
        }

        private void ConfirmSelection()
        {
            if (string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                MessageBox.Show("名称不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 网易API有长度限制(通常20字符左右)，可以自己稍微做个限制
            if (InputTextBox.Text.Length > 20)
            {
                MessageBox.Show("名称长度不能超过20个字符！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InputText = InputTextBox.Text.Trim();
            this.DialogResult = true;
            this.Close();
        }
    }
}