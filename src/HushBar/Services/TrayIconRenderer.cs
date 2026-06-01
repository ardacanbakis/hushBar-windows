using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace HushBar.Services;

/// <summary>
/// Renders the tray icon dynamically with GDI+. Unlike the macOS menu bar (which
/// can show an arbitrary-width text pill), the Windows tray is a fixed-size icon,
/// so we draw a mic glyph tinted with the preset color, with a slash when muted.
/// The preset's label text goes in the NotifyIcon tooltip instead.
/// </summary>
public static class TrayIconRenderer
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    /// <summary>
    /// Builds a 32x32 icon. Caller owns the returned Icon and must Dispose it.
    /// </summary>
    public static Icon Render(bool muted, Color onColor, Color offColor)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var color = muted ? offColor : onColor;
            using var fill = new SolidBrush(color);
            using var pen = new Pen(color, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            // Mic body (capsule)
            float bw = 11, bh = 16;
            float bx = (size - bw) / 2f, by = 4;
            g.FillRoundedRectangle(fill, bx, by, bw, bh, bw / 2f);

            // U-collar
            using var arcPen = new Pen(color, 2.4f);
            g.DrawArc(arcPen, bx - 3, by + bh - 9, bw + 6, 12, 0, 180);

            // Stem + base
            float cx = size / 2f;
            g.DrawLine(pen, cx, by + bh + 3, cx, by + bh + 8);
            g.DrawLine(pen, cx - 5, by + bh + 8, cx + 5, by + bh + 8);

            // Red diagonal slash when muted
            if (muted)
            {
                using var slash = new Pen(Color.FromArgb(235, 30, 30), 2.6f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(slash, 6, 6, size - 6, size - 6);
            }
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            // Clone into a managed Icon so we can free the GDI handle immediately.
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(
        this Graphics g, Brush brush, float x, float y, float w, float h, float radius)
    {
        using var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
