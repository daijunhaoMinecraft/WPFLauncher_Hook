using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

public class AccountEditDialog : Form
{
    private readonly Font BoldFont = new("Microsoft YaHei", 9F, FontStyle.Bold);
    private readonly Font MainFont = new("Microsoft YaHei", 9F, FontStyle.Regular);

    private readonly Color ThemeColor = Color.FromArgb(0, 120, 215);
    private Panel _4399Panel;
    private RadioButton _4399Radio;
    private Button cancelButton;
    private Label cookieLabel;
    private Panel cookiePanel;
    private RadioButton cookieRadio;
    private TextBox cookieTextBox;
    private Label errorLabel;
    private Label nameLabel;
    private TextBox nameTextBox;
    private Label notesLabel;
    private TextBox notesTextBox;
    private Label passwordLabel;
    private TextBox passwordTextBox;
    private Label phoneLabel;
    private Panel phonePanel;
    private RadioButton phoneRadio;
    private TextBox phoneTextBox;
    private Button saveButton;
    private Label typeLabel;
    private Label usernameLabel;
    private TextBox usernameTextBox;

    public AccountEditDialog(AccountInfo account = null)
    {
        if (account != null)
        {
            IsEditMode = true;
            OriginalName = account.Name;
            Account = new AccountInfo
            {
                Name = account.Name,
                Type = account.Type,
                CookieData = account.CookieData,
                Username = account.Username,
                Password = account.Password,
                PhoneNumber = account.PhoneNumber,
                DeviceId = account.DeviceId,
                Notes = account.Notes,
                CreatedAt = account.CreatedAt,
                LastUsed = account.LastUsed
            };
        }
        else
        {
            Account = new AccountInfo { Type = AccountType.Cookie };
        }

        InitializeComponent();
        LoadAccountData();
    }

    public AccountInfo Account { get; }
    public bool IsEditMode { get; set; }
    public string OriginalName { get; set; }

    private void InitializeComponent()
    {
        Text = IsEditMode ? "编辑账号" : "添加账号";
        Size = new Size(480, 460);
        BackColor = Color.FromArgb(243, 243, 243);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = MainFont;

        var y = 15;
        var labelWidth = 70;
        var inputLeft = 90;
        var inputWidth = 360;

        // Name
        nameLabel = new Label
        {
            Text = "账号名称:",
            Location = new Point(15, y + 3),
            Size = new Size(labelWidth, 20),
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(nameLabel);

        nameTextBox = new TextBox
        {
            Location = new Point(inputLeft, y),
            Size = new Size(inputWidth, 23),
            MaxLength = 50
        };
        Controls.Add(nameTextBox);

        y += 35;

        // Type
        typeLabel = new Label
        {
            Text = "账号类型:",
            Location = new Point(15, y + 3),
            Size = new Size(labelWidth, 20),
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(typeLabel);

        cookieRadio = new RadioButton
        {
            Text = "Cookie 登录",
            Location = new Point(inputLeft, y),
            Size = new Size(100, 20),
            Checked = Account.Type == AccountType.Cookie
        };
        Controls.Add(cookieRadio);

        _4399Radio = new RadioButton
        {
            Text = "4399 登录",
            Location = new Point(inputLeft + 110, y),
            Size = new Size(100, 20),
            Checked = Account.Type == AccountType._4399
        };
        Controls.Add(_4399Radio);

        phoneRadio = new RadioButton
        {
            Text = "手机号登录",
            Location = new Point(inputLeft + 220, y),
            Size = new Size(120, 20),
            Checked = Account.Type == AccountType.Phone
        };
        Controls.Add(phoneRadio);

        cookieRadio.CheckedChanged += OnTypeChanged;
        _4399Radio.CheckedChanged += OnTypeChanged;
        phoneRadio.CheckedChanged += OnTypeChanged;

        y += 35;

        // Cookie panel
        cookiePanel = new Panel
        {
            Location = new Point(inputLeft, y),
            Size = new Size(inputWidth, 130),
            Visible = Account.Type == AccountType.Cookie
        };

        cookieLabel = new Label
        {
            Text = "Cookie / Sauth JSON 数据:",
            Location = new Point(0, 0),
            Size = new Size(inputWidth, 18),
            Font = BoldFont
        };
        cookiePanel.Controls.Add(cookieLabel);

        cookieTextBox = new TextBox
        {
            Location = new Point(0, 22),
            Size = new Size(inputWidth, 100),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true
        };
        cookiePanel.Controls.Add(cookieTextBox);

        Controls.Add(cookiePanel);

        // 4399 panel
        _4399Panel = new Panel
        {
            Location = new Point(inputLeft, y),
            Size = new Size(inputWidth, 120),
            Visible = Account.Type == AccountType._4399
        };

        usernameLabel = new Label
        {
            Text = "用户名:",
            Location = new Point(0, 0),
            Size = new Size(60, 20),
            TextAlign = ContentAlignment.MiddleRight
        };
        _4399Panel.Controls.Add(usernameLabel);

        usernameTextBox = new TextBox
        {
            Location = new Point(65, 0),
            Size = new Size(inputWidth - 65, 23)
        };
        _4399Panel.Controls.Add(usernameTextBox);

        passwordLabel = new Label
        {
            Text = "密码:",
            Location = new Point(0, 30),
            Size = new Size(60, 20),
            TextAlign = ContentAlignment.MiddleRight
        };
        _4399Panel.Controls.Add(passwordLabel);

        passwordTextBox = new TextBox
        {
            Location = new Point(65, 30),
            Size = new Size(inputWidth - 65, 23),
            UseSystemPasswordChar = true
        };
        _4399Panel.Controls.Add(passwordTextBox);

        Controls.Add(_4399Panel);

        // Phone panel
        phonePanel = new Panel
        {
            Location = new Point(inputLeft, y),
            Size = new Size(inputWidth, 55),
            Visible = Account.Type == AccountType.Phone
        };

        phoneLabel = new Label
        {
            Text = "手机号:",
            Location = new Point(0, 0),
            Size = new Size(60, 20),
            TextAlign = ContentAlignment.MiddleRight
        };
        phonePanel.Controls.Add(phoneLabel);

        phoneTextBox = new TextBox
        {
            Location = new Point(65, 0),
            Size = new Size(inputWidth - 65, 23),
            MaxLength = 11
        };
        phoneTextBox.TextChanged += (s, ev) =>
        {
            // 只允许数字输入
            var filtered = "";
            foreach (var c in phoneTextBox.Text)
                if (char.IsDigit(c))
                    filtered += c;
            if (filtered != phoneTextBox.Text)
            {
                phoneTextBox.Text = filtered;
                phoneTextBox.SelectionStart = filtered.Length;
            }
        };
        phonePanel.Controls.Add(phoneTextBox);

        Controls.Add(phonePanel);

        y += Account.Type == AccountType.Cookie ? 140 : Account.Type == AccountType.Phone ? 65 : 130;

        // Notes
        notesLabel = new Label
        {
            Text = "备注:",
            Location = new Point(15, y + 3),
            Size = new Size(labelWidth, 20),
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(notesLabel);

        notesTextBox = new TextBox
        {
            Location = new Point(inputLeft, y),
            Size = new Size(inputWidth, 50),
            Multiline = true,
            MaxLength = 200
        };
        Controls.Add(notesTextBox);

        y += 60;

        // Error label
        errorLabel = new Label
        {
            Location = new Point(inputLeft, y),
            Size = new Size(inputWidth, 20),
            ForeColor = Color.FromArgb(209, 52, 56),
            Font = new Font("Microsoft YaHei", 8F, FontStyle.Regular)
        };
        Controls.Add(errorLabel);

        y += 30;

        // Buttons
        saveButton = new Button
        {
            Text = "保存",
            Location = new Point(inputLeft + inputWidth - 160, y),
            Size = new Size(75, 30),
            BackColor = ThemeColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = BoldFont
        };
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.Click += OnSaveClick;
        Controls.Add(saveButton);

        cancelButton = new Button
        {
            Text = "取消",
            Location = new Point(inputLeft + inputWidth - 75, y),
            Size = new Size(75, 30),
            BackColor = Color.FromArgb(200, 200, 200),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat
        };
        cancelButton.FlatAppearance.BorderSize = 0;
        cancelButton.Click += (s, e) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        Controls.Add(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void LoadAccountData()
    {
        nameTextBox.Text = Account.Name ?? "";
        cookieRadio.Checked = Account.Type == AccountType.Cookie;
        _4399Radio.Checked = Account.Type == AccountType._4399;
        phoneRadio.Checked = Account.Type == AccountType.Phone;
        cookieTextBox.Text = Account.CookieData ?? "";
        usernameTextBox.Text = Account.Username ?? "";
        passwordTextBox.Text = Account.Password ?? "";
        phoneTextBox.Text = Account.PhoneNumber ?? "";
        notesTextBox.Text = Account.Notes ?? "";
    }

    private void OnTypeChanged(object sender, EventArgs e)
    {
        if (cookieRadio.Checked)
        {
            Account.Type = AccountType.Cookie;
            cookiePanel.Visible = true;
            _4399Panel.Visible = false;
            phonePanel.Visible = false;
        }

        if (_4399Radio.Checked)
        {
            Account.Type = AccountType._4399;
            cookiePanel.Visible = false;
            _4399Panel.Visible = true;
            phonePanel.Visible = false;
        }

        if (phoneRadio.Checked)
        {
            Account.Type = AccountType.Phone;
            cookiePanel.Visible = false;
            _4399Panel.Visible = false;
            phonePanel.Visible = true;
        }
    }

    private void OnSaveClick(object sender, EventArgs e)
    {
        errorLabel.Text = "";
        errorLabel.ForeColor = Color.FromArgb(209, 52, 56);

        var name = nameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            errorLabel.Text = "请输入账号名称";
            nameTextBox.Focus();
            return;
        }

        // Check for duplicate name (skip if editing same account)
        if (!IsEditMode || name != OriginalName)
            if (AccountManager.FindByName(name) != null)
            {
                errorLabel.Text = "账号名称已存在，请使用其他名称";
                nameTextBox.Focus();
                return;
            }

        if (cookieRadio.Checked)
        {
            if (string.IsNullOrEmpty(cookieTextBox.Text.Trim()))
            {
                errorLabel.Text = "请输入Cookie数据";
                cookieTextBox.Focus();
                return;
            }

            Account.CookieData = cookieTextBox.Text.Trim();
            Account.Username = null;
            Account.Password = null;
            Account.PhoneNumber = null;
        }
        else if (_4399Radio.Checked)
        {
            if (string.IsNullOrEmpty(usernameTextBox.Text.Trim()))
            {
                errorLabel.Text = "请输入4399用户名";
                usernameTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(passwordTextBox.Text.Trim()))
            {
                errorLabel.Text = "请输入4399密码";
                passwordTextBox.Focus();
                return;
            }

            Account.Username = usernameTextBox.Text.Trim();
            Account.Password = passwordTextBox.Text.Trim();
            Account.CookieData = null;
            Account.PhoneNumber = null;
        }
        else // Phone
        {
            var phone = phoneTextBox.Text.Trim();
            if (string.IsNullOrEmpty(phone))
            {
                errorLabel.Text = "请输入手机号";
                phoneTextBox.Focus();
                return;
            }

            if (phone.Length != 11)
            {
                errorLabel.Text = "请输入正确的11位手机号";
                phoneTextBox.Focus();
                return;
            }

            Account.PhoneNumber = phone;
            Account.Username = null;
            Account.Password = null;
            Account.CookieData = null;
        }

        Account.Name = name;
        Account.Notes = notesTextBox.Text.Trim();

        DialogResult = DialogResult.OK;
        Close();
    }
}