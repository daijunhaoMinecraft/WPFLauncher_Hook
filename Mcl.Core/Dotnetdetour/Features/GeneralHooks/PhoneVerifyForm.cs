using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Features.Authentication.Providers;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

/// <summary>
///     手机号验证码输入对话框
///     支持普通短信验证码和上行短信两种模式
/// </summary>
public class PhoneVerifyForm : Form
{
    private readonly string _phoneNumber;
    private readonly SmsResult _smsResult;
    private readonly Font BoldFont = new("Microsoft YaHei", 9F, FontStyle.Bold);
    private readonly Font MainFont = new("Microsoft YaHei", 9F, FontStyle.Regular);

    private readonly Color ThemeColor = Color.FromArgb(0, 110, 210);
    private Button cancelButton;
    private TextBox codeTextBox;
    private Label errorLabel;
    private Label instructionLabel;
    private Panel normalPanel;
    private Label phoneLabel;
    private Button retrySmsButton;
    private Label titleLabel;
    private Label upstreamLabel;
    private Panel upstreamPanel;
    private Button verifyButton;

    public PhoneVerifyForm(string phoneNumber, SmsResult smsResult)
    {
        _phoneNumber = phoneNumber;
        _smsResult = smsResult;
        Ticket = null;

        InitializeComponent();
        TopMost = WpfConfig.IsWindowTopMost;
    }

    /// <summary>验证成功后获取的 ticket</summary>
    public string Ticket { get; private set; }

    private void InitializeComponent()
    {
        Text = "手机号验证";
        Size = new Size(450, _smsResult.Status == SmsStatus.UpstreamRequired ? 380 : 280);
        BackColor = Color.FromArgb(245, 247, 250);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = MainFont;

        var y = 15;
        var labelWidth = 80;
        var inputLeft = 95;
        var inputWidth = 320;

        // Title
        titleLabel = new Label
        {
            Text = _smsResult.Status == SmsStatus.UpstreamRequired
                ? "需要上行短信验证"
                : "请输入短信验证码",
            Location = new Point(15, y),
            Size = new Size(400, 24),
            Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
            ForeColor = ThemeColor
        };
        Controls.Add(titleLabel);

        y += 32;

        // Phone label
        phoneLabel = new Label
        {
            Text = $"手机号: {_phoneNumber}",
            Location = new Point(15, y),
            Size = new Size(400, 18),
            ForeColor = Color.FromArgb(100, 100, 100),
            Font = new Font("Microsoft YaHei", 8.5F)
        };
        Controls.Add(phoneLabel);

        y += 28;

        if (_smsResult.Status == SmsStatus.UpstreamRequired)
        {
            BuildUpstreamPanel(y);
            y += 150;
        }
        else
        {
            BuildNormalPanel(y);
            y += 100;
        }

        // Error label
        errorLabel = new Label
        {
            Location = new Point(15, y),
            Size = new Size(400, 36),
            ForeColor = Color.FromArgb(209, 52, 56),
            Font = new Font("Microsoft YaHei", 8F)
        };
        Controls.Add(errorLabel);

        y += 42;

        // Buttons
        var btnWidth = 90;
        var btnHeight = 30;
        var btnY = y;

        // Retry SMS button (only for normal mode)
        if (_smsResult.Status == SmsStatus.Success)
        {
            retrySmsButton = new Button
            {
                Text = "重新发送",
                Location = new Point(15, btnY),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei", 8.5F)
            };
            retrySmsButton.FlatAppearance.BorderSize = 0;
            retrySmsButton.Click += OnRetrySmsClick;
            Controls.Add(retrySmsButton);
        }

        cancelButton = new Button
        {
            Text = "取消",
            Location = new Point(ClientSize.Width - btnWidth - 15, btnY),
            Size = new Size(btnWidth, btnHeight),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(200, 200, 200),
            ForeColor = Color.Black,
            Font = new Font("Microsoft YaHei", 8.5F)
        };
        cancelButton.FlatAppearance.BorderSize = 0;
        cancelButton.Click += (s, e) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        Controls.Add(cancelButton);

        verifyButton = new Button
        {
            Text = _smsResult.Status == SmsStatus.UpstreamRequired ? "检查验证状态" : "验证",
            Location = new Point(ClientSize.Width - btnWidth * 2 - 25, btnY),
            Size = new Size(btnWidth, btnHeight),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeColor,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Bold)
        };
        verifyButton.FlatAppearance.BorderSize = 0;
        verifyButton.Click += OnVerifyClick;
        Controls.Add(verifyButton);

        AcceptButton = verifyButton;
        CancelButton = cancelButton;
    }

    private void BuildNormalPanel(int startY)
    {
        normalPanel = new Panel
        {
            Location = new Point(0, startY),
            Size = new Size(ClientSize.Width, 80)
        };

        instructionLabel = new Label
        {
            Text = "验证码已发送至您的手机，请在下方输入:",
            Location = new Point(15, 0),
            Size = new Size(400, 18),
            Font = new Font("Microsoft YaHei", 8.5F),
            ForeColor = Color.FromArgb(80, 80, 80)
        };
        normalPanel.Controls.Add(instructionLabel);

        codeTextBox = new TextBox
        {
            Location = new Point(15, 24),
            Size = new Size(200, 25),
            Font = new Font("Microsoft YaHei", 11F),
            MaxLength = 10,
            TextAlign = HorizontalAlignment.Center
        };
        codeTextBox.TextChanged += (s, e) =>
        {
            // 只允许数字输入
            var filtered = "";
            foreach (var c in codeTextBox.Text)
                if (char.IsDigit(c))
                    filtered += c;
            if (filtered != codeTextBox.Text)
            {
                codeTextBox.Text = filtered;
                codeTextBox.SelectionStart = filtered.Length;
            }
        };
        normalPanel.Controls.Add(codeTextBox);

        Controls.Add(normalPanel);
    }

    private void BuildUpstreamPanel(int startY)
    {
        upstreamPanel = new Panel
        {
            Location = new Point(0, startY),
            Size = new Size(ClientSize.Width, 135)
        };

        upstreamLabel = new Label
        {
            Text = "[!] 触发上行短信验证\n\n"
                   + $"请使用手机 {_phoneNumber} 发送短信:\n"
                   + $"内容: {_smsResult.UpstreamContent}\n"
                   + $"发送至: {_smsResult.UpstreamNumber}\n\n"
                   + "发送完成后，请点击下方按钮检查验证状态",
            Location = new Point(15, 0),
            Size = new Size(400, 120),
            Font = new Font("Microsoft YaHei", 8.5F),
            ForeColor = Color.FromArgb(180, 100, 0)
        };
        upstreamPanel.Controls.Add(upstreamLabel);

        Controls.Add(upstreamPanel);
    }

    private void OnVerifyClick(object sender, EventArgs e)
    {
        errorLabel.Text = "";
        verifyButton.Enabled = false;
        verifyButton.Text = "验证中...";

        var code = "";
        var upContent = "";

        if (_smsResult.Status == SmsStatus.UpstreamRequired)
        {
            upContent = _smsResult.UpstreamContent ?? "";
        }
        else
        {
            code = codeTextBox?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(code))
            {
                errorLabel.Text = "请输入验证码";
                verifyButton.Enabled = true;
                verifyButton.Text = "验证";
                return;
            }
        }

        // 在后台线程执行验证，避免 UI 卡死
        Task.Run(() =>
        {
            var result = MpayPhoneLogin.VerifySms(_phoneNumber, code, upContent);
            return result;
        }).ContinueWith(task =>
        {
            BeginInvoke(new Action(() =>
            {
                verifyButton.Enabled = true;
                verifyButton.Text = _smsResult.Status == SmsStatus.UpstreamRequired
                    ? "检查验证状态"
                    : "验证";

                var result = task.Result;
                if (result.Success)
                {
                    Ticket = result.Ticket;
                    WpfConfig.DefaultLogger.Info("[MpayPhone] 验证成功!");
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    errorLabel.Text = $"验证失败: {result.ErrorMessage}\n请重试或点击取消";
                    if (codeTextBox != null)
                    {
                        codeTextBox.Focus();
                        codeTextBox.SelectAll();
                    }
                }
            }));
        });
    }

    private void OnRetrySmsClick(object sender, EventArgs e)
    {
        errorLabel.Text = "";
        retrySmsButton.Enabled = false;
        retrySmsButton.Text = "发送中...";

        Task.Run(() => { return MpayPhoneLogin.SendSms(_phoneNumber); }).ContinueWith(task =>
        {
            BeginInvoke(new Action(() =>
            {
                retrySmsButton.Enabled = true;
                retrySmsButton.Text = "重新发送";

                var result = task.Result;
                if (result.Status == SmsStatus.Success)
                {
                    errorLabel.Text = "";
                    WpfConfig.DefaultLogger.Info("[MpayPhone] 验证码已重新发送");
                }
                else
                {
                    errorLabel.Text = $"重新发送失败: {result.ErrorMessage}";
                }
            }));
        });
    }
}