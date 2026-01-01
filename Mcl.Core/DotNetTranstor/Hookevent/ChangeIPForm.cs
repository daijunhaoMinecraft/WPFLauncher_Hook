using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;
using WPFLauncher.Model.Game;
using WPFLauncher.Network.Protocol.LobbyGame;

namespace DotNetTranstor.Hookevent
{
    public class ChangeIPForm : Form
    {
        private TextBox ipTextBox;
        private TextBox portTextBox;
        private Button applyButton;
        private Button cancelButton;
        private Label ipLabel;
        private Label portLabel;
        private akv _roomInfo;

        public ChangeIPForm(akv roomInfo)
        {
            _roomInfo = roomInfo;
            InitializeComponent();
            LoadRoomInfo();
        }

        private void InitializeComponent()
        {
            this.Text = "更改房间IP地址";
            this.Size = new Size(350, 200); // 增加窗体宽度
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // IP标签
            ipLabel = new Label();
            ipLabel.Text = "IP地址:";
            ipLabel.Location = new Point(20, 20);
            ipLabel.Size = new Size(80, 20);
            this.Controls.Add(ipLabel);

            // IP输入框
            ipTextBox = new TextBox();
            ipTextBox.Location = new Point(100, 20);
            ipTextBox.Size = new Size(200, 20); // 增加宽度
            ipTextBox.Enabled = true;
            this.Controls.Add(ipTextBox);

            // 端口标签
            portLabel = new Label();
            portLabel.Text = "端口号:";
            portLabel.Location = new Point(20, 50);
            portLabel.Size = new Size(80, 20);
            this.Controls.Add(portLabel);

            // 端口输入框
            portTextBox = new TextBox();
            portTextBox.Location = new Point(100, 50);
            portTextBox.Size = new Size(200, 20); // 增加宽度
            portTextBox.Enabled = true;
            this.Controls.Add(portTextBox);

            // 应用按钮
            applyButton = new Button();
            applyButton.Text = "应用";
            applyButton.Location = new Point(50, 100);
            applyButton.Size = new Size(75, 30);
            applyButton.Click += ApplyButton_Click;
            applyButton.Enabled = true;
            this.Controls.Add(applyButton);

            // 取消按钮
            cancelButton = new Button();
            cancelButton.Text = "关闭";
            cancelButton.Location = new Point(150, 100);
            cancelButton.Size = new Size(75, 30);
            cancelButton.Click += CancelButton_Click;
            this.Controls.Add(cancelButton);
        }
        private void LoadRoomInfo()
        {
            if (_roomInfo != null && _roomInfo.CppGameCfg != null && _roomInfo.CppGameCfg.room_info != null)
            {
                ipTextBox.Text = _roomInfo.CppGameCfg.room_info.ip;
                portTextBox.Text = _roomInfo.CppGameCfg.room_info.port.ToString();
            }
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 验证IP地址
                if (string.IsNullOrWhiteSpace(ipTextBox.Text))
                {
                    MessageBox.Show("IP地址不能为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 验证端口号
                if (!int.TryParse(portTextBox.Text, out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("端口号必须是1-65535之间的整数", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 更新房间信息
                _roomInfo.CppGameCfg.room_info.ip = ipTextBox.Text;
                _roomInfo.CppGameCfg.room_info.port = port;
                string sCppGameConfigPath = _roomInfo.CppGameCfg.path;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[CustomIP] CppGamePath: {sCppGameConfigPath}");
                System.IO.File.WriteAllText(sCppGameConfigPath, JsonConvert.SerializeObject(_roomInfo.CppGameCfg));
                Console.WriteLine("[CustomIP] Config Saved!");
                Console.ForegroundColor = ConsoleColor.White;
                // _roomInfo.CppGameCfg.Save();

                //MessageBox.Show("IP地址和端口号已成功更新", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
                Path_Bool.IsSelectedIP = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
            Path_Bool.IsSelectedIP = true;
        }
    }
}