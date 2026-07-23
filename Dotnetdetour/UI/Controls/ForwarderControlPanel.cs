using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Var;
using WPFLauncher.Manager.LanGame;

namespace Mcl.Core.Dotnetdetour.Window
{
    public class ForwarderControlPanel : Form
    {
        private Label modeLabel;
        private Label statusLabel;
        private Label portLabel;
        private Button stopButton;
        private bool isClosing = false;

        public ForwarderControlPanel()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "转发控制台";
            this.Size = new Size(380, 280);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = SystemColors.Control;
            this.TopMost = WpfConfig.IsWindowTopMost;

            var topMostCheck = new CheckBox
            {
                Text = "置顶",
                Size = new Size(55, 20),
                Checked = WpfConfig.IsWindowTopMost,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            topMostCheck.Left = this.ClientSize.Width - topMostCheck.Width - 15;
            topMostCheck.Top = 8;
            topMostCheck.CheckedChanged += (s, e) => this.TopMost = topMostCheck.Checked;

            // 标题
            Label titleLabel = new Label
            {
                Text = "WebRTC 转发状态",
                Font = new Font(SystemFonts.DefaultFont.FontFamily, 10, FontStyle.Bold),
                Location = new Point(20, 15),
                Size = new Size(200, 20)
            };

            // 模式显示
            Label modeTitle = new Label
            {
                Text = "当前模式:",
                Location = new Point(20, 50),
                Size = new Size(70, 20)
            };
            modeLabel = new Label
            {
                Location = new Point(100, 50),
                Size = new Size(200, 20),
                ForeColor = Color.DarkBlue
            };

            // 端口显示
            Label portTitle = new Label
            {
                Text = "转发端口:",
                Location = new Point(20, 75),
                Size = new Size(70, 20)
            };
            portLabel = new Label
            {
                Location = new Point(100, 75),
                Size = new Size(100, 20)
            };

            // 状态显示
            Label statusTitle = new Label
            {
                Text = "运行状态:",
                Location = new Point(20, 100),
                Size = new Size(70, 20)
            };
            statusLabel = new Label
            {
                Text = "运行中",
                ForeColor = Color.Green,
                Location = new Point(100, 100),
                Size = new Size(100, 20)
            };

            // 分隔线
            Label separator = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(20, 160),
                Size = new Size(320, 2),
                Height = 2
            };

            // 停止按钮
            stopButton = new Button
            {
                Text = "停止转发",
                Location = new Point(130, 185),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(220, 220, 220)
            };
            stopButton.Click += StopButton_Click;

            // 添加控件
            this.Controls.AddRange(new Control[]
            {
                topMostCheck,
                titleLabel,
                modeTitle, modeLabel,
                portTitle, portLabel,
                statusTitle, statusLabel,
                // bytesTitle, bytesLabel,
                separator,
                stopButton,
            });

            this.FormClosing += ForwarderControlPanel_FormClosing;
        }

        private void LoadSettings()
        {
            modeLabel.Text = WebRtcVar.Mode.ToString() ?? "Unknown";
            portLabel.Text = WebRtcVar.Port > 0 ? WebRtcVar.Port.ToString() : "未设置";
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要停止转发服务吗？", "确认", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                StopForwarding();
            }
        }

        private void StopForwarding()
        {
            try
            {
                stopButton.Enabled = false;
                stopButton.Text = "停止中...";
                statusLabel.Text = "正在停止...";
                statusLabel.ForeColor = Color.Orange;

                statusLabel.Text = "已停止";
                statusLabel.ForeColor = Color.Red;

                // 延迟关闭以便看到状态变化
                var closeTimer = new Timer { Interval = 500 };
                closeTimer.Tick += (s, e) =>
                {
                    closeTimer.Stop();
                    if (!this.IsDisposed)
                        this.Close();
                };
                closeTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("停止失败: " + ex.Message, "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                stopButton.Enabled = true;
                stopButton.Text = "停止转发";
                statusLabel.Text = "运行中";
                statusLabel.ForeColor = Color.Green;
            }
        }

        private void ForwarderControlPanel_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isClosing) return;
            isClosing = true;
            WebRtcVar.LanGameManager.aya.@as(new object[] { 516, WebRtcVar.LanGameManager.HostID });
            WebRtcVar.LanGameManager.aya.d(atl.f);
            Console.WriteLine("停止转发");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}