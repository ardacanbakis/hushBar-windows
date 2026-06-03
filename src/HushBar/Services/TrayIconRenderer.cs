using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace HushBar.Services;

public static class TrayIconRenderer
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static readonly string[] IconStyles =
    {
        "Waveform", "Capsule", "Condenser", "MicMute", "RecordDot",
    };

    public static Icon Render(bool muted, Color onColor, Color offColor,
                              string iconStyle = "RecordDot", string muteStyle = "DiagonalSlash",
                              string? customIconPath = null)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var drawColor = (muted && muteStyle == "GrayedOut") ? offColor
                           : (muted ? offColor : onColor);

            DrawIcon(g, iconStyle, drawColor, size);

            if (muted && muteStyle != "GrayedOut")
                DrawMuteOverlay(g, muteStyle, size);
        }

        return BitmapToIcon(bmp);
    }

    public static Bitmap RenderPreview(string iconStyle, Color color, int size = 32)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        DrawIcon(g, iconStyle, color, size);
        return bmp;
    }

    private static void DrawIcon(Graphics g, string style, Color color, int size)
    {
        using var fill = new SolidBrush(color);
        using var pen = new Pen(color, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float cx = size / 2f;

        switch (style)
        {
            case "Condenser":     DrawCondenser(g, fill, pen, color, cx, size); break;
            case "Waveform":      DrawWaveform(g, fill, color, cx, size); break;
            case "RecordDot":     DrawRecordDot(g, fill, color, cx, size); break;
            case "MicMute":       DrawMicMute(g, fill, pen, color, cx, size); break;
            default:              DrawCapsule(g, fill, pen, color, cx, size); break;
        }
    }

    private static void DrawCapsule(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        float bw = 11, bh = 16, bx = cx - bw / 2f, by = 3f;
        g.FillRoundedRectangle(fill, bx, by, bw, bh, bw / 2f);
        using var arc = new Pen(color, 2.2f);
        g.DrawArc(arc, bx - 3, by + bh - 9, bw + 6, 12, 0, 180);
        g.DrawLine(pen, cx, by + bh + 3, cx, by + bh + 7);
        g.DrawLine(pen, cx - 5, by + bh + 7, cx + 5, by + bh + 7);
    }

    private static void DrawCondenser(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        float bw = 13, bh = 14, bx = cx - bw / 2f, by = 2f;
        g.FillRoundedRectangle(fill, bx, by, bw, bh, 3f);
        using var linePen = new Pen(Color.FromArgb(80, 0, 0, 0), 1f);
        for (int i = 1; i <= 3; i++)
            g.DrawLine(linePen, bx + 2, by + i * 3, bx + bw - 2, by + i * 3);
        using var collar = new Pen(color, 2f);
        g.DrawLine(collar, bx - 1, by + bh, bx + bw + 1, by + bh);
        g.DrawLine(pen, cx, by + bh + 1, cx, by + bh + 7);
        g.DrawLine(pen, cx - 5, by + bh + 7, cx + 5, by + bh + 7);
    }

    private static void DrawWaveform(Graphics g, SolidBrush fill, Color color, float cx, int size)
    {
        float[] heights = { 8, 14, 20, 14, 8 };
        float barW = 4f, gap = 2f;
        float totalW = heights.Length * barW + (heights.Length - 1) * gap;
        float startX = cx - totalW / 2f;
        for (int i = 0; i < heights.Length; i++)
        {
            float h = heights[i];
            float bx = startX + i * (barW + gap);
            float by = (size - h) / 2f;
            g.FillRoundedRectangle(fill, bx, by, barW, h, 2f);
        }
    }

    private static void DrawRecordDot(Graphics g, SolidBrush fill, Color color, float cx, int size)
    {
        float cy = size / 2f;
        using var ringPen = new Pen(color, 2.2f);
        g.DrawEllipse(ringPen, cx - 11, cy - 11, 22, 22);
        g.FillEllipse(fill, cx - 6, cy - 6, 12, 12);
    }

    private static void DrawMicMute(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        float bw = 11, bh = 16, bx = cx - bw / 2f, by = 3f;
        g.FillRoundedRectangle(fill, bx, by, bw, bh, bw / 2f);
        using var arc = new Pen(color, 2.2f);
        g.DrawArc(arc, bx - 3, by + bh - 9, bw + 6, 12, 0, 180);
        g.DrawLine(pen, cx, by + bh + 3, cx, by + bh + 7);
        g.DrawLine(pen, cx - 5, by + bh + 7, cx + 5, by + bh + 7);
        using var slash = new Pen(Color.FromArgb(80, 0, 0, 0), 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(slash, 6, 6, size - 6, size - 6);
    }

    // ── Mute overlays ────────────────────────────────────────────

    private static void DrawMuteOverlay(Graphics g, string style, int size)
    {
        switch (style)
        {
            case "XOverlay":
                using (var p = new Pen(Color.FromArgb(230, 30, 30), 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(p, size - 11, size - 11, size - 3, size - 3);
                    g.DrawLine(p, size - 3, size - 11, size - 11, size - 3);
                }
                break;
            case "RedDot":
                g.FillEllipse(new SolidBrush(Color.FromArgb(230, 30, 30)), size - 10, size - 10, 9, 9);
                break;
            case "CrossedCircle":
                using (var p = new Pen(Color.FromArgb(230, 30, 30), 2.2f))
                {
                    g.DrawEllipse(p, 4, 4, size - 8, size - 8);
                    g.DrawLine(p, 7, size - 7, size - 7, 7);
                }
                break;
            default:
                using (var p = new Pen(Color.FromArgb(230, 30, 30), 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(p, 5, 5, size - 5, size - 5);
                break;
        }
    }

    private static Icon BitmapToIcon(Bitmap bmp)
    {
        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally { DestroyIcon(hIcon); }
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
