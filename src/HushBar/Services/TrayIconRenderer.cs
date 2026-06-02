using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace HushBar.Services;

/// <summary>
/// Renders tray icons dynamically with GDI+.
/// Supports 10 icon styles and 5 mute overlay styles.
/// </summary>
public static class TrayIconRenderer
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static readonly string[] IconStyles =
    {
        "Capsule",       // classic rounded mic body
        "MicStand",      // mic on a stand with base
        "Condenser",     // studio condenser mic
        "Vintage",       // ball-top broadcast mic
        "Waves",         // mic with sound wave arcs
        "Headset",       // headset with mic arm
        "RadioTower",    // signal tower
        "Speaker",       // speaker cone
        "Waveform",      // audio bar waveform
        "Bullhorn",      // megaphone/bullhorn
        "Custom",        // user-provided icon file
    };

    public static readonly string[] MuteStyles =
    {
        "DiagonalSlash",   // red diagonal line
        "XOverlay",        // red X in corner
        "RedDot",          // red dot bottom-right
        "CrossedCircle",   // circle+bar
        "GrayedOut",       // no overlay, just gray
    };

    public static Icon Render(bool muted, Color onColor, Color offColor,
                              string iconStyle = "Capsule", string muteStyle = "DiagonalSlash",
                              string? customIconPath = null)
    {
        if (iconStyle == "Custom" && !string.IsNullOrEmpty(customIconPath))
        {
            try
            {
                var loaded = LoadCustomIcon(customIconPath, muted, offColor, muteStyle);
                if (loaded is not null) return loaded;
            }
            catch { }
        }

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

    private static void DrawIcon(Graphics g, string style, Color color, int size)
    {
        using var fill = new SolidBrush(color);
        using var pen = new Pen(color, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float cx = size / 2f;

        switch (style)
        {
            case "MicStand":   DrawMicStand(g, fill, pen, color, cx, size);   break;
            case "Condenser":  DrawCondenser(g, fill, pen, color, cx, size);  break;
            case "Vintage":    DrawVintage(g, fill, pen, color, cx, size);    break;
            case "Waves":      DrawWaves(g, fill, pen, color, cx, size);      break;
            case "Headset":    DrawHeadset(g, fill, pen, color, cx, size);    break;
            case "RadioTower": DrawRadioTower(g, fill, pen, color, cx, size); break;
            case "Speaker":    DrawSpeaker(g, fill, pen, color, cx, size);    break;
            case "Waveform":   DrawWaveform(g, fill, color, cx, size);        break;
            case "Bullhorn":   DrawBullhorn(g, fill, pen, color, cx, size);   break;
            default:           DrawCapsule(g, fill, pen, color, cx, size);    break;
        }
    }

    // ── Icon drawing methods ──────────────────────────────────────────────

    private static void DrawCapsule(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        float bw = 11, bh = 16, bx = cx - bw / 2f, by = 3f;
        g.FillRoundedRectangle(fill, bx, by, bw, bh, bw / 2f);
        using var arc = new Pen(color, 2.2f);
        g.DrawArc(arc, bx - 3, by + bh - 9, bw + 6, 12, 0, 180);
        g.DrawLine(pen, cx, by + bh + 3, cx, by + bh + 7);
        g.DrawLine(pen, cx - 5, by + bh + 7, cx + 5, by + bh + 7);
    }

    private static void DrawMicStand(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        float bw = 9, bh = 12, bx = cx - bw / 2f, by = 3f;
        g.FillRoundedRectangle(fill, bx, by, bw, bh, bw / 2f);
        using var thickPen = new Pen(color, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(thickPen, cx, by + bh, cx - 4, by + bh + 8);
        g.DrawLine(thickPen, cx - 8, by + bh + 8, cx + 4, by + bh + 8);
        using var ring = new Pen(color, 1.5f);
        g.DrawEllipse(ring, cx - 2, by + bh - 1, 4, 4);
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

    private static void DrawVintage(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        float r = 7f, by = 3f;
        g.FillEllipse(fill, cx - r, by, r * 2, r * 2);
        using var grille = new Pen(Color.FromArgb(60, 0, 0, 0), 1.2f);
        g.DrawEllipse(grille, cx - r + 2, by + 2, (r - 2) * 2, (r - 2) * 2);
        g.DrawLine(pen, cx, by + r * 2, cx, by + r * 2 + 7);
        g.DrawLine(pen, cx - 6, by + r * 2 + 7, cx + 6, by + r * 2 + 7);
    }

    private static void DrawWaves(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        float bw = 9, bh = 13, bx = 4f, by = 3f;
        g.FillRoundedRectangle(fill, bx, by, bw, bh, bw / 2f);
        g.DrawLine(pen, bx + bw / 2f, by + bh + 2, bx + bw / 2f, by + bh + 6);
        g.DrawLine(pen, bx + bw / 2f - 4, by + bh + 6, bx + bw / 2f + 4, by + bh + 6);
        float wx = bx + bw + 2, wy = by + bh / 2f;
        using var wavePen = new Pen(color, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(wavePen, wx, wy - 5, 6, 10, -60, 120);
        g.DrawArc(wavePen, wx + 4, wy - 8, 8, 16, -60, 120);
    }

    private static void DrawHeadset(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        float r = 10f, by = 3f;
        using var arcPen = new Pen(color, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(arcPen, cx - r, by, r * 2, r * 2, 180, 180);
        g.FillEllipse(fill, cx - r - 2, by + r - 3, 7, 7);
        g.FillEllipse(fill, cx + r - 5, by + r - 3, 7, 7);
        using var armPen = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(armPen, cx + r - 2, by + r + 2, cx + r + 3, by + r * 2 + 1);
        g.FillEllipse(fill, cx + r + 1, by + r * 2, 4, 4);
    }

    private static void DrawRadioTower(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        using var thickPen = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(thickPen, cx, 3, cx - 8, size - 4);
        g.DrawLine(thickPen, cx, 3, cx + 8, size - 4);
        g.DrawLine(pen, cx - 3, 12, cx + 3, 12);
        g.DrawLine(pen, cx - 5, 19, cx + 5, 19);
        using var sigPen = new Pen(color, 1.6f);
        g.DrawArc(sigPen, cx - 5, 0, 10, 8, 200, 140);
        g.DrawArc(sigPen, cx - 8, -2, 16, 11, 210, 120);
    }

    private static void DrawSpeaker(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        float bx = 4, by = 9, bw = 10, bh = 14;
        g.FillRectangle(fill, bx, by, bw, bh);
        var cone = new PointF[]
        {
            new(bx + bw, by),
            new(bx + bw + 8, by - 5),
            new(bx + bw + 8, by + bh + 5),
            new(bx + bw, by + bh),
        };
        g.FillPolygon(fill, cone);
        float wx = bx + bw + 9, wy = by + bh / 2f;
        using var wavePen = new Pen(color, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(wavePen, wx, wy - 5, 5, 10, -70, 140);
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

    private static void DrawBullhorn(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        var body = new PointF[] { new(8, 12), new(8, 20), new(17, 20), new(17, 12) };
        g.FillPolygon(fill, body);
        var bell = new PointF[] { new(17, 12), new(26, 6), new(26, 26), new(17, 20) };
        g.FillPolygon(fill, bell);
        using var handlePen = new Pen(color, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(handlePen, 8, 20, 5, 26);
        g.FillEllipse(fill, 27, 14, 3, 4);
    }

    // ── Mute overlays ────────────────────────────────────────────────────

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
                    g.DrawLine(p, 4 + 3, size - 4 - 3, size - 4 - 3, 4 + 3);
                }
                break;

            default: // DiagonalSlash
                using (var p = new Pen(Color.FromArgb(230, 30, 30), 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(p, 5, 5, size - 5, size - 5);
                break;
        }
    }

    // ── Custom icon loading ───────────────────────────────────────────────

    private static Icon? LoadCustomIcon(string path, bool muted, Color offColor, string muteStyle)
    {
        if (!System.IO.File.Exists(path)) return null;

        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".ico")
        {
            var ico = new Icon(path, 32, 32);
            if (!muted || muteStyle == "GrayedOut") return ico;
            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawIcon(ico, 0, 0);
                DrawMuteOverlay(g, muteStyle, 32);
            }
            ico.Dispose();
            return BitmapToIcon(bmp);
        }
        else
        {
            using var src = new Bitmap(path);
            using var bmp = new Bitmap(src, 32, 32);
            if (muted && muteStyle != "GrayedOut")
            {
                using var g = Graphics.FromImage(bmp);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                DrawMuteOverlay(g, muteStyle, 32);
            }
            return BitmapToIcon(bmp);
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
