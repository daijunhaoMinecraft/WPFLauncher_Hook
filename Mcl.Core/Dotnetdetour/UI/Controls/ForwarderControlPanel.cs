using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Globals;
using WPFLauncher.Manager.LanGame;

namespace Mcl.Core.Dotnetdetour.UI.Controls;

public class ForwarderControlPanel : Form
{
    private bool isClosing;
    private Label modeLabel;
    private Label portLabel;
    private Label statusLabel;
    private Button stopButton;

    public ForwarderControlPanel()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        Text = "转发控制台";
        Size = new Size(380, 280);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = SystemColors.Control;
        TopMost = WpfConfig.IsWindowTopMost;

        var topMostCheck = new CheckBox
        {
            Text = "置顶",
            Size = new Size(55, 20),
            Checked = WpfConfig.IsWindowTopMost,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        topMostCheck.Left = ClientSize.Width - topMostCheck.Width - 15;
        topMostCheck.Top = 8;
        topMostCheck.CheckedChanged += (s, e) => TopMost = topMostCheck.Checked;

        var titleLabel = new Label
        {
            Text = "WebRTC 转发状态",
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 10, FontStyle.Bold),
            Location = new Point(20, 15),
            Size = new Size(200, 20)
        };

        var modeTitle = new Label
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

        var portTitle = new Label
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

        var statusTitle = new Label
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

        var separator = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Location = new Point(20, 160),
            Size = new Size(320, 2),
            Height = 2
        };

        stopButton = new Button
        {
            Text = "停止转发",
            Location = new Point(130, 185),
            Size = new Size(100, 30),
            BackColor = Color.FromArgb(220, 220, 220)
        };
        stopButton.Click += StopButton_Click;

        Controls.AddRange(new Control[]
        {
            topMostCheck,
            titleLabel,
            modeTitle, modeLabel,
            portTitle, portLabel,
            statusTitle, statusLabel,
            separator,
            stopButton
        });

        FormClosing += ForwarderControlPanel_FormClosing;
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
            StopForwarding();
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

            var closeTimer = new Timer { Interval = 500 };
            closeTimer.Tick += (s, e) =>
            {
                closeTimer.Stop();
                if (!IsDisposed)
                    Close();
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
        WebRtcVar.LanGameManager.aya.@as(516, WebRtcVar.LanGameManager.HostID);
        WebRtcVar.LanGameManager.aya.d(atl.f);
        Console.WriteLine("停止转发");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
