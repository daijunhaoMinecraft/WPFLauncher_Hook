using System;
using System.Drawing;
using System.Windows.Forms;
using Mcl.Core.DotNetTranstor.Var;
using WPFLauncher.Manager.LanGame;

namespace Mcl.Core.DotNetTranstor.Window
{
    public class ForwarderControlPanel : Form
    {
        public ForwarderControlPanel()
        {
            this.Text = "转发控制台";
            this.Size = new Size(300, 150);
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

            Label lbl = new Label();
            // 修复：显式调用 ToString() 避免 Format 匹配错误
            lbl.Text = string.Format("模式: {0}\n状态: 运行中...", WebRtcVar.Mode.ToString());
            lbl.Location = new Point(20, 20);
            lbl.Size = new Size(250, 40);

            Button btn = new Button() { Text = "停止转发", Location = new Point(100, 70)};
            btn.Click += (s, e) => { 
                WebRtcVar.AitFunction.axy.t(); };
            // Button btnReset = new Button() { Text = "重置 Zlib 字典", Location = new Point(100, 110), Size = new Size(100, 25) };
            // btnReset.Click += (s, e) => {
            //     foreach (var ctx in WebRtcVar.Contexts.Values) ctx.Compressor.c();
            //     WebRtcVar.Contexts.Clear();
            //     Console.WriteLine("[System] Zlib 字典已手动重置。");
            // };
            // this.Controls.Add(btnReset);

            this.Controls.Add(lbl);
            this.Controls.Add(btn);
            this.FormClosing += (s, e) => WebRtcVar.AitFunction.axy.t();
        }
    }
}