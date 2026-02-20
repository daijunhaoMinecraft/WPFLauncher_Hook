using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.DotNetTranstor.Var;

namespace Mcl.Core.DotNetTranstor.Window
{
    public class ClientSelectPort : Form
    {
        private TextBox portTextBox;
        private Button applyButton;
        private Button cancelButton;
        private Label statusLabel;
        private Panel headerPanel;

        // 定义标准颜色和字体
        private readonly Color ThemeColor = Color.FromArgb(0, 120, 215); // 现代 Windows 蓝色
        private readonly Color ErrorColor = Color.FromArgb(209, 52, 56);   // 警告红
        private readonly Font MainFont = new Font("Microsoft YaHei", 9F, FontStyle.Regular);
        private readonly Font TitleFont = new Font("Microsoft YaHei", 10.5F, FontStyle.Bold);

        public int SelectedPort => int.TryParse(portTextBox.Text, out int port) ? port : 25565;

        public ClientSelectPort()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            // 窗口基础属性
            this.Text = "配置选项";
            this.Size = new Size(340, 240);
            this.BackColor = Color.FromArgb(243, 243, 243); // 浅灰背景
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = MainFont;

            // 顶部装饰条/标题区
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(20, 0, 0, 0)
            };

            Label titleLabel = new Label
            {
                Text = "端口转发设置",
                Font = TitleFont,
                ForeColor = Color.FromArgb(32, 32, 32),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            headerPanel.Controls.Add(titleLabel);

            // 分割线
            Label line = new Label
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(220, 220, 220)
            };

            // 内容区域
            Label portLabel = new Label
            {
                Text = "目标端口",
                Location = new Point(25, 85),
                Size = new Size(100, 20),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            portTextBox = new TextBox
            {
                Location = new Point(25, 110),
                Size = new Size(275, 25),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10F) // 端口号使用等宽字体更专业
            };
            portTextBox.KeyPress += (s, e) => {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
            };

            // 状态提示
            statusLabel = new Label
            {
                Location = new Point(25, 140),
                Size = new Size(275, 20),
                ForeColor = ErrorColor,
                Text = "",
                Visible = false
            };

            // 底部按钮区域
            applyButton = new Button
            {
                Text = "保存设置",
                Location = new Point(115, 165),
                Size = new Size(90, 30),
                BackColor = ThemeColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            applyButton.FlatAppearance.BorderSize = 0;
            applyButton.Click += ApplyButton_Click;

            cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(210, 165),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(225, 225, 225),
                FlatStyle = FlatStyle.Flat
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.DialogResult = DialogResult.Cancel;

            // 添加控件
            this.Controls.AddRange(new Control[] {
                cancelButton,
                applyButton,
                statusLabel,
                portTextBox,
                portLabel,
                line,
                headerPanel
            });

            this.AcceptButton = applyButton;
            this.CancelButton = cancelButton;
        }

        private void LoadCurrentSettings()
        {
            if (WebRtcVar.Port > 0 && WebRtcVar.Port <= 65535)
            {
                portTextBox.Text = WebRtcVar.Port.ToString();
            }
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(portTextBox.Text) || !int.TryParse(portTextBox.Text, out int port))
            {
                ShowError("请输入有效的数字端口号");
                return;
            }

            if (port < 1 || port > 65535)
            {
                ShowError("端口范围必须在 1-65535 之间");
                return;
            }

            // 验证通过
            WebRtcVar.Port = port;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ShowError(string message)
        {
            statusLabel.Text = message;
            statusLabel.Visible = true;
            // 简单的抖动反馈效果（可选）
            Timer t = new Timer { Interval = 50 };
            int count = 0;
            t.Tick += (s, ev) => {
                portTextBox.Left += (count % 2 == 0) ? 2 : -2;
                if (++count > 6) { t.Stop(); portTextBox.Left = 25; }
            };
            t.Start();
        }
    }
}