using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Mcl.Core.Dotnetdetour.UI.Core;

namespace Mcl.Core.Dotnetdetour.UI.Forms;

public class AccountEditorForm : Form
{
    private TextBox _nameInput, _cookieInput, _userInput, _passInput, _phoneInput, _notesInput;
    private RadioButton _rbCookie, _rb4399, _rbPhone;
    private Panel _cardPanel;
    private CheckBox _showPassCheck;

    public AccountInfo Account { get; private set; }
    public string OriginalName { get; private set; }
    public bool IsEditMode { get; private set; }

    public AccountEditorForm(AccountInfo account = null)
    {
        IsEditMode = account != null;
        if (IsEditMode)
        {
            OriginalName = account.Name;
            Account = account.Clone(); // 假设有Clone方法，或者手动赋值
        }
        else
        {
            Account = new AccountInfo { Type = AccountType.Cookie };
        }

        UITheme.ApplyModernForm(this, IsEditMode ? "编辑账号" : "添加账号", 460, 520);
        InitializeComponent();
        BindData();
    }

    private void InitializeComponent()
    {
        int y = 20;

        Controls.Add(new Label { Text = "账号名称", Location = new Point(30, y), AutoSize = true });
        _nameInput = new TextBox { Location = new Point(30, y += 25), Size = new Size(380, 25) };
        Controls.Add(_nameInput);

        Controls.Add(new Label { Text = "账号类型", Location = new Point(30, y += 35), AutoSize = true });
        
        y += 25;
        _rbCookie = new RadioButton { Text = "Cookie", Location = new Point(30, y), Size = new Size(80, 24), Checked = true };
        _rb4399 = new RadioButton { Text = "4399登录", Location = new Point(120, y), Size = new Size(90, 24) };
        _rbPhone = new RadioButton { Text = "手机号", Location = new Point(220, y), Size = new Size(80, 24) };
        Controls.Add(_rbCookie); Controls.Add(_rb4399); Controls.Add(_rbPhone);

        _rbCookie.CheckedChanged += SwitchPanel;
        _rb4399.CheckedChanged += SwitchPanel;
        _rbPhone.CheckedChanged += SwitchPanel;

        _cardPanel = new Panel { Location = new Point(30, y += 35), Size = new Size(380, 140), BackColor = UITheme.Surface, BorderStyle = BorderStyle.FixedSingle };
        Controls.Add(_cardPanel);

        // Inputs within Card Panel (Toggled by SwitchPanel)
        _cookieInput = new TextBox { Location = new Point(15, 15), Size = new Size(350, 110), Multiline = true };
        
        _userInput = new TextBox { Location = new Point(15, 30), Size = new Size(350, 25) };
        _passInput = new TextBox { Location = new Point(15, 90), Size = new Size(270, 25), UseSystemPasswordChar = true };

        _showPassCheck = new CheckBox { Text = "显示", Location = new Point(295, 92), Size = new Size(60, 20) };
        _showPassCheck.CheckedChanged += (s, e) => _passInput.UseSystemPasswordChar = !_showPassCheck.Checked;
        
        _phoneInput = new TextBox { Location = new Point(15, 30), Size = new Size(350, 25) };

        Controls.Add(new Label { Text = "备注 (可选)", Location = new Point(30, y += 155), AutoSize = true });
        _notesInput = new TextBox { Location = new Point(30, y += 25), Size = new Size(380, 60), Multiline = true };
        Controls.Add(_notesInput);

        var saveBtn = new ModernButton { Text = "保存", Location = new Point(230, 430), IsPrimary = true };
        saveBtn.Click += OnSave;
        var cancelBtn = new ModernButton { Text = "取消", Location = new Point(340, 430) };
        cancelBtn.Click += (s, e) => Close();

        Controls.Add(saveBtn);
        Controls.Add(cancelBtn);

        SwitchPanel(null, null);
    }

    private void SwitchPanel(object sender, EventArgs e)
    {
        _cardPanel.Controls.Clear();
        if (_rbCookie.Checked)
        {
            _cardPanel.Controls.Add(new Label { Text = "Cookie 数据:", Location = new Point(15, 15), AutoSize = true, Visible = false });
            _cardPanel.Controls.Add(_cookieInput);
        }
        else if (_rb4399.Checked)
        {
            _cardPanel.Controls.Add(new Label { Text = "用户名:", Location = new Point(15, 10), AutoSize = true });
            _cardPanel.Controls.Add(_userInput);
            _cardPanel.Controls.Add(new Label { Text = "密码:", Location = new Point(15, 70), AutoSize = true });
            _cardPanel.Controls.Add(_passInput);
            _cardPanel.Controls.Add(_showPassCheck);
        }
        else
        {
            _cardPanel.Controls.Add(new Label { Text = "手机号:", Location = new Point(15, 10), AutoSize = true });
            _cardPanel.Controls.Add(_phoneInput);
        }
    }

    private void BindData()
    {
        _nameInput.Text = Account.Name;
        _notesInput.Text = Account.Notes;
        if (Account.Type == AccountType.Cookie) { _rbCookie.Checked = true; _cookieInput.Text = Account.CookieData; }
        if (Account.Type == AccountType._4399) { _rb4399.Checked = true; _userInput.Text = Account.Username; _passInput.Text = Account.Password; }
        if (Account.Type == AccountType.Phone) { _rbPhone.Checked = true; _phoneInput.Text = Account.PhoneNumber; }
    }

    private void OnSave(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_nameInput.Text)) return;

        Account.Name = _nameInput.Text.Trim();
        Account.Notes = _notesInput.Text.Trim();

        if (_rbCookie.Checked)
        {
            if (!AccountInfo.CookieValidator.ValidateSauth(_cookieInput.Text.Trim(), out string err))
            {
                MessageBox.Show(err, "Cookie 格式错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Account.Type = AccountType.Cookie;
            Account.CookieData = _cookieInput.Text.Trim();
        }
        else if (_rb4399.Checked)
        {
            Account.Type = AccountType._4399;
            Account.Username = _userInput.Text.Trim();
            Account.Password = _passInput.Text.Trim();
        }
        else
        {
            Account.Type = AccountType.Phone;
            Account.PhoneNumber = _phoneInput.Text.Trim();
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}