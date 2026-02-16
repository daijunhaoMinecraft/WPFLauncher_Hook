using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.DotNetTranstor.Var;

namespace Mcl.Core.DotNetTranstor.Window
{
    public class ServerSelectPort : Form
    {
        private TextBox ipTextBox;
        private TextBox portTextBox;
        private CheckBox compressCheckBox; // 新增 CheckBox
        private Button applyButton;
        private Button cancelButton;

        public ServerSelectPort() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Text = "TCP 端口转发配置";
            this.Size = new Size(350, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label ipLabel = new Label() { Text = "目标IP:", Location = new Point(20, 20), Size = new Size(60, 20) };
            ipTextBox = new TextBox() { Location = new Point(100, 20), Size = new Size(200, 20), Text = "127.0.0.1" };

            Label portLabel = new Label() { Text = "端口号:", Location = new Point(20, 55), Size = new Size(60, 20) };
            portTextBox = new TextBox() { Location = new Point(100, 55), Size = new Size(200, 20), Text = "25565" };

            // 初始化 CheckBox
            // compressCheckBox = new CheckBox() 
            // { 
            //     Text = "启用 WebRTC 压缩检测 (针对国际服隧道)", 
            //     Location = new Point(20, 90), 
            //     Size = new Size(280, 20),
            //     Checked = true 
            // };

            applyButton = new Button() { Text = "确定", Location = new Point(60, 140), Size = new Size(80, 30) };
            applyButton.Click += (s, e) => {
                if (int.TryParse(portTextBox.Text, out int port)) {
                    WebRtcVar.Ip = ipTextBox.Text;
                    WebRtcVar.Port = port;
                    // WebRtcVar.UseCompressionDetection = compressCheckBox.Checked;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };

            cancelButton = new Button() { Text = "取消", Location = new Point(180, 140), Size = new Size(80, 30) };
            cancelButton.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { ipLabel, ipTextBox, portLabel, portTextBox, compressCheckBox, applyButton, cancelButton });
        }
    }
}