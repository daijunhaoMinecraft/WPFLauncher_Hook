using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms; // 添加 WinForms 支持

// Token: 0x0200000A RID: 10
internal class Ocr
{
    /// <summary>
    /// 异步执行命令（已清空，不再使用）
    /// </summary>
    public static async Task InvokeCmdAsync(string cmdArgs)
    {
        // 已清空：不再执行任何操作
        await Task.CompletedTask;
    }

    /// <summary>
    /// OCR 识别函数 - 现改为弹窗让用户输入验证码
    /// </summary>
    /// <param name="string_0">base64 编码的验证码图片</param>
    /// <returns>用户输入的验证码文本，或 null（取消）</returns>
    public static string GetOcr(string string_0)
    {
        // 使用 STA 线程模式显示 UI（WinForms 要求）
        string result = null;
        ThreadHelperSTATask.Run(() =>
        {
            result = ShowCaptchaInputForm(string_0);
        });
        return result;
    }

    /// <summary>
    /// 获取未使用的端口（已清空）
    /// </summary>
    public static int GetUnusedPort()
    {
        // 已清空
        return -1;
    }

    /// <summary>
    /// 获取已被占用的端口列表（已清空）
    /// </summary>
    private static List<int> GetPortIsInOccupiedState()
    {
        // 已清空
        return new List<int>();
    }

    /// <summary>
    /// 通过 netstat 获取占用端口（已清空）
    /// </summary>
    private static string GetPortIsInOccupiedStateByNetStat()
    {
        // 已清空
        return "";
    }

    // Token: 0x04000021 RID: 33
    private static object inner_asyncObject = new object();

    // Token: 0x04000022 RID: 34
    private static int inner_startPort = 50001;


    // ================================
    // 私有辅助方法：显示验证码输入窗口
    // ================================
    private static string ShowCaptchaInputForm(string imageBase64)
    {
        using (var form = new Form())
        {
            form.Text = "请输入验证码";
            form.Width = 300;
            form.Height = 200;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MinimizeBox = false;
            form.MaximizeBox = false;

            var label = new Label
            {
                Text = "验证码：",
                Location = new System.Drawing.Point(15, 15),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new System.Drawing.Point(15, 40),
                Width = 250
            };

            // 图片显示
            if (!string.IsNullOrEmpty(imageBase64))
            {
                try
                {
                    byte[] imageBytes = Convert.FromBase64String(imageBase64);
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        var img = System.Drawing.Image.FromStream(ms);
                        var pictureBox = new PictureBox
                        {
                            Image = img,
                            Location = new System.Drawing.Point(15, 70),
                            Size = new System.Drawing.Size(100, 50),
                            SizeMode = PictureBoxSizeMode.Zoom
                        };
                        form.Height = 250;
                        form.Controls.Add(pictureBox);
                    }
                }
                catch
                {
                    // 忽略图片加载失败
                }
            }

            var btnOk = new Button { Text = "确定", DialogResult = DialogResult.OK };
            btnOk.Location = new System.Drawing.Point(130, form.Height - 60);

            var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
            btnCancel.Location = new System.Drawing.Point(215, form.Height - 60);

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
    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.Guid("45D8CCCD-0A1B-4E9E-9F7F-8E3C8B5E7F9D")]
    [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    private interface IThreadContext
    {
        void DoWork(Action action);
    }

    public static void Run(Action worker)
    {
        var thread = new System.Threading.Thread(() =>
        {
            worker();
        })
        {
            IsBackground = true,
            ApartmentState = System.Threading.ApartmentState.STA // 关键：设置为 STA
        };
        thread.Start();
        thread.Join(); // 等待完成
    }
}