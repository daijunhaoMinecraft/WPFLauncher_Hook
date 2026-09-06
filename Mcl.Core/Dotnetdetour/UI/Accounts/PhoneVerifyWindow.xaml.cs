using System;
using System.Threading.Tasks;
using System.Windows;
using Mcl.Core.Dotnetdetour.Features.Authentication.Providers;

namespace Mcl.Core.Dotnetdetour.UI.Forms
{
    public partial class PhoneVerifyWindow : Window
    {
        private readonly string _phone;
        private readonly SmsResult _smsResult;
        public string Ticket { get; private set; }

        public PhoneVerifyWindow(string phone, SmsResult smsResult)
        {
            InitializeComponent();
            _phone = phone;
            _smsResult = smsResult;
            
            bool isUpstream = _smsResult.Status == SmsStatus.UpstreamRequired;
            TitleText.Text = isUpstream ? "需要上行短信验证" : "请输入验证码";
            SubTitleText.Text = $"为手机号 {_phone} 进行安全验证";

            if (isUpstream)
            {
                UpstreamPanel.Visibility = Visibility.Visible;
                CodePanel.Visibility = Visibility.Collapsed;
                UpNumberText.Text = $"接收号码: {_smsResult.UpstreamNumber}";
                UpContentText.Text = $"短信内容: {_smsResult.UpstreamContent}";
                VerifyBtn.Content = "我已发送，检查状态";
            }
        }

        private void OnVerifyClick(object sender, RoutedEventArgs e)
        {
            StatusLabel.Text = "";
            VerifyBtn.IsEnabled = false;
            VerifyBtn.Content = "处理中...";

            string code = CodeBox.Text.Trim();
            string upContent = _smsResult.Status == SmsStatus.UpstreamRequired ? _smsResult.UpstreamContent : "";

            Task.Run(() => MpayLogin.VerifySms(_phone, code, upContent))
                .ContinueWith(t =>
                {
                    // WPF 中不能使用 WinForms 的 BeginInvoke，必须使用 Dispatcher
                    Dispatcher.Invoke(() =>
                    {
                        VerifyBtn.IsEnabled = true;
                        VerifyBtn.Content = _smsResult.Status == SmsStatus.UpstreamRequired ? "我已发送，检查状态" : "立即验证";

                        if (t.Result.Success)
                        {
                            Ticket = t.Result.Ticket;
                            DialogResult = true;
                        }
                        else
                        {
                            StatusLabel.Text = $"验证失败: {t.Result.ErrorMessage}";
                        }
                    });
                });
        }
    }
}