using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Mcl.Core.Dotnetdetour.UI.Core;

namespace Mcl.Core.Dotnetdetour.UI.Forms;

public class AccountSelectionForm : Form
{
    private List<AccountInfo> _accounts;
    private ListView _accountListView;
    private Label _detailContent;
    private Label _detailTitle;
    private ModernButton _loginButton;
    private ModernButton _editButton;
    private ModernButton _deleteButton;
    
    public AccountInfo SelectedAccount { get; private set; }
    public bool UseOriginalLogin { get; private set; }

    public AccountSelectionForm()
    {
        UITheme.ApplyModernForm(this, "多账号管理", 700, 500);
        InitializeComponent();
        RefreshAccountList();
    }

    private void InitializeComponent()
    {
        // 顶部 Header
        var headerPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UITheme.Surface };
        var titleLabel = new Label { Text = "选择登录账号", Font = UITheme.TitleFont, ForeColor = UITheme.PrimaryBlue, Location = new Point(20, 18), AutoSize = true };
        headerPanel.Controls.Add(titleLabel);
        Controls.Add(headerPanel);

        // 左侧列表
        _accountListView = new ListView
        {
            Location = new Point(20, 80),
            Size = new Size(460, 280),
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            GridLines = false,
            BorderStyle = BorderStyle.FixedSingle,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        _accountListView.Columns.Add("账号名称", 150);
        _accountListView.Columns.Add("类型", 80);
        _accountListView.Columns.Add("备注", 200);
        _accountListView.SelectedIndexChanged += OnAccountSelected;
        _accountListView.DoubleClick += (s, e) => PerformLogin();
        Controls.Add(_accountListView);

        // 详情面板
        var detailPanel = new Panel { Location = new Point(20, 375), Size = new Size(460, 75), BackColor = UITheme.Surface, BorderStyle = BorderStyle.FixedSingle };
        _detailTitle = new Label { Location = new Point(10, 10), Size = new Size(440, 20), Font = UITheme.BoldFont, Text = "选择一个账号查看详情" };
        _detailContent = new Label { Location = new Point(10, 35), Size = new Size(440, 35), ForeColor = UITheme.TextSecondary, Text = "暂无详细信息" };
        detailPanel.Controls.Add(_detailTitle);
        detailPanel.Controls.Add(_detailContent);
        Controls.Add(detailPanel);

        // 右侧按钮组
        int btnX = 500;
        int btnWidth = 160;

        _loginButton = new ModernButton { Text = "登录选中账号", Location = new Point(btnX, 80), Size = new Size(btnWidth, 36), IsPrimary = true, Enabled = false };
        _loginButton.Click += (s, e) => PerformLogin();
        Controls.Add(_loginButton);

        var addButton = new ModernButton { Text = "添加账号", Location = new Point(btnX, 126), Size = new Size(btnWidth, 36) };
        addButton.Click += OnAddClick;
        Controls.Add(addButton);

        _editButton = new ModernButton { Text = "编辑账号", Location = new Point(btnX, 172), Size = new Size(btnWidth, 36), Enabled = false };
        _editButton.Click += OnEditClick;
        Controls.Add(_editButton);

        _deleteButton = new ModernButton { Text = "删除账号", Location = new Point(btnX, 218), Size = new Size(btnWidth, 36), Enabled = false };
        _deleteButton.Click += OnDeleteClick;
        Controls.Add(_deleteButton);

        var sep = new Label { Location = new Point(btnX, 270), Size = new Size(btnWidth, 1), BackColor = UITheme.Border };
        Controls.Add(sep);

        var manualButton = new ModernButton { Text = "手动输入 (一次性)", Location = new Point(btnX, 285), Size = new Size(btnWidth, 36) };
        manualButton.Click += OnManualClick;
        Controls.Add(manualButton);

        var originalButton = new ModernButton { Text = "使用原版登录", Location = new Point(btnX, 331), Size = new Size(btnWidth, 36) };
        originalButton.Click += (s, e) => { UseOriginalLogin = true; DialogResult = DialogResult.OK; Close(); };
        Controls.Add(originalButton);
    }

    private void RefreshAccountList()
    {
        _accounts = AccountManager.GetAllSorted();
        _accountListView.Items.Clear();
        foreach (var acc in _accounts)
        {
            var item = new ListViewItem(acc.Name);
            item.SubItems.Add(acc.TypeDisplay);
            item.SubItems.Add(acc.Notes ?? "");
            item.Tag = acc;
            _accountListView.Items.Add(item);
        }
    }

    private void OnAccountSelected(object sender, EventArgs e)
    {
        bool hasSelection = _accountListView.SelectedItems.Count > 0;
        _loginButton.Enabled = _editButton.Enabled = _deleteButton.Enabled = hasSelection;

        if (hasSelection)
        {
            var acc = (AccountInfo)_accountListView.SelectedItems[0].Tag;
            _detailTitle.Text = $"{acc.Name} [{acc.Type}]";
            _detailContent.Text = acc.Type == AccountType.Cookie ? "类型: Cookie 数据" : 
                                  acc.Type == AccountType.Phone ? $"手机号: {acc.PhoneNumber}" : 
                                  $"账号: {acc.Username}";
        }
        else
        {
            _detailTitle.Text = "选择一个账号查看详情";
            _detailContent.Text = "暂无详细信息";
        }
    }

    private void PerformLogin()
    {
        if (_accountListView.SelectedItems.Count == 0) return;
        SelectedAccount = (AccountInfo)_accountListView.SelectedItems[0].Tag;
        AccountManager.MarkUsed(SelectedAccount);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnManualClick(object sender, EventArgs e)
    {
        using var manualForm = new ManualLoginForm();
        if (manualForm.ShowDialog(this) == DialogResult.OK)
        {
            SelectedAccount = manualForm.GeneratedAccount;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void OnAddClick(object sender, EventArgs e)
    {
        using var dialog = new AccountEditorForm();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AccountManager.Add(dialog.Account);
            RefreshAccountList();
        }
    }

    private void OnEditClick(object sender, EventArgs e)
    {
        if (_accountListView.SelectedItems.Count == 0) return;
        var acc = (AccountInfo)_accountListView.SelectedItems[0].Tag;
        using var dialog = new AccountEditorForm(acc);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AccountManager.Update(dialog.OriginalName, dialog.Account);
            RefreshAccountList();
        }
    }

    private void OnDeleteClick(object sender, EventArgs e)
    {
        if (_accountListView.SelectedItems.Count == 0) return;
        var acc = (AccountInfo)_accountListView.SelectedItems[0].Tag;
        if (MessageBox.Show($"确定要删除账号 \"{acc.Name}\" 吗？", "确认删除", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
        {
            AccountManager.Delete(acc.Name);
            RefreshAccountList();
        }
    }
}