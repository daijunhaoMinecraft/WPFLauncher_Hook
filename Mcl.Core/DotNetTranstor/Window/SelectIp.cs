using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Mcl.Core.DotNetTranstor.Var;
using Mcl.Core.DotNetTranstor.Model;
using Mcl.Core.DotNetTranstor.Tools; // 引用配置工具类

namespace Mcl.Core.DotNetTranstor.Window
{
    public class SelectIp : Form
    {
        private TextBox lastOctetTextBox;
        private Button applyButton;
        private Button cancelButton;
        private Label statusLabel;
        private Panel headerPanel;
        private Label prefixLabel;

        private readonly Color ThemeColor = Color.FromArgb(0, 120, 215);
        private readonly Color ErrorColor = Color.FromArgb(209, 52, 56);
        private readonly Color SuccessColor = Color.FromArgb(16, 124, 16);
        private readonly Font MainFont = new Font("Microsoft YaHei", 9F, FontStyle.Regular);
        private readonly Font TitleFont = new Font("Microsoft YaHei", 10.5F, FontStyle.Bold);
        private readonly Font MonoFont = new Font("Consolas", 11F, FontStyle.Bold);

        private readonly string IpPrefix = "10.0.0.";
        
        public string SelectedIp { get; private set; } = string.Empty;

        public SelectIp()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "分配虚拟 IP";
            this.Size = new Size(360, 260);
            this.BackColor = Color.FromArgb(243, 243, 243);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = MainFont;

            // --- 顶部 ---
            headerPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White, Padding = new Padding(20, 0, 0, 0) };
            Label titleLabel = new Label { Text = "虚拟网卡 IP 配置", Font = TitleFont, ForeColor = Color.FromArgb(32, 32, 32), TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            headerPanel.Controls.Add(titleLabel);
            this.Controls.Add(headerPanel);

            Label line = new Label { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(220, 220, 220) };
            this.Controls.Add(line);

            // --- 输入区 ---
            Label instructionLabel = new Label { Text = "请输入主机位 (0-255):", Location = new Point(25, 80), Size = new Size(300, 20), ForeColor = Color.FromArgb(64, 64, 64) };
            this.Controls.Add(instructionLabel);

            Panel ipContainer = new Panel { Location = new Point(25, 105), Size = new Size(290, 35), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            
            prefixLabel = new Label { Text = IpPrefix, Location = new Point(10, 0), Size = new Size(75, 33), TextAlign = ContentAlignment.MiddleRight, Font = MonoFont, ForeColor = Color.FromArgb(64, 64, 64), BackColor = Color.White };
            
            lastOctetTextBox = new TextBox { Location = new Point(90, 5), Size = new Size(60, 25), BorderStyle = BorderStyle.None, Font = MonoFont, TextAlign = HorizontalAlignment.Center, MaxLength = 3 };
            lastOctetTextBox.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            
            ipContainer.Controls.Add(prefixLabel);
            ipContainer.Controls.Add(lastOctetTextBox);
            this.Controls.Add(ipContainer);

            // --- 状态 ---
            statusLabel = new Label { Location = new Point(25, 145), Size = new Size(290, 20), ForeColor = ErrorColor, Text = "", Visible = false, Font = new Font("Microsoft YaHei", 8F, FontStyle.Italic) };
            this.Controls.Add(statusLabel);

            // --- 按钮 ---
            applyButton = new Button { Text = "确认并保存", Location = new Point(130, 180), Size = new Size(90, 32), BackColor = ThemeColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold) };
            applyButton.FlatAppearance.BorderSize = 0;
            applyButton.Click += ApplyButton_Click;

            cancelButton = new Button { Text = "取消", Location = new Point(230, 180), Size = new Size(90, 32), BackColor = Color.FromArgb(225, 225, 225), ForeColor = Color.FromArgb(64, 64, 64), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.DialogResult = DialogResult.Cancel;

            this.Controls.Add(cancelButton);
            this.Controls.Add(applyButton);

            this.AcceptButton = applyButton;
            this.CancelButton = cancelButton;
        }

        private void LoadCurrentSettings()
        {
            // 优先级 1: 尝试读取配置文件 (LanGameIP.txt)
            int? savedOctet = IpConfigManager.LoadLastOctet();
            
            if (savedOctet.HasValue)
            {
                lastOctetTextBox.Text = savedOctet.Value.ToString();
                statusLabel.Text = $"已加载上次配置: {IpPrefix}{savedOctet.Value}";
                statusLabel.ForeColor = Color.FromArgb(64, 64, 64); // 灰色提示
                statusLabel.Visible = true;
                lastOctetTextBox.SelectAll();
                return;
            }

            // 优先级 2: 如果配置文件没有，尝试读取当前运行时变量 (WebRtcVar.MyVirtualIp)
            // 注意：这里增加了 null/empty 检查，防止报错
            if (!string.IsNullOrEmpty(WebRtcVar.MyVirtualIp) && WebRtcVar.MyVirtualIp.StartsWith(IpPrefix))
            {
                string lastPart = WebRtcVar.MyVirtualIp.Substring(IpPrefix.Length);
                if (int.TryParse(lastPart, out int val))
                {
                    lastOctetTextBox.Text = val.ToString();
                    lastOctetTextBox.SelectAll();
                    // 此时不显示"已加载配置"，因为这是内存里的，不是持久化的
                    return;
                }
            }

            // 优先级 3: 默认值
            lastOctetTextBox.Text = "100";
            lastOctetTextBox.SelectAll();
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(lastOctetTextBox.Text))
            {
                ShowError("请输入主机位数字");
                return;
            }

            if (!int.TryParse(lastOctetTextBox.Text, out int lastOctet))
            {
                ShowError("格式错误：必须是数字");
                return;
            }

            if (lastOctet < 0 || lastOctet > 255)
            {
                ShowError("范围错误：必须在 0 到 255 之间");
                return;
            }

            string candidateIp = $"{IpPrefix}{lastOctet}";

            // 冲突检测
            bool isConflict = WebRtcVar.PlayerList.Any(p => 
                !string.IsNullOrEmpty(p.VirtualIp) && 
                p.VirtualIp == candidateIp
                // 如果需要排除自己: && p.PeerId != WebRtcVar.MyPeerId
            );

            if (isConflict)
            {
                ShowError($"冲突：IP {candidateIp} 已被其他玩家占用");
                return;
            }

            // --- 核心步骤：保存 ---
            
            // 1. 写入配置文件
            if (!IpConfigManager.SaveLastOctet(lastOctet))
            {
                ShowError("保存配置文件失败 (检查磁盘权限)");
                return;
            }

            // 2. 更新运行时变量 (现在 WebRtcVar 中已有此属性)
            WebRtcVar.MyVirtualIp = candidateIp;

            // 验证通过，准备返回
            SelectedIp = candidateIp;
            
            statusLabel.Text = "验证通过 & 配置已保存";
            statusLabel.ForeColor = SuccessColor;
            statusLabel.Visible = true;

            Timer t = new Timer { Interval = 400 };
            t.Tick += (s, ev) => {
                t.Stop();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            t.Start();
        }

        private void ShowError(string message)
        {
            statusLabel.Text = message;
            statusLabel.ForeColor = ErrorColor;
            statusLabel.Visible = true;

            int originalLeft = lastOctetTextBox.Left;
            Timer t = new Timer { Interval = 40 };
            int count = 0;
            t.Tick += (s, ev) => {
                int offset = (count % 2 == 0) ? 4 : -4;
                lastOctetTextBox.Left = originalLeft + offset;
                
                if (++count > 6) 
                { 
                    t.Stop(); 
                    lastOctetTextBox.Left = originalLeft; 
                    lastOctetTextBox.Focus();
                    lastOctetTextBox.SelectAll();
                }
            };
            t.Start();
        }
    }
}