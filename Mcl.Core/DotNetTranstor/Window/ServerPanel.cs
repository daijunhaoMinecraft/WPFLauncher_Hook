using System;
using System.Drawing;
using System.Windows.Forms;
using DotNetTranstor.Hookevent;
using Mcl.Core.DotNetTranstor.Var;

namespace Mcl.Core.DotNetTranstor.Window
{
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
            this.Text = "服务器操作";
            this.Size = new Size(350, 200); // 增加窗体宽度
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = Path_Bool.IsWindowTopMost;

            var topMostCheck = new CheckBox
            {
                Text = "置顶",
                Size = new Size(55, 20),
                Checked = Path_Bool.IsWindowTopMost,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            topMostCheck.Left = this.ClientSize.Width - topMostCheck.Width - 15;
            topMostCheck.Top = 8;
            topMostCheck.CheckedChanged += (s, e) => this.TopMost = topMostCheck.Checked;
            this.Controls.Add(topMostCheck);
            
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
            this.Controls.Add(closeButton);
        }
        private void CloseButton_Click(object sender, EventArgs e)
        {
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
            Path_Bool.IsSelectedIP = true;
        }
    }
}