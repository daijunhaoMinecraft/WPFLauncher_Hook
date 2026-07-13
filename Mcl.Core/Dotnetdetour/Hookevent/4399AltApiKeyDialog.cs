using System;
using System.Drawing;
using System.Windows.Forms;

namespace Mcl.Core.Dotnetdetour.Hookevent
{
    internal class _4399AltApiKeyDialog : Form
    {
        private readonly TextBox apiKeyTextBox;
        private readonly Label errorLabel;

        public string ApiKey => apiKeyTextBox.Text.Trim();

        public _4399AltApiKeyDialog(string currentApiKey)
        {
            Text = "4399小号ApiKey";
            Size = new Size(430, 210);
            BackColor = Color.FromArgb(243, 243, 243);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular);

            var tipLabel = new Label
            {
                Text = "请输入4399小号ApiKey。ApiKey获取页面:",
                Location = new Point(18, 18),
                Size = new Size(380, 22)
            };
            Controls.Add(tipLabel);

            var profileLink = new LinkLabel
            {
                Text = _4399AltApi.ProfileUrl,
                Location = new Point(18, 43),
                Size = new Size(380, 22),
                LinkColor = Color.FromArgb(0, 102, 204),
                ActiveLinkColor = Color.FromArgb(0, 80, 180),
                VisitedLinkColor = Color.FromArgb(0, 102, 204)
            };
            profileLink.LinkClicked += OnProfileLinkClicked;
            Controls.Add(profileLink);

            apiKeyTextBox = new TextBox
            {
                Location = new Point(18, 78),
                Size = new Size(380, 24),
                Text = currentApiKey ?? string.Empty
            };
            Controls.Add(apiKeyTextBox);

            errorLabel = new Label
            {
                Location = new Point(18, 110),
                Size = new Size(380, 20),
                ForeColor = Color.FromArgb(209, 52, 56),
                Font = new Font("Microsoft YaHei", 8F, FontStyle.Regular)
            };
            Controls.Add(errorLabel);

            var okButton = new Button
            {
                Text = "保存",
                Location = new Point(238, 132),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.Click += OnOkClick;
            Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(323, 132),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private void OnProfileLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!_4399AltApi.OpenProfilePage(out string errorMessage))
            {
                errorLabel.Text = "打开页面失败: " + errorMessage;
            }
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(apiKeyTextBox.Text))
            {
                errorLabel.Text = "请输入ApiKey";
                DialogResult = DialogResult.None;
                apiKeyTextBox.Focus();
            }
        }
    }
}