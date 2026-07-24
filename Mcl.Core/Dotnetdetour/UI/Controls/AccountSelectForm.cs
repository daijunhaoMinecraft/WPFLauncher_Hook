using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.UI.Controls;

public class AccountSelectForm : Form
{
    public enum LoginAction
    {
        UseSelected,
        UseOriginal,
        ManualInput,
        Exit
    }

    private readonly Color BgColor = Color.FromArgb(243, 243, 243);
    private readonly Font BoldFont = new("Microsoft YaHei", 9F, FontStyle.Bold);
    private readonly Font MainFont = new("Microsoft YaHei", 9F, FontStyle.Regular);

    private readonly Color ThemeColor = Color.FromArgb(0, 120, 215);
    private readonly Font TitleFont = new("Microsoft YaHei", 11F, FontStyle.Bold);

    private List<AccountInfo> _accounts;
    private Label accountCountLabel;
    private ListView accountListView;
    private Button addButton;
    private Button deleteButton;
    private Label detailContent;
    private Panel detailPanel;
    private Label detailTitle;
    private Button editButton;
    private Panel headerPanel;
    private Label headerTitle;
    private Button loginButton;
    private Button manualButton;
    private Button originalButton;
    private CheckBox topMostCheck;

    public AccountSelectForm()
    {
        _accounts = AccountManager.GetAllSorted();
        Action = LoginAction.Exit;
        InitializeComponent();
        RefreshAccountList();
    }

    public AccountInfo SelectedAccount { get; private set; }
    public LoginAction Action { get; private set; }

    private void InitializeComponent()
    {
        Text = "多账号管理器";
        Size = new Size(680, 480);
        BackColor = BgColor;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = MainFont;

        // Header panel
        headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.White
        };

        headerTitle = new Label
        {
            Text = "选择登录账号",
            Location = new Point(16, 12),
            Size = new Size(200, 24),
            Font = TitleFont,
            ForeColor = ThemeColor
        };
        headerPanel.Controls.Add(headerTitle);

        accountCountLabel = new Label
        {
            Text = "",
            Location = new Point(220, 14),
            Size = new Size(200, 20),
            ForeColor = Color.Gray,
            Font = new Font("Microsoft YaHei", 8F)
        };
        headerPanel.Controls.Add(accountCountLabel);

        Controls.Add(headerPanel);

        // TopMost checkbox
        topMostCheck = new CheckBox
        {
            Text = "置顶",
            Size = new Size(55, 20),
            Checked = WpfConfig.IsWindowTopMost,
            Location = new Point(ClientSize.Width - 70, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        topMostCheck.CheckedChanged += (s, e) => TopMost = topMostCheck.Checked;
        headerPanel.Controls.Add(topMostCheck);

        TopMost = WpfConfig.IsWindowTopMost;

        // ListView
        accountListView = new ListView
        {
            Location = new Point(12, 56),
            Size = new Size(420, 300),
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            GridLines = true,
            Font = MainFont,
            BorderStyle = BorderStyle.FixedSingle,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };

        accountListView.Columns.Add("账号名称", 140);
        accountListView.Columns.Add("类型", 60);
        accountListView.Columns.Add("最后使用", 140);
        accountListView.Columns.Add("备注", 76);

        accountListView.SelectedIndexChanged += OnAccountSelected;
        accountListView.DoubleClick += OnAccountDoubleClick;
        Controls.Add(accountListView);

        // Button panel (right side)
        var btnLeft = 445;
        var btnWidth = 100;
        var btnHeight = 34;

        loginButton = CreateButton("登录选中账号", btnLeft, 56, btnWidth, btnHeight, true);
        loginButton.Click += OnLoginClick;
        Controls.Add(loginButton);

        addButton = CreateButton("添加账号", btnLeft, 98, btnWidth, btnHeight, false);
        addButton.Click += OnAddClick;
        Controls.Add(addButton);

        editButton = CreateButton("编辑账号", btnLeft, 182, btnWidth, btnHeight, false);
        editButton.Click += OnEditClick;
        Controls.Add(editButton);

        deleteButton = CreateButton("删除账号", btnLeft, 224, btnWidth, btnHeight, false);
        deleteButton.Click += OnDeleteClick;
        Controls.Add(deleteButton);

        // Separator
        var sep = new Label
        {
            Location = new Point(btnLeft, 266),
            Size = new Size(btnWidth, 2),
            BackColor = Color.FromArgb(220, 220, 220),
            Text = ""
        };
        Controls.Add(sep);

        manualButton = CreateButton("手动输入(一次性)", btnLeft, 276, btnWidth, btnHeight, false);
        manualButton.BackColor = Color.FromArgb(100, 180, 100);
        manualButton.Click += OnManualClick;
        Controls.Add(manualButton);

        originalButton = CreateButton("使用原号登录", btnLeft, 318, btnWidth, btnHeight, false);
        originalButton.BackColor = Color.FromArgb(180, 180, 180);
        originalButton.Click += OnOriginalClick;
        Controls.Add(originalButton);

        // Detail panel
        detailPanel = new Panel
        {
            Location = new Point(12, 364),
            Size = new Size(533, 70),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        detailTitle = new Label
        {
            Location = new Point(10, 6),
            Size = new Size(510, 18),
            Font = BoldFont,
            ForeColor = ThemeColor,
            Text = "选择一个账号查看详情"
        };
        detailPanel.Controls.Add(detailTitle);

        detailContent = new Label
        {
            Location = new Point(10, 28),
            Size = new Size(510, 36),
            Font = new Font("Microsoft YaHei", 8F),
            ForeColor = Color.FromArgb(80, 80, 80),
            Text = ""
        };
        detailPanel.Controls.Add(detailContent);

        Controls.Add(detailPanel);

        // Initially disable edit/delete/login
        editButton.Enabled = false;
        deleteButton.Enabled = false;
        loginButton.Enabled = false;

        UpdateAccountCount();
    }

    private Button CreateButton(string text, int x, int y, int width, int height, bool isPrimary)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Regular),
            BackColor = isPrimary ? ThemeColor : Color.FromArgb(230, 230, 230),
            ForeColor = isPrimary ? Color.White : Color.Black
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (s, e) =>
        {
            if (isPrimary)
                btn.BackColor = Color.FromArgb(0, 100, 190);
            else
                btn.BackColor = Color.FromArgb(210, 210, 210);
        };
        btn.MouseLeave += (s, e) =>
        {
            btn.BackColor = isPrimary ? ThemeColor : Color.FromArgb(230, 230, 230);
            if (text == "手动输入(一次性)")
                btn.BackColor = Color.FromArgb(100, 180, 100);
            if (text == "使用原号登录")
                btn.BackColor = Color.FromArgb(180, 180, 180);
            if (text == "获取4399小号")
                btn.BackColor = Color.FromArgb(90, 150, 210);
        };
        return btn;
    }

    private void RefreshAccountList()
    {
        _accounts = AccountManager.GetAllSorted();
        accountListView.Items.Clear();

        foreach (var acc in _accounts)
        {
            var item = new ListViewItem(acc.Name);
            item.SubItems.Add(acc.TypeDisplay);
            item.SubItems.Add(acc.LastUsedDisplay);
            item.SubItems.Add(acc.Notes ?? "");
            item.Tag = acc;
            accountListView.Items.Add(item);
        }

        UpdateAccountCount();
    }

    private void UpdateAccountCount()
    {
        accountCountLabel.Text = _accounts.Count > 0
            ? $"共 {_accounts.Count} 个账号"
            : "暂无保存的账号，请添加或手动输入";
    }

    private void OnAccountSelected(object sender, EventArgs e)
    {
        var hasSelection = accountListView.SelectedItems.Count > 0;
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
            detailTitle.Text = "选择一个账号查看详情";
            detailContent.Text = "";
        }
    }

    private void UpdateDetailPanel(AccountInfo acc)
    {
        detailTitle.Text = $"{acc.Name}  [{acc.TypeDisplay}]";
        string info;
        if (acc.Type == AccountType.Cookie)
        {
            var preview = string.IsNullOrEmpty(acc.CookieData)
                ? "(空)"
                : acc.CookieData.Length > 80
                    ? acc.CookieData.Substring(0, 80) + "..."
                    : acc.CookieData;
            info = $"Cookie: {preview}";
        }
        else if (acc.Type == AccountType.Phone)
        {
            var hasCache = string.IsNullOrEmpty(acc.CookieData) ? "无" : "有";
            info = $"手机号: {acc.PhoneNumber ?? "(空)"}    已缓存凭证: {hasCache}";
        }
        else
        {
            info = $"用户名: {acc.Username ?? "(空)"}    密码: {(string.IsNullOrEmpty(acc.Password) ? "(空)" : "******")}";
        }

        if (!string.IsNullOrEmpty(acc.Notes)) info += $"\n备注: {acc.Notes}";
        detailContent.Text = info;
    }

    private void OnAccountDoubleClick(object sender, EventArgs e)
    {
        if (accountListView.SelectedItems.Count > 0) PerformLogin();
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
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnAddClick(object sender, EventArgs e)
    {
        var dialog = new AccountEditDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AccountManager.Add(dialog.Account);
            RefreshAccountList();
            // Select newly added
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
            $"确定要删除账号 \"{acc.Name}\" 吗？\n此操作不可恢复。",
            "确认删除",
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
        DialogResult = DialogResult.None;
        Close();
    }

    private void OnOriginalClick(object sender, EventArgs e)
    {
        Action = LoginAction.UseOriginal;
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void SelectAccountByName(string name)
    {
        foreach (ListViewItem item in accountListView.Items)
            if (item.Text == name)
            {
                item.Selected = true;
                item.EnsureVisible();
                break;
            }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (DialogResult == DialogResult.None)
        {
            // User closed the window via X button
            // Check if there was a selection they might have wanted to use
            Action = LoginAction.UseOriginal;
            DialogResult = DialogResult.Abort;
        }

        base.OnFormClosed(e);
    }
}