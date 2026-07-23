using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.UI.Controls;

public class ServerPanel : Form
{
    private Button closeButton;
    // private Label ServerInfoLabel;

    public ServerPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "服务器操作";
        Size = new Size(350, 200); // 增加窗体宽度
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
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
        Controls.Add(topMostCheck);

        // ServerInfoLabel = new Label();
        // ServerInfoLabel.Text = $"服务器内网Ip:{WebRtcVar.Port}\n服务器端口: {WebRtcVar.Port.ToString()}\n房间号详见网易弹出的房间管理窗口";
        // ServerInfoLabel.Location = new Point(20, 20);
        // ServerInfoLabel.Size = new Size(80, 20);
        // this.Controls.Add(ServerInfoLabel);


        // 应用按钮
        closeButton = new Button();
        closeButton.Text = "关闭服务器";
        closeButton.Location = new Point(50, 100);
        closeButton.Size = new Size(75, 30);
        closeButton.Click += CloseButton_Click;
        closeButton.Enabled = true;
        Controls.Add(closeButton);
    }

    private void CloseButton_Click(object sender, EventArgs e)
    {
    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
        WpfConfig.IsSelectedIP = true;
    }
}