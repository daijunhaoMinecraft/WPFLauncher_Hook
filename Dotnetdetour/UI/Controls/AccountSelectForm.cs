using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Mcl.Core.Dotnetdetour.HookList
{
    public class AccountSelectForm : Form
    {
        // ── Color Constants ──
        private static readonly Color Primary = Color.FromArgb(0, 110, 210);
        private static readonly Color PrimaryDark = Color.FromArgb(0, 90, 180);
        private static readonly Color PrimaryHover = Color.FromArgb(0, 80, 155);
        private static readonly Color BgColor = Color.FromArgb(245, 247, 250);
        private static readonly Color CardWhite = Color.White;
        private static readonly Color TextPrimary = Color.FromArgb(30, 30, 30);
        private static readonly Color TextSecondary = Color.FromArgb(100, 100, 100);
        private static readonly Color HeaderTextLight = Color.FromArgb(190, 215, 245);
        private static readonly Color BorderColor = Color.FromArgb(218, 220, 224);
        private static readonly Color AltRowColor = Color.FromArgb(240, 244, 250);
        private static readonly Color SuccessBtn = Color.FromArgb(52, 168, 83);
        private static readonly Color SuccessBtnHover = Color.FromArgb(40, 148, 72);
        private static readonly Color NeutralBtn = Color.FromArgb(140, 140, 140);
        private static readonly Color NeutralBtnHover = Color.FromArgb(120, 120, 120);
        private static readonly Color BtnDefault = Color.FromArgb(235, 237, 240);
        private static readonly Color BtnDefaultHover = Color.FromArgb(215, 218, 224);
        private static readonly Color BtnDefaultText = Color.FromArgb(60, 60, 60);
        private static readonly Color WhiteText = Color.White;

        // ── Font Constants ──
        private static readonly Font MainFont = new Font("Microsoft YaHei", 9F, FontStyle.Regular);
        private static readonly Font BoldFont = new Font("Microsoft YaHei", 9F, FontStyle.Bold);
        private static readonly Font HeaderFont = new Font("Microsoft YaHei", 13F, FontStyle.Bold);
        private static readonly Font CountFont = new Font("Microsoft YaHei", 8.5F, FontStyle.Regular);
        private static readonly Font DetailTitleFont = new Font("Microsoft YaHei", 9.5F, FontStyle.Bold);
        private static readonly Font DetailFont = new Font("Microsoft YaHei", 8F, FontStyle.Regular);

        // ── Controls ──
        private ListView accountListView;
        private Button loginButton;
        private Button addButton;
        private Button editButton;
        private Button deleteButton;
        private Button manualButton;
        private Button originalButton;
        private CheckBox topMostCheck;
        private Panel detailPanel;
        private Label detailTitle;
        private Label detailContent;
        private Panel headerPanel;
        private Label headerTitle;
        private Label accountCountLabel;

        private List<AccountInfo> _accounts;

        public AccountInfo SelectedAccount { get; private set; }
        public LoginAction Action { get; private set; }

        public enum LoginAction
        {
            UseSelected,
            UseOriginal,
            ManualInput,
            Exit
        }

        public AccountSelectForm()
        {
            _accounts = AccountManager.GetAllSorted();
            Action = LoginAction.Exit;
            InitializeComponent();
            RefreshAccountList();
        }

        // ────────────────────────────────────────────────────────
        //  Layout Initialization
        // ────────────────────────────────────────────────────────

        private void InitializeComponent()
        {
            this.Text = "Account Manager";
            this.Size = new Size(700, 500);
            this.BackColor = BgColor;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = MainFont;

            BuildHeader();
            BuildListView();
            BuildActionButtons();
            BuildSeparator();
            BuildDetailPanel();
            BuildTopMostCheck();

            editButton.Enabled = false;
            deleteButton.Enabled = false;
            loginButton.Enabled = false;

            UpdateAccountCount();
        }

        // ── Header ──

        private void BuildHeader()
        {
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = PrimaryDark
            };

            headerTitle = new Label
            {
                Text = "Account Manager",
                Location = new Point(18, 12),
                Size = new Size(240, 30),
                Font = HeaderFont,
                ForeColor = WhiteText,
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(headerTitle);

            accountCountLabel = new Label
            {
                Text = "",
                Location = new Point(260, 16),
                Size = new Size(220, 22),
                ForeColor = HeaderTextLight,
                Font = CountFont,
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(accountCountLabel);

            this.Controls.Add(headerPanel);
        }

        // ── ListView ──

        private void BuildListView()
        {
            accountListView = new ListView
            {
                Location = new Point(14, 62),
                Size = new Size(462, 268),
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false,
                GridLines = false,
                Font = MainFont,
                BorderStyle = BorderStyle.FixedSingle,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BackColor = CardWhite
            };

            accountListView.Columns.Add("Account Name", 160);
            accountListView.Columns.Add("Type", 70);
            accountListView.Columns.Add("Last Used", 145);
            accountListView.Columns.Add("Notes", 83);

            accountListView.SelectedIndexChanged += OnAccountSelected;
            accountListView.DoubleClick += OnAccountDoubleClick;
            this.Controls.Add(accountListView);
        }

        // ── Action Buttons (right column) ──

        private void BuildActionButtons()
        {
            const int btnLeft = 492;
            const int btnWidth = 186;
            const int btnHeight = 36;
            const int btnGap = 8;

            int y = 62;

            loginButton = CreateStyledButton("Login Selected", btnLeft, y, btnWidth, btnHeight,
                Primary, WhiteText, PrimaryHover);
            loginButton.Click += OnLoginClick;
            this.Controls.Add(loginButton);
            y += btnHeight + btnGap;

            addButton = CreateStyledButton("Add Account", btnLeft, y, btnWidth, btnHeight,
                BtnDefault, BtnDefaultText, BtnDefaultHover);
            addButton.Click += OnAddClick;
            this.Controls.Add(addButton);
            y += btnHeight + btnGap;

            editButton = CreateStyledButton("Edit Account", btnLeft, y, btnWidth, btnHeight,
                BtnDefault, BtnDefaultText, BtnDefaultHover);
            editButton.Click += OnEditClick;
            this.Controls.Add(editButton);
            y += btnHeight + btnGap;

            deleteButton = CreateStyledButton("Delete Account", btnLeft, y, btnWidth, btnHeight,
                BtnDefault, BtnDefaultText, BtnDefaultHover);
            deleteButton.Click += OnDeleteClick;
            this.Controls.Add(deleteButton);
            y += btnHeight + 4;

            // Separator after delete
            BuildSeparator();
            y += 10;

            manualButton = CreateStyledButton("Manual Input", btnLeft, y, btnWidth, btnHeight,
                SuccessBtn, WhiteText, SuccessBtnHover);
            manualButton.Click += OnManualClick;
            this.Controls.Add(manualButton);
            y += btnHeight + btnGap;

            originalButton = CreateStyledButton("Original Login", btnLeft, y, btnWidth, btnHeight,
                NeutralBtn, WhiteText, NeutralBtnHover);
            originalButton.Click += OnOriginalClick;
            this.Controls.Add(originalButton);
        }

        private void BuildSeparator()
        {
            var sep = new Label
            {
                Location = new Point(492, 238),
                Size = new Size(186, 1),
                BackColor = BorderColor,
                Text = ""
            };
            this.Controls.Add(sep);
        }

        // ── Detail Panel ──

        private void BuildDetailPanel()
        {
            detailPanel = new Panel
            {
                Location = new Point(14, 340),
                Size = new Size(656, 82),
                BackColor = CardWhite,
                BorderStyle = BorderStyle.None
            };
            detailPanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, detailPanel.ClientRectangle,
                    BorderColor, ButtonBorderStyle.Solid);
            };

            detailTitle = new Label
            {
                Location = new Point(12, 8),
                Size = new Size(630, 20),
                Font = DetailTitleFont,
                ForeColor = Primary,
                Text = "Select an account to view details"
            };
            detailPanel.Controls.Add(detailTitle);

            detailContent = new Label
            {
                Location = new Point(12, 32),
                Size = new Size(630, 42),
                Font = DetailFont,
                ForeColor = TextSecondary
            };
            detailPanel.Controls.Add(detailContent);

            this.Controls.Add(detailPanel);
        }

        // ── TopMost Checkbox ──

        private void BuildTopMostCheck()
        {
            topMostCheck = new CheckBox
            {
                Text = "Keep on Top",
                Size = new Size(100, 22),
                Checked = WpfConfig.IsWindowTopMost,
                Location = new Point(568, 428),
                ForeColor = TextSecondary,
                Font = CountFont
            };
            topMostCheck.CheckedChanged += (s, e) => this.TopMost = topMostCheck.Checked;
            this.Controls.Add(topMostCheck);

            this.TopMost = WpfConfig.IsWindowTopMost;
        }

        // ── Button Factory ──

        private Button CreateStyledButton(
            string text, int x, int y, int w, int h,
            Color back, Color fore, Color hover)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Regular),
                BackColor = back,
                ForeColor = fore
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = hover;
            btn.MouseLeave += (s, e) => btn.BackColor = back;
            return btn;
        }

        // ────────────────────────────────────────────────────────
        //  Account List Logic
        // ────────────────────────────────────────────────────────

        private void RefreshAccountList()
        {
            _accounts = AccountManager.GetAllSorted();
            accountListView.Items.Clear();

            bool isEven = true;
            foreach (var acc in _accounts)
            {
                var item = new ListViewItem(acc.Name);
                item.SubItems.Add(acc.TypeDisplay);
                item.SubItems.Add(acc.LastUsedDisplay);
                item.SubItems.Add(acc.Notes ?? "");
                item.Tag = acc;
                item.BackColor = isEven ? CardWhite : AltRowColor;
                accountListView.Items.Add(item);
                isEven = !isEven;
            }

            UpdateAccountCount();
        }

        private void UpdateAccountCount()
        {
            accountCountLabel.Text = _accounts.Count > 0
                ? $"共 {_accounts.Count} 个账号"
                : "No saved accounts — add one or use manual login";
        }

        // ────────────────────────────────────────────────────────
        //  Selection & Detail
        // ────────────────────────────────────────────────────────

        private void OnAccountSelected(object sender, EventArgs e)
        {
            bool hasSelection = accountListView.SelectedItems.Count > 0;
            editButton.Enabled = hasSelection;
            deleteButton.Enabled = hasSelection;
            loginButton.Enabled = hasSelection;

            if (hasSelection)
            {
                var acc = (AccountInfo)accountListView.SelectedItems[0].Tag;
                UpdateDetailPanel(acc);
            }
            else
            {
                detailTitle.Text = "Select an account to view details";
                detailContent.Text = "";
            }
        }

        private void UpdateDetailPanel(AccountInfo acc)
        {
            detailTitle.Text = $"{acc.Name}  [{acc.TypeDisplay}]";

            string info;
            if (acc.Type == AccountType.Cookie)
            {
                string preview = string.IsNullOrEmpty(acc.CookieData)
                    ? "(empty)"
                    : (acc.CookieData.Length > 80 ? acc.CookieData.Substring(0, 80) + "..." : acc.CookieData);
                info = $"Cookie: {preview}";
            }
            else if (acc.Type == AccountType.Phone)
            {
                string hasCache = string.IsNullOrEmpty(acc.CookieData) ? "No" : "Yes";
                info = $"Phone: {acc.PhoneNumber ?? "(empty)"}    Cached Credentials: {hasCache}";
            }
            else
            {
                info = $"Username: {acc.Username ?? "(empty)"}    Password: {(string.IsNullOrEmpty(acc.Password) ? "(empty)" : "******")}";
            }

            if (!string.IsNullOrEmpty(acc.Notes))
            {
                info += $"\nNotes: {acc.Notes}";
            }

            detailContent.Text = info;
        }

        // ────────────────────────────────────────────────────────
        //  Button Handlers
        // ────────────────────────────────────────────────────────

        private void OnAccountDoubleClick(object sender, EventArgs e)
        {
            if (accountListView.SelectedItems.Count > 0)
                PerformLogin();
        }

        private void OnLoginClick(object sender, EventArgs e)
        {
            PerformLogin();
        }

        private void PerformLogin()
        {
            var acc = (AccountInfo)accountListView.SelectedItems[0].Tag;
            SelectedAccount = acc;
            Action = LoginAction.UseSelected;
            AccountManager.MarkUsed(acc);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            var dialog = new AccountEditDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                AccountManager.Add(dialog.Account);
                RefreshAccountList();
                SelectAccountByName(dialog.Account.Name);
            }
        }

        private void OnEditClick(object sender, EventArgs e)
        {
            if (accountListView.SelectedItems.Count == 0) return;
            var acc = (AccountInfo)accountListView.SelectedItems[0].Tag;
            var dialog = new AccountEditDialog(acc);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                AccountManager.Update(dialog.OriginalName, dialog.Account);
                RefreshAccountList();
                SelectAccountByName(dialog.Account.Name);
            }
        }

        private void OnDeleteClick(object sender, EventArgs e)
        {
            if (accountListView.SelectedItems.Count == 0) return;
            var acc = (AccountInfo)accountListView.SelectedItems[0].Tag;
            var result = MessageBox.Show(
                $"Are you sure you want to delete \"{acc.Name}\"?\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
            {
                AccountManager.Delete(acc.Name);
                RefreshAccountList();
            }
        }

        private void OnManualClick(object sender, EventArgs e)
        {
            Action = LoginAction.ManualInput;
            this.DialogResult = DialogResult.None;
            this.Close();
        }

        private void OnOriginalClick(object sender, EventArgs e)
        {
            Action = LoginAction.UseOriginal;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ── Helpers ──

        private void SelectAccountByName(string name)
        {
            foreach (ListViewItem item in accountListView.Items)
            {
                if (item.Text == name)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (this.DialogResult == DialogResult.None)
            {
                Action = LoginAction.UseOriginal;
                this.DialogResult = DialogResult.Abort;
            }
            base.OnFormClosed(e);
        }
    }
}
