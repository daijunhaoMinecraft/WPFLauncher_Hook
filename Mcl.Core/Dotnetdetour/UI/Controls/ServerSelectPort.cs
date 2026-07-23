using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Globals;

namespace Mcl.Core.Dotnetdetour.UI.Controls;

public class ServerSelectPort : Form
{
    private Button applyButton;
    private Button cancelButton;
    private CheckBox compressCheckBox; // 新增 CheckBox
    private TextBox ipTextBox;
    private TextBox portTextBox;

    public ServerSelectPort()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "TCP 端口转发配置";
        Size = new Size(350, 250);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
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

        var ipLabel = new Label { Text = "目标IP:", Location = new Point(20, 20), Size = new Size(60, 20) };
        ipTextBox = new TextBox { Location = new Point(100, 20), Size = new Size(200, 20), Text = "127.0.0.1" };

        var portLabel = new Label { Text = "端口号:", Location = new Point(20, 55), Size = new Size(60, 20) };
        portTextBox = new TextBox { Location = new Point(100, 55), Size = new Size(200, 20), Text = "25565" };

        // 初始化 CheckBox
        // compressCheckBox = new CheckBox() 
        // { 
        //     Text = "启用 WebRTC 压缩检测 (针对国际服隧道)", 
        //     Location = new Point(20, 90), 
        //     Size = new Size(280, 20),
        //     Checked = true 
        // };

        applyButton = new Button { Text = "确定", Location = new Point(60, 140), Size = new Size(80, 30) };
        applyButton.Click += (s, e) =>
        {
            if (int.TryParse(portTextBox.Text, out var port))
            {
                WebRtcVar.Ip = ipTextBox.Text;
                WebRtcVar.Port = port;
                // WebRtcVar.UseCompressionDetection = compressCheckBox.Checked;
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        cancelButton = new Button { Text = "取消", Location = new Point(180, 140), Size = new Size(80, 30) };
        cancelButton.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
            { topMostCheck, ipLabel, ipTextBox, portLabel, portTextBox, applyButton, cancelButton });
    }
}