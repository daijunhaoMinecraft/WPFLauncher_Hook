using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.DotNetTranstor.Var;
using WPFLauncher.Manager.LanGame;

namespace Mcl.Core.DotNetTranstor.Window
{
    public class ForwarderControlPanel : Form
    {
        private Label modeLabel;
        private Label statusLabel;
        private Label portLabel;
        // private Label bytesLabel;
        private Button stopButton;
        // private Timer updateTimer;
        private bool isClosing = false;
        // private long lastBytes = 0;
        // private DateTime lastUpdate = DateTime.Now;

        public ForwarderControlPanel()
        {
            InitializeComponent();
            LoadSettings();
            // StartStatusUpdate();
        }

        private void InitializeComponent()
        {
            // 基础窗口设置
            this.Text = "转发控制台";
            this.Size = new Size(380, 280);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = SystemColors.Control;

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

            // // 流量统计
            // Label bytesTitle = new Label
            // {
            //     Text = "转发流量:",
            //     Location = new Point(20, 125),
            //     Size = new Size(70, 20)
            // };
            // bytesLabel = new Label
            // {
            //     Text = "0 B",
            //     Location = new Point(100, 125),
            //     Size = new Size(200, 20)
            // };

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

            // 日志/信息区域（只读文本框）
            // TextBox infoBox = new TextBox
            // {
            //     Location = new Point(20, 225),
            //     Size = new Size(320, 40),
            //     Multiline = true,
            //     ReadOnly = true,
            //     BackColor = SystemColors.Control,
            //     BorderStyle = BorderStyle.FixedSingle,
            //     Text = "提示: 关闭此窗口不会停止转发服务",
            //     Font = new Font(SystemFonts.DefaultFont.FontFamily, 8)
            // };

            // 添加控件
            this.Controls.AddRange(new Control[]
            {
                titleLabel,
                modeTitle, modeLabel,
                portTitle, portLabel,
                statusTitle, statusLabel,
                // bytesTitle, bytesLabel,
                separator,
                stopButton,
                // infoBox
            });

            this.FormClosing += ForwarderControlPanel_FormClosing;
        }

        private void LoadSettings()
        {
            modeLabel.Text = WebRtcVar.Mode.ToString() ?? "Unknown";
            portLabel.Text = WebRtcVar.Port > 0 ? WebRtcVar.Port.ToString() : "未设置";
        }

        // private void StartStatusUpdate()
        // {
        //     updateTimer = new Timer { Interval = 1000 };
        //     updateTimer.Tick += (s, e) => UpdateStatus();
        //     updateTimer.Start();
        // }

        // private void UpdateStatus()
        // {
        //     try
        //     {
        //         // 更新模式（可能动态变化）
        //         if (WebRtcVar.Mode != null)
        //             modeLabel.Text = WebRtcVar.Mode.ToString();
        //
        //         // 计算速率（如果有字节统计）
        //         long currentBytes = WebRtcVar.TotalBytesTransferred; // 假设有这个变量
        //         double seconds = (DateTime.Now - lastUpdate).TotalSeconds;
        //         if (seconds > 0 && currentBytes != lastBytes)
        //         {
        //             long bytesDiff = currentBytes - lastBytes;
        //             double speed = bytesDiff / seconds;
        //             bytesLabel.Text = FormatBytes(currentBytes) + 
        //                 " (" + FormatBytes((long)speed) + "/s)";
        //             lastBytes = currentBytes;
        //             lastUpdate = DateTime.Now;
        //         }
        //     }
        //     catch { /* 忽略更新错误 */ }
        // }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("F2") + " GB";
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

                // 执行停止
                // WebRtcVar.AitFunction.axy.t();
                // WebRtcVar.ExitRoomFunction();

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
            // 执行停止
            WebRtcVar.AitFunction.axy.t();

            // updateTimer?.Stop();
            // updateTimer?.Dispose();

            // 注意：这里只停止计时器，不停止转发服务
            // 转发服务在点击"停止转发"按钮时才停止
        }

        protected override void Dispose(bool disposing)
        {
            // if (disposing)
            // {
            //     updateTimer?.Dispose();
            // }
            base.Dispose(disposing);
        }
    }
}