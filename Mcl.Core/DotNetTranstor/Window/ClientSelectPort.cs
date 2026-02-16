using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.DotNetTranstor.Var;

namespace Mcl.Core.DotNetTranstor.Window
{
    public class ClientSelectPort : Form
    {
        private TextBox portTextBox;
        private CheckBox compressCheckBox; // 新增 CheckBox
        private Button applyButton;
        private Button cancelButton;

        public ClientSelectPort() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Text = "TCP 端口转发配置(Client)";
            this.Size = new Size(350, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            
            Label portLabel = new Label() { Text = "端口号:", Location = new Point(20, 55), Size = new Size(60, 20) };
            portTextBox = new TextBox() { Location = new Point(100, 55), Size = new Size(200, 20), Text = "25565" };

            // 初始化 CheckBox
            // compressCheckBox = new CheckBox() 
            // { 
            //     Text = "启用 WebRTC 压缩检测", 
            //     Location = new Point(20, 90), 
            //     Size = new Size(280, 20),
            //     Checked = true 
            // };

            applyButton = new Button() { Text = "确定", Location = new Point(60, 140), Size = new Size(80, 30) };
            applyButton.Click += (s, e) => {
                if (int.TryParse(portTextBox.Text, out int port)) {
                    WebRtcVar.Port = port;
                    // WebRtcVar.UseCompressionDetection = compressCheckBox.Checked;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };

            cancelButton = new Button() { Text = "取消", Location = new Point(180, 140), Size = new Size(80, 30) };
            cancelButton.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { portLabel, portTextBox, compressCheckBox, applyButton, cancelButton });
        }
    }
}