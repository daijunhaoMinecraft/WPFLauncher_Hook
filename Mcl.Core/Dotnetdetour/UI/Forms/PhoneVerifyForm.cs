using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Features.Authentication.Providers;
using Mcl.Core.Dotnetdetour.UI.Core;

namespace Mcl.Core.Dotnetdetour.UI.Forms;

public class PhoneVerifyForm : Form
{
    private readonly string _phone;
    private readonly SmsResult _smsResult;
    private TextBox _codeBox;
    private Label _statusLabel;
    private ModernButton _verifyBtn;

    public string Ticket { get; private set; }

    public PhoneVerifyForm(string phone, SmsResult smsResult)
    {
        _phone = phone;
        _smsResult = smsResult;
        
        bool isUpstream = _smsResult.Status == SmsStatus.UpstreamRequired;
        UITheme.ApplyModernForm(this, "安全验证", 400, isUpstream ? 380 : 300);
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        bool isUpstream = _smsResult.Status == SmsStatus.UpstreamRequired;

        var title = new Label { Text = isUpstream ? "需要上行短信验证" : "请输入验证码", Font = UITheme.TitleFont, ForeColor = UITheme.PrimaryBlue, Location = new Point(25, 25), AutoSize = true };
        var subTitle = new Label { Text = $"为手机号 {_phone} 进行安全验证", ForeColor = UITheme.TextSecondary, Location = new Point(25, 60), AutoSize = true };
        Controls.Add(title); Controls.Add(subTitle);

        var contentPanel = new Panel { Location = new Point(25, 95), Size = new Size(335, isUpstream ? 140 : 80), BackColor = UITheme.Surface, BorderStyle = BorderStyle.FixedSingle };
        Controls.Add(contentPanel);

        if (isUpstream)
        {
            var upLabel = new Label
            {
                Text = $"请使用手机发送以下短信：\n\n接收号码: {_smsResult.UpstreamNumber}\n短信内容: {_smsResult.UpstreamContent}\n\n发送成功后请点击下方检查按钮。",
                Location = new Point(15, 15), Size = new Size(300, 110), Font = UITheme.MainFont
            };
            contentPanel.Controls.Add(upLabel);
        }
        else
        {
            var instruction = new Label { Text = "验证码已发送，请输入 6 位数字：", Location = new Point(15, 15), AutoSize = true };
            _codeBox = new TextBox { Location = new Point(15, 40), Size = new Size(200, 25), Font = UITheme.TitleFont, TextAlign = HorizontalAlignment.Center, MaxLength = 10 };
            contentPanel.Controls.Add(instruction);
            contentPanel.Controls.Add(_codeBox);
        }

        _statusLabel = new Label { Location = new Point(25, contentPanel.Bottom + 10), Size = new Size(335, 20), ForeColor = UITheme.Danger };
        Controls.Add(_statusLabel);

        _verifyBtn = new ModernButton { Text = isUpstream ? "我已发送，检查状态" : "立即验证", Location = new Point(230, contentPanel.Bottom + 40), Size = new Size(130, 36), IsPrimary = true };
        _verifyBtn.Click += OnVerifyClick;
        Controls.Add(_verifyBtn);
    }

    private void OnVerifyClick(object sender, EventArgs e)
    {
        _statusLabel.Text = "";
        _verifyBtn.Enabled = false;
        _verifyBtn.Text = "处理中...";

        string code = _codeBox?.Text?.Trim() ?? "";
        string upContent = _smsResult.Status == SmsStatus.UpstreamRequired ? _smsResult.UpstreamContent : "";

        Task.Run(() => MpayLogin.VerifySms(_phone, code, upContent))
            .ContinueWith(t =>
            {
                BeginInvoke(new Action(() =>
                {
                    _verifyBtn.Enabled = true;
                    _verifyBtn.Text = _smsResult.Status == SmsStatus.UpstreamRequired ? "我已发送，检查状态" : "立即验证";

                    if (t.Result.Success)
                    {
                        Ticket = t.Result.Ticket;
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        _statusLabel.Text = $"验证失败: {t.Result.ErrorMessage}";
                    }
                }));
            });
    }
}