using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Mcl.Core.Dotnetdetour.UI.Core;

namespace Mcl.Core.Dotnetdetour.UI.Forms
{
    public partial class CaptchaWindow : Window
    {
        private Func<string> _onRefresh;
        public string CaptchaCode => InputBox.Text.Trim();

        public CaptchaWindow(string initialBase64, Func<string> onRefresh)
        {
            InitializeComponent();
            _onRefresh = onRefresh;
            if (_onRefresh == null) RefreshBtn.Visibility = Visibility.Collapsed;
            
            UpdateImage(initialBase64);
        }

        private void UpdateImage(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return;
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                using (var ms = new MemoryStream(bytes))
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ms;
                    bi.EndInit();
                    CaptchaImage.Source = bi;
                }
            }
            catch { }
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => UpdateImage(_onRefresh?.Invoke());
        private void OnConfirmClick(object sender, RoutedEventArgs e) => DialogResult = true;
        private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
    }

    // 在你的主代码调用处，将原 Helper 改写为:
    public static class CaptchaHelper
    {
        public static string GetOcrWithRefresh(string imageBase64, Func<string> onRefresh)
        {
            string result = null;
            // 确保使用我们在上一步编写的安全的 STA Thread 包裹方法
            ThreadHelperSTATask.Run(() => 
            {
                var window = new CaptchaWindow(imageBase64, onRefresh);
                if (window.ShowDialog() == true)
                    result = window.CaptchaCode;
            });
            return result;
        }
    }
}