using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.DotNetTranstor.Tools;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
    /// <summary>
    /// 手机号验证码输入对话框
    /// 支持普通短信验证码和上行短信两种模式
    /// </summary>
    public class PhoneVerifyForm : Form
    {
        private TextBox codeTextBox;
        private Button verifyButton;
        private Button cancelButton;
        private Button retrySmsButton;
        private Label titleLabel;
        private Label instructionLabel;
        private Label phoneLabel;
        private Label errorLabel;
        private Label upstreamLabel;
        private Panel normalPanel;
        private Panel upstreamPanel;

        private readonly Color ThemeColor = Color.FromArgb(0, 120, 215);
        private readonly Font MainFont = new Font("Microsoft YaHei", 9F, FontStyle.Regular);
        private readonly Font BoldFont = new Font("Microsoft YaHei", 9F, FontStyle.Bold);

        private readonly string _phoneNumber;
        private readonly SmsResult _smsResult;

        /// <summary>验证成功后获取的 ticket</summary>
        public string Ticket { get; private set; }

        public PhoneVerifyForm(string phoneNumber, SmsResult smsResult)
        {
            _phoneNumber = phoneNumber;
            _smsResult = smsResult;
            Ticket = null;

            InitializeComponent();
            this.TopMost = Path_Bool.IsWindowTopMost;
        }

        private void InitializeComponent()
        {
            this.Text = "手机号验证";
            this.Size = new Size(450, _smsResult.Status == SmsStatus.UpstreamRequired ? 380 : 280);
            this.BackColor = Color.FromArgb(243, 243, 243);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = MainFont;

            int y = 15;
            int labelWidth = 80;
            int inputLeft = 95;
            int inputWidth = 320;

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
            this.Controls.Add(titleLabel);

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
            this.Controls.Add(phoneLabel);

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
            this.Controls.Add(errorLabel);

            y += 42;

            // Buttons
            int btnWidth = 90;
            int btnHeight = 30;
            int btnY = y;

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
                this.Controls.Add(retrySmsButton);
            }

            cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(this.ClientSize.Width - btnWidth - 15, btnY),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei", 8.5F)
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(cancelButton);

            verifyButton = new Button
            {
                Text = _smsResult.Status == SmsStatus.UpstreamRequired ? "检查验证状态" : "验证",
                Location = new Point(this.ClientSize.Width - btnWidth * 2 - 25, btnY),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeColor,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Bold)
            };
            verifyButton.FlatAppearance.BorderSize = 0;
            verifyButton.Click += OnVerifyClick;
            this.Controls.Add(verifyButton);

            this.AcceptButton = verifyButton;
            this.CancelButton = cancelButton;
        }

        private void BuildNormalPanel(int startY)
        {
            normalPanel = new Panel
            {
                Location = new Point(0, startY),
                Size = new Size(this.ClientSize.Width, 80)
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
                string filtered = "";
                foreach (char c in codeTextBox.Text)
                {
                    if (char.IsDigit(c)) filtered += c;
                }
                if (filtered != codeTextBox.Text)
                {
                    codeTextBox.Text = filtered;
                    codeTextBox.SelectionStart = filtered.Length;
                }
            };
            normalPanel.Controls.Add(codeTextBox);

            this.Controls.Add(normalPanel);
        }

        private void BuildUpstreamPanel(int startY)
        {
            upstreamPanel = new Panel
            {
                Location = new Point(0, startY),
                Size = new Size(this.ClientSize.Width, 135)
            };

            upstreamLabel = new Label
            {
                Text = "⚠ 触发上行短信验证\n\n"
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

            this.Controls.Add(upstreamPanel);
        }

        private void OnVerifyClick(object sender, EventArgs e)
        {
            errorLabel.Text = "";
            verifyButton.Enabled = false;
            verifyButton.Text = "验证中...";

            string code = "";
            string upContent = "";

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
            System.Threading.Tasks.Task.Run(() =>
            {
                var result = MpayPhoneLogin.VerifySms(_phoneNumber, code, upContent);
                return result;
            }).ContinueWith(task =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    verifyButton.Enabled = true;
                    verifyButton.Text = _smsResult.Status == SmsStatus.UpstreamRequired
                        ? "检查验证状态"
                        : "验证";

                    var result = task.Result;
                    if (result.Success)
                    {
                        Ticket = result.Ticket;
                        Tool.PrintYellow("[MpayPhone] 验证成功!");
                        this.DialogResult = DialogResult.OK;
                        this.Close();
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

            System.Threading.Tasks.Task.Run(() =>
            {
                return MpayPhoneLogin.SendSms(_phoneNumber);
            }).ContinueWith(task =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    retrySmsButton.Enabled = true;
                    retrySmsButton.Text = "重新发送";

                    var result = task.Result;
                    if (result.Status == SmsStatus.Success)
                    {
                        errorLabel.Text = "";
                        Tool.PrintYellow("[MpayPhone] 验证码已重新发送");
                    }
                    else
                    {
                        errorLabel.Text = $"重新发送失败: {result.ErrorMessage}";
                    }
                }));
            });
        }
    }
}
