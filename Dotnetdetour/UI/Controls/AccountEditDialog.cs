using System;
using System.Drawing;
using System.Windows.Forms;

namespace Mcl.Core.Dotnetdetour.HookList
{
    public class AccountEditDialog : Form
    {
        // ── Color Constants ──
        private static readonly Color Primary = Color.FromArgb(0, 110, 210);
        private static readonly Color PrimaryHover = Color.FromArgb(0, 80, 155);
        private static readonly Color BgColor = Color.FromArgb(245, 247, 250);
        private static readonly Color CardWhite = Color.White;
        private static readonly Color TextPrimary = Color.FromArgb(30, 30, 30);
        private static readonly Color TextSecondary = Color.FromArgb(100, 100, 100);
        private static readonly Color BorderColor = Color.FromArgb(218, 220, 224);
        private static readonly Color ErrorColor = Color.FromArgb(209, 52, 56);
        private static readonly Color BtnDefault = Color.FromArgb(235, 237, 240);
        private static readonly Color BtnDefaultHover = Color.FromArgb(215, 218, 224);
        private static readonly Color BtnDefaultText = Color.FromArgb(60, 60, 60);
        private static readonly Color WhiteText = Color.White;
        private static readonly Color SeparatorColor = Color.FromArgb(225, 227, 230);
        private static readonly Color ToggleCheckedBg = Color.FromArgb(0, 110, 210);
        private static readonly Color ToggleCheckedText = Color.White;
        private static readonly Color ToggleUncheckedBg = Color.FromArgb(235, 237, 240);
        private static readonly Color ToggleUncheckedText = Color.FromArgb(100, 100, 100);

        // ── Font Constants ──
        private static readonly Font MainFont = new Font("Microsoft YaHei", 9F, FontStyle.Regular);
        private static readonly Font BoldFont = new Font("Microsoft YaHei", 9F, FontStyle.Bold);
        private static readonly Font HeaderFont = new Font("Microsoft YaHei", 11F, FontStyle.Bold);
        private static readonly Font LabelFont = new Font("Microsoft YaHei", 9F, FontStyle.Regular);

        // ── Header ──
        private Panel headerPanel;
        private Label headerTitle;

        // ── Form Fields ──
        private TextBox nameTextBox;
        private RadioButton cookieRadio;
        private RadioButton radio4399;
        private RadioButton phoneRadio;
        private TextBox cookieTextBox;
        private TextBox usernameTextBox;
        private TextBox passwordTextBox;
        private TextBox phoneTextBox;
        private TextBox notesTextBox;
        private Panel cookiePanel;
        private Panel panel4399;
        private Panel phonePanel;

        private Label errorLabel;
        private Button saveButton;
        private Button cancelButton;

        public AccountInfo Account { get; private set; }
        public bool IsEditMode { get; set; }
        public string OriginalName { get; set; }

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

        private void InitializeComponent()
        {
            this.Text = IsEditMode ? "编辑账号" : "添加账号";
            this.Size = new Size(500, 480);
            this.BackColor = BgColor;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = MainFont;

            // ══════════════════════════════════════════
            //  Header
            // ══════════════════════════════════════════
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = CardWhite
            };

            headerTitle = new Label
            {
                Text = IsEditMode ? "编辑账号信息" : "添加新账号",
                Location = new Point(20, 12),
                Size = new Size(300, 24),
                Font = HeaderFont,
                ForeColor = Primary
            };
            headerPanel.Controls.Add(headerTitle);

            var headerSep = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = SeparatorColor,
                Text = ""
            };
            headerPanel.Controls.Add(headerSep);

            this.Controls.Add(headerPanel);

            // ══════════════════════════════════════════
            //  Form Content
            // ══════════════════════════════════════════
            int inputLeft = 100;
            int inputWidth = 380;
            int rowGap = 12;

            int y = 58;

            // 1. Account Name
            AddLabel("账号名称", 20, y + 5, 75, 20);
            nameTextBox = AddTextBox(inputLeft, y, inputWidth, 28, 50);
            y += 28 + rowGap;

            // 2. Account Type — toggle-button radio group
            AddLabel("账号类型", 20, y + 5, 75, 20);

            cookieRadio = CreateToggleRadio("Cookie 登录", inputLeft, y, 110, 28);
            radio4399 = CreateToggleRadio("4399 登录", inputLeft + 120, y, 110, 28);
            phoneRadio = CreateToggleRadio("手机号登录", inputLeft + 240, y, 115, 28);

            cookieRadio.Checked = Account.Type == AccountType.Cookie;
            radio4399.Checked = Account.Type == AccountType._4399;
            phoneRadio.Checked = Account.Type == AccountType.Phone;

            cookieRadio.CheckedChanged += OnTypeChanged;
            radio4399.CheckedChanged += OnTypeChanged;
            phoneRadio.CheckedChanged += OnTypeChanged;

            y += 28 + rowGap;

            // 3. Type-specific content panels (all at same position, only one visible)
            int panelHeight = 148;

            // ── Cookie Panel ──
            cookiePanel = new Panel
            {
                Location = new Point(inputLeft, y),
                Size = new Size(inputWidth, panelHeight),
                Visible = Account.Type == AccountType.Cookie
            };

            var cookieHint = new Label
            {
                Text = "Cookie / Sauth JSON 数据",
                Location = new Point(0, 0),
                Size = new Size(inputWidth, 20),
                Font = BoldFont,
                ForeColor = TextSecondary
            };
            cookiePanel.Controls.Add(cookieHint);

            cookieTextBox = new TextBox
            {
                Location = new Point(0, 24),
                Size = new Size(inputWidth, 120),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = MainFont
            };
            cookiePanel.Controls.Add(cookieTextBox);
            this.Controls.Add(cookiePanel);

            // ── 4399 Panel ──
            panel4399 = new Panel
            {
                Location = new Point(inputLeft, y),
                Size = new Size(inputWidth, panelHeight),
                Visible = Account.Type == AccountType._4399
            };

            var usernameLabel = new Label
            {
                Text = "用户名",
                Location = new Point(0, 6),
                Size = new Size(55, 24),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = TextSecondary
            };
            panel4399.Controls.Add(usernameLabel);

            usernameTextBox = new TextBox
            {
                Location = new Point(60, 6),
                Size = new Size(inputWidth - 60, 24),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel4399.Controls.Add(usernameTextBox);

            var passwordLabel = new Label
            {
                Text = "密码",
                Location = new Point(0, 40),
                Size = new Size(55, 24),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = TextSecondary
            };
            panel4399.Controls.Add(passwordLabel);

            passwordTextBox = new TextBox
            {
                Location = new Point(60, 40),
                Size = new Size(inputWidth - 60, 24),
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel4399.Controls.Add(passwordTextBox);
            this.Controls.Add(panel4399);

            // ── Phone Panel ──
            phonePanel = new Panel
            {
                Location = new Point(inputLeft, y),
                Size = new Size(inputWidth, panelHeight),
                Visible = Account.Type == AccountType.Phone
            };

            var phoneLabel = new Label
            {
                Text = "手机号",
                Location = new Point(0, 6),
                Size = new Size(55, 24),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = TextSecondary
            };
            phonePanel.Controls.Add(phoneLabel);

            phoneTextBox = new TextBox
            {
                Location = new Point(60, 6),
                Size = new Size(inputWidth - 60, 24),
                MaxLength = 11,
                BorderStyle = BorderStyle.FixedSingle
            };
            phoneTextBox.TextChanged += (s, ev) =>
            {
                string filtered = "";
                foreach (char c in phoneTextBox.Text)
                {
                    if (char.IsDigit(c)) filtered += c;
                }
                if (filtered != phoneTextBox.Text)
                {
                    phoneTextBox.Text = filtered;
                    phoneTextBox.SelectionStart = filtered.Length;
                }
            };
            phonePanel.Controls.Add(phoneTextBox);
            this.Controls.Add(phonePanel);

            y += panelHeight + rowGap;

            // 4. Notes
            AddLabel("备注", 20, y + 5, 75, 20);
            notesTextBox = new TextBox
            {
                Location = new Point(inputLeft, y),
                Size = new Size(inputWidth, 56),
                Multiline = true,
                MaxLength = 200,
                BorderStyle = BorderStyle.FixedSingle,
                Font = MainFont
            };
            this.Controls.Add(notesTextBox);

            y += 56 + rowGap;

            // 5. Error label
            errorLabel = new Label
            {
                Location = new Point(inputLeft, y),
                Size = new Size(inputWidth, 20),
                ForeColor = ErrorColor,
                Font = new Font("Microsoft YaHei", 8F, FontStyle.Regular)
            };
            this.Controls.Add(errorLabel);

            y += 20 + rowGap;

            // 6. Buttons — Save primary, Cancel ghost
            cancelButton = CreateButton("取消", inputLeft + inputWidth - 85, y, 85, 32,
                BtnDefault, BtnDefaultText);
            cancelButton.Click += (s, ev) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            cancelButton.MouseEnter += (s, ev) => { cancelButton.BackColor = BtnDefaultHover; };
            cancelButton.MouseLeave += (s, ev) => { cancelButton.BackColor = BtnDefault; };
            this.Controls.Add(cancelButton);

            saveButton = CreateButton("保存", inputLeft + inputWidth - 85 - 10 - 85, y, 85, 32,
                Primary, WhiteText);
            saveButton.Font = BoldFont;
            saveButton.Click += OnSaveClick;
            saveButton.MouseEnter += (s, ev) => { saveButton.BackColor = PrimaryHover; };
            saveButton.MouseLeave += (s, ev) => { saveButton.BackColor = Primary; };
            this.Controls.Add(saveButton);

            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
        }

        private void AddLabel(string text, int x, int y, int w, int h)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = TextSecondary
            };
            this.Controls.Add(lbl);
        }

        private TextBox AddTextBox(int x, int y, int w, int h, int maxLen = 0)
        {
            var tb = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BorderStyle = BorderStyle.FixedSingle
            };
            if (maxLen > 0) tb.MaxLength = maxLen;
            this.Controls.Add(tb);
            return tb;
        }

        private RadioButton CreateToggleRadio(string text, int x, int y, int w, int h)
        {
            var rb = new RadioButton
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                Appearance = Appearance.Button,
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                Font = MainFont,
                ForeColor = ToggleUncheckedText,
                BackColor = ToggleUncheckedBg
            };
            rb.FlatAppearance.BorderSize = 0;
            rb.FlatAppearance.CheckedBackColor = ToggleCheckedBg;
            rb.CheckedChanged += (s, ev) =>
            {
                if (rb.Checked)
                {
                    rb.BackColor = ToggleCheckedBg;
                    rb.ForeColor = ToggleCheckedText;
                }
                else
                {
                    rb.BackColor = ToggleUncheckedBg;
                    rb.ForeColor = ToggleUncheckedText;
                }
            };
            this.Controls.Add(rb);
            return rb;
        }

        private Button CreateButton(string text, int x, int y, int w, int h, Color back, Color fore)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                Font = MainFont,
                BackColor = back,
                ForeColor = fore
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadAccountData()
        {
            nameTextBox.Text = Account.Name ?? "";
            cookieRadio.Checked = Account.Type == AccountType.Cookie;
            radio4399.Checked = Account.Type == AccountType._4399;
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
                panel4399.Visible = false;
                phonePanel.Visible = false;
            }
            else if (radio4399.Checked)
            {
                Account.Type = AccountType._4399;
                cookiePanel.Visible = false;
                panel4399.Visible = true;
                phonePanel.Visible = false;
            }
            else if (phoneRadio.Checked)
            {
                Account.Type = AccountType.Phone;
                cookiePanel.Visible = false;
                panel4399.Visible = false;
                phonePanel.Visible = true;
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            errorLabel.Text = "";

            string name = nameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                errorLabel.Text = "请输入账号名称";
                nameTextBox.Focus();
                return;
            }

            if (!IsEditMode || name != OriginalName)
            {
                if (AccountManager.FindByName(name) != null)
                {
                    errorLabel.Text = "账号名称已存在，请使用其他名称";
                    nameTextBox.Focus();
                    return;
                }
            }

            if (cookieRadio.Checked)
            {
                if (string.IsNullOrEmpty(cookieTextBox.Text.Trim()))
                {
                    errorLabel.Text = "请输入 Cookie 数据";
                    cookieTextBox.Focus();
                    return;
                }
                Account.CookieData = cookieTextBox.Text.Trim();
                Account.Username = null;
                Account.Password = null;
                Account.PhoneNumber = null;
            }
            else if (radio4399.Checked)
            {
                if (string.IsNullOrEmpty(usernameTextBox.Text.Trim()))
                {
                    errorLabel.Text = "请输入 4399 用户名";
                    usernameTextBox.Focus();
                    return;
                }
                if (string.IsNullOrEmpty(passwordTextBox.Text.Trim()))
                {
                    errorLabel.Text = "请输入 4399 密码";
                    passwordTextBox.Focus();
                    return;
                }
                Account.Username = usernameTextBox.Text.Trim();
                Account.Password = passwordTextBox.Text.Trim();
                Account.CookieData = null;
                Account.PhoneNumber = null;
            }
            else
            {
                string phone = phoneTextBox.Text.Trim();
                if (string.IsNullOrEmpty(phone))
                {
                    errorLabel.Text = "请输入手机号";
                    phoneTextBox.Focus();
                    return;
                }
                if (phone.Length != 11)
                {
                    errorLabel.Text = "请输入正确的 11 位手机号";
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

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
