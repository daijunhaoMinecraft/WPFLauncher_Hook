using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Mcl.Core.Dotnetdetour.UI.Core;

public static class UITheme
{
    public static readonly Color PrimaryBlue = Color.FromArgb(0, 95, 184); // WinUI 3 Blue
    public static readonly Color PrimaryHover = Color.FromArgb(25, 117, 210);
    public static readonly Color PrimaryPressed = Color.FromArgb(0, 76, 153);
    
    public static readonly Color Background = Color.FromArgb(243, 243, 243);
    public static readonly Color Surface = Color.White;
    public static readonly Color Border = Color.FromArgb(229, 229, 229);
    public static readonly Color TextMain = Color.FromArgb(28, 28, 28);
    public static readonly Color TextSecondary = Color.FromArgb(96, 96, 96);
    public static readonly Color Danger = Color.FromArgb(196, 43, 28);

    public static readonly Font MainFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
    public static readonly Font BoldFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font TitleFont = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);

    public static void ApplyModernForm(Form form, string title, int width, int height)
    {
        form.Text = title;
        form.Size = new Size(width, height);
        form.BackColor = Background;
        form.ForeColor = TextMain;
        form.Font = MainFont;
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ShowIcon = false;
    }
}

public class ModernButton : Button
{
    public bool IsPrimary { get; set; }

    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = UITheme.MainFont;
        Cursor = Cursors.Hand;
        Size = new Size(100, 32);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        base.OnPaint(pevent);
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Color bgColor = IsPrimary ? UITheme.PrimaryBlue : UITheme.Surface;
        Color textColor = IsPrimary ? Color.White : UITheme.TextMain;

        if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
        {
            bgColor = IsPrimary ? UITheme.PrimaryHover : Color.FromArgb(245, 245, 245);
        }

        pevent.Graphics.Clear(Parent.BackColor);

        using (GraphicsPath path = GetRoundedPath(ClientRectangle, 4))
        {
            using (SolidBrush brush = new SolidBrush(bgColor))
            {
                pevent.Graphics.FillPath(brush, path);
            }
            if (!IsPrimary)
            {
                using (Pen pen = new Pen(UITheme.Border, 1))
                {
                    pevent.Graphics.DrawPath(pen, path);
                }
            }
        }

        TextRenderer.DrawText(pevent.Graphics, Text, Font, ClientRectangle, textColor, 
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        GraphicsPath path = new GraphicsPath();
        float r2 = radius / 2f;
        path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
        path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
        path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        return path;
    }
}