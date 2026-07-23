using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.Utilities.Common;
// 添加 WinForms 支持

internal class Ocr
{
    private static int inner_startPort = 50001;

    /// <summary>
    ///     异步执行命令（已清空，不再使用）
    /// </summary>
    public static async Task InvokeCmdAsync(string cmdArgs)
    {
        // 已清空：不再执行任何操作
        await Task.CompletedTask;
    }

    /// <summary>
    ///     OCR 识别函数 - 弹窗让用户输入验证码。
    /// </summary>
    /// <param name="base64Image">base64 编码的验证码图片</param>
    /// <returns>用户输入的验证码文本，或 null（取消）</returns>
    public static string GetOcr(string base64Image)
    {
        return GetOcrWithRefresh(base64Image, null);
    }

    /// <summary>
    ///     OCR 识别函数（带刷新按钮）。
    ///     当 onRefresh 不为 null 时显示"刷新"按钮，
    ///     点击后调用 onRefresh 获取新图片 base64 并更新显示。
    /// </summary>
    /// <param name="imageBase64">base64 编码的验证码图片</param>
    /// <param name="onRefresh">刷新回调，返回新图片 base64</param>
    /// <returns>用户输入的验证码文本，或 null（取消）</returns>
    public static string GetOcrWithRefresh(
        string imageBase64, Func<string> onRefresh)
    {
        string result = null;
        ThreadHelperSTATask.Run(() => { result = ShowCaptchaInputForm(imageBase64, onRefresh); });
        return result;
    }

    /// <summary>
    ///     获取未使用的端口（已清空）
    /// </summary>
    public static int GetUnusedPort()
    {
        // 已清空
        return -1;
    }

    /// <summary>
    ///     获取已被占用的端口列表（已清空）
    /// </summary>
    private static List<int> GetPortIsInOccupiedState()
    {
        // 已清空
        return new List<int>();
    }

    /// <summary>
    ///     通过 netstat 获取占用端口（已清空）
    /// </summary>
    private static string GetPortIsInOccupiedStateByNetStat()
    {
        // 已清空
        return "";
    }


    // ================================
    // 私有辅助方法：显示验证码输入窗口
    // ================================
    private static string ShowCaptchaInputForm(
        string imageBase64, Func<string> onRefresh = null)
    {
        using (var form = new Form())
        {
            form.Text = "请输入验证码";
            form.Width = 320;
            form.Height = 240;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.TopMost = WpfConfig.IsWindowTopMost;

            var topMostCheck = new CheckBox
            {
                Text = "置顶",
                Size = new Size(55, 20),
                Checked = WpfConfig.IsWindowTopMost,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            topMostCheck.Left = form.ClientSize.Width - topMostCheck.Width - 15;
            topMostCheck.Top = 5;
            topMostCheck.CheckedChanged += (s, e) => form.TopMost = topMostCheck.Checked;
            form.Controls.Add(topMostCheck);

            var label = new Label
            {
                Text = "验证码：",
                Location = new Point(15, 15),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new Point(15, 40),
                Width = 200
            };

            // 图片显示
            PictureBox pictureBox = null;
            Action<string> updateImage = b64 =>
            {
                if (pictureBox != null)
                    form.Controls.Remove(pictureBox);
                if (string.IsNullOrEmpty(b64)) return;
                try
                {
                    var imageBytes = Convert.FromBase64String(b64);
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        var img = Image.FromStream(ms);
                        pictureBox = new PictureBox
                        {
                            Image = img,
                            Location = new Point(15, 70),
                            Size = new Size(120, 60),
                            SizeMode = PictureBoxSizeMode.Zoom
                        };
                        form.Controls.Add(pictureBox);
                    }
                }
                catch
                {
                }
            };

            updateImage(imageBase64);

            // 刷新按钮
            if (onRefresh != null)
            {
                var btnRefresh = new Button
                {
                    Text = "刷新",
                    Size = new Size(60, 25),
                    Location = new Point(220, 38)
                };
                btnRefresh.Click += (s, e) =>
                {
                    try
                    {
                        var newImage = onRefresh();
                        if (!string.IsNullOrEmpty(newImage))
                            updateImage(newImage);
                    }
                    catch
                    {
                    }
                };
                form.Controls.Add(btnRefresh);
            }

            var btnOk = new Button { Text = "确定", DialogResult = DialogResult.OK };
            btnOk.Location = new Point(130, form.Height - 60);

            var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
            btnCancel.Location = new Point(215, form.Height - 60);

            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            form.Controls.Add(label);
            form.Controls.Add(textBox);
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);

            var dialogResult = form.ShowDialog();
            return dialogResult == DialogResult.OK ? textBox.Text : null;
        }
    }
}

// ================================
// STA 线程帮助类（用于在非 UI 线程中弹出 WinForm）
// ================================
internal static class ThreadHelperSTATask
{
    public static void Run(Action worker)
    {
        var thread = new Thread(() => { worker(); })
        {
            IsBackground = true,
            ApartmentState = ApartmentState.STA // 关键：设置为 STA
        };
        thread.Start();
        thread.Join(); // 等待完成
    }

    [ComImport]
    [Guid("45D8CCCD-0A1B-4E9E-9F7F-8E3C8B5E7F9D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IThreadContext
    {
        void DoWork(Action action);
    }
}