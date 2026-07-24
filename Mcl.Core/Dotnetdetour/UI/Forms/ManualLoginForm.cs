using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Mcl.Core.Dotnetdetour.UI.Core;

namespace Mcl.Core.Dotnetdetour.UI.Forms;

public class ManualLoginForm : Form
{
    private ComboBox _typeCombo;
    private TextBox _input1, _input2;
    private Label _label1, _label2;
    private CheckBox _showPassCheck;
    
    public AccountInfo GeneratedAccount { get; private set; }

    public ManualLoginForm()
    {
        UITheme.ApplyModernForm(this, "手动输入账号 (一次性)", 450, 400);
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var title = new Label { Text = "临时登录", Font = UITheme.TitleFont, ForeColor = UITheme.PrimaryBlue, Location = new Point(20, 20), AutoSize = true };
        Controls.Add(title);

        var typeLabel = new Label { Text = "账号类型", Location = new Point(20, 65), AutoSize = true };
        Controls.Add(typeLabel);

        _typeCombo = new ComboBox { Location = new Point(20, 90), Size = new Size(340, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _typeCombo.Items.AddRange(new string[] { "Cookie 登录", "4399 账号密码", "手机号验证码" });
        _typeCombo.SelectedIndex = 0;
        _typeCombo.SelectedIndexChanged += OnTypeChanged;
        Controls.Add(_typeCombo);

        _label1 = new Label { Location = new Point(20, 130), AutoSize = true };
        _input1 = new TextBox { Location = new Point(20, 155), Size = new Size(340, 25) };
        Controls.Add(_label1);
        Controls.Add(_input1);

        _label2 = new Label { Location = new Point(20, 195), AutoSize = true };
        _input2 = new TextBox { Location = new Point(20, 220), Size = new Size(340, 25), UseSystemPasswordChar = true };
        Controls.Add(_label2);
        Controls.Add(_input2);

        _showPassCheck = new CheckBox { Text = "显示密码", Location = new Point(20, 255), AutoSize = true, Visible = false };
        _showPassCheck.CheckedChanged += (s, e) => _input2.UseSystemPasswordChar = !_showPassCheck.Checked;
        Controls.Add(_showPassCheck);

        // 将原来的按钮 Y 坐标由 260 改为 300，避免拥挤：
        var loginBtn = new ModernButton { Text = "登录", Location = new Point(190, 300), IsPrimary = true };
        loginBtn.Click += OnLoginClick;
        var cancelBtn = new ModernButton { Text = "取消", Location = new Point(300, 300) };
        cancelBtn.Click += (s, e) => Close();

        Controls.Add(loginBtn);
        Controls.Add(cancelBtn);

        OnTypeChanged(null, null);
    }

    private void OnTypeChanged(object sender, EventArgs e)
    {
        _input1.Text = _input2.Text = "";
        if (_typeCombo.SelectedIndex == 0) // Cookie
        {
            _label1.Text = "Cookie 数据:";
            _label2.Visible = _input2.Visible = false;
        }
        else if (_typeCombo.SelectedIndex == 1) // 4399
        {
            _label1.Text = "用户名:";
            _label2.Text = "密码:";
            _label2.Visible = _input2.Visible = true;
            _showPassCheck.Visible = true;
        }
        else // Phone
        {
            _showPassCheck.Visible = false;
            _label1.Text = "手机号:";
            _label2.Visible = _input2.Visible = false;
        }
    }

    private void OnLoginClick(object sender, EventArgs e)
    {
        GeneratedAccount = new AccountInfo { Name = "临时手动账号" };
        
        if (_typeCombo.SelectedIndex == 0)
        {
            if (!AccountInfo.CookieValidator.ValidateSauth(_input1.Text, out string err))
            {
                MessageBox.Show(err, "Cookie 格式错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GeneratedAccount.Type = AccountType.Cookie;
            GeneratedAccount.CookieData = _input1.Text;
        }
        else if (_typeCombo.SelectedIndex == 1)
        {
            if (string.IsNullOrWhiteSpace(_input1.Text) || string.IsNullOrWhiteSpace(_input2.Text)) return;
            GeneratedAccount.Type = AccountType._4399;
            GeneratedAccount.Username = _input1.Text;
            GeneratedAccount.Password = _input2.Text;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_input1.Text)) return;
            GeneratedAccount.Type = AccountType.Phone;
            GeneratedAccount.PhoneNumber = _input1.Text;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}