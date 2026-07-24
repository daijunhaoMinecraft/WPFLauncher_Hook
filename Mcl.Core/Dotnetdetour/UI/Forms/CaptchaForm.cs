using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.UI.Core;

namespace Mcl.Core.Dotnetdetour.UI.Forms;

public static class CaptchaHelper
{
    public static string GetOcrWithRefresh(string imageBase64, Func<string> onRefresh)
    {
        string result = null;
        ThreadHelperSTATask.Run(() => 
        {
            using var form = new CaptchaForm(imageBase64, onRefresh);
            if (form.ShowDialog() == DialogResult.OK)
                result = form.CaptchaCode;
        });
        return result;
    }
}

public class CaptchaForm : Form
{
    private PictureBox _pictureBox;
    private TextBox _inputBox;
    private Func<string> _onRefresh;
    public string CaptchaCode => _inputBox.Text.Trim();

    public CaptchaForm(string initialBase64, Func<string> onRefresh)
    {
        _onRefresh = onRefresh;
        UITheme.ApplyModernForm(this, "安全验证", 340, 260);
        TopMost = true;
        InitializeComponent();
        UpdateImage(initialBase64);
    }

    private void InitializeComponent()
    {
        var title = new Label { Text = "请输入图片中的验证码", Font = UITheme.TitleFont, ForeColor = UITheme.PrimaryBlue, Location = new Point(20, 20), AutoSize = true };
        Controls.Add(title);

        var imgPanel = new Panel { Location = new Point(20, 60), Size = new Size(140, 70), BackColor = UITheme.Surface, BorderStyle = BorderStyle.FixedSingle };
        _pictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
        imgPanel.Controls.Add(_pictureBox);
        Controls.Add(imgPanel);

        if (_onRefresh != null)
        {
            var refreshBtn = new ModernButton { Text = "换一张", Location = new Point(180, 75), Size = new Size(80, 32) };
            refreshBtn.Click += (s, e) => UpdateImage(_onRefresh());
            Controls.Add(refreshBtn);
        }

        _inputBox = new TextBox { Location = new Point(20, 150), Size = new Size(285, 25), Font = UITheme.MainFont };
        Controls.Add(_inputBox);

        var confirmBtn = new ModernButton { Text = "确定", Location = new Point(115, 200), IsPrimary = true };
        var cancelBtn = new ModernButton { Text = "取消", Location = new Point(225, 200) };
        
        confirmBtn.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
        cancelBtn.Click += (s, e) => Close();

        AcceptButton = confirmBtn;
        Controls.Add(confirmBtn);
        Controls.Add(cancelBtn);
    }

    private void UpdateImage(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return;
        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using var ms = new MemoryStream(bytes);
            _pictureBox.Image = Image.FromStream(ms);
        }
        catch { }
    }
}

internal static class ThreadHelperSTATask
{
    public static void Run(Action worker)
    {
        var thread = new Thread(() => worker())
        {
            IsBackground = true,
            ApartmentState = ApartmentState.STA
        };
        thread.Start();
        thread.Join();
    }
}