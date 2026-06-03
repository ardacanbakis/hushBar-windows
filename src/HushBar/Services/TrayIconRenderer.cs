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
        "Capsule", "MicStand", "Condenser", "Vintage", "Waves",
        "Headset", "RadioTower", "Speaker", "Waveform", "Bullhorn",
        "HandMic", "Lavalier", "RibbonMic", "RecordDot", "MicMute",
        "Equalizer", "MusicalNote", "TuningFork", "SatelliteDish",
        "Phone", "Walkie", "PodcastMic", "TallyLight", "SilenceSymbol",
        "VinylRecord", "Knob", "Bell", "Antenna",
        "Custom",
    };

    public static readonly string[] MuteStyles =
    {
        "DiagonalSlash", "XOverlay", "RedDot", "CrossedCircle", "GrayedOut",
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
            case "MicStand":      DrawMicStand(g, fill, pen, color, cx, size); break;
            case "Condenser":     DrawCondenser(g, fill, pen, color, cx, size); break;
            case "Vintage":       DrawVintage(g, fill, pen, color, cx, size); break;
            case "Waves":         DrawWaves(g, fill, pen, color, cx, size); break;
            case "Headset":       DrawHeadset(g, fill, pen, color, cx, size); break;
            case "RadioTower":    DrawRadioTower(g, fill, pen, color, cx, size); break;
            case "Speaker":       DrawSpeaker(g, fill, pen, color, cx, size); break;
            case "Waveform":      DrawWaveform(g, fill, color, cx, size); break;
            case "Bullhorn":      DrawBullhorn(g, fill, pen, color, cx, size); break;
            case "HandMic":       DrawHandMic(g, fill, pen, color, cx, size); break;
            case "Lavalier":      DrawLavalier(g, fill, pen, color, cx, size); break;
            case "RibbonMic":     DrawRibbonMic(g, fill, pen, color, cx, size); break;
            case "RecordDot":     DrawRecordDot(g, fill, color, cx, size); break;
            case "MicMute":       DrawMicMute(g, fill, pen, color, cx, size); break;
            case "Equalizer":     DrawEqualizer(g, fill, color, cx, size); break;
            case "MusicalNote":   DrawMusicalNote(g, fill, pen, color, cx, size); break;
            case "TuningFork":    DrawTuningFork(g, fill, pen, color, cx, size); break;
            case "SatelliteDish": DrawSatelliteDish(g, fill, pen, color, cx, size); break;
            case "Phone":         DrawPhone(g, fill, pen, color, cx, size); break;
            case "Walkie":        DrawWalkie(g, fill, pen, color, cx, size); break;
            case "PodcastMic":    DrawPodcastMic(g, fill, pen, color, cx, size); break;
            case "TallyLight":    DrawTallyLight(g, fill, color, cx, size); break;
            case "SilenceSymbol": DrawSilenceSymbol(g, fill, color, cx, size); break;
            case "VinylRecord":   DrawVinylRecord(g, fill, pen, color, cx, size); break;
            case "Knob":          DrawKnob(g, fill, pen, color, cx, size); break;
            case "Bell":          DrawBell(g, fill, pen, color, cx, size); break;
            case "Antenna":       DrawAntenna(g, fill, pen, color, cx, size); break;
            default:              DrawCapsule(g, fill, pen, color, cx, size); break;
        }
    }

    // ── Original 10 icons ─────────────────────────────────────────────────────────

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
        var cone = new PointF[] { new(bx + bw, by), new(bx + bw + 8, by - 5), new(bx + bw + 8, by + bh + 5), new(bx + bw, by + bh) };
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

    // ── New 18 icons ──────────────────────────────────────────────────────────

    private static void DrawHandMic(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        // Ball head
        g.FillEllipse(fill, cx - 5, 2, 10, 10);
        // Cylindrical grip
        g.FillRoundedRectangle(fill, cx - 4, 11, 8, 16, 2f);
        // Grille dots
        using var dark = new Pen(Color.FromArgb(60, 0, 0, 0), 1f);
        g.DrawLine(dark, cx - 3, 5, cx + 3, 5);
        g.DrawLine(dark, cx - 3, 8, cx + 3, 8);
    }

    private static void DrawLavalier(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        // Clip body
        g.FillRoundedRectangle(fill, cx - 3, 4, 6, 12, 2f);
        // Clip top
        using var clipPen = new Pen(color, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(clipPen, cx - 5, 4, cx + 5, 4);
        // Wire going down
        g.DrawLine(pen, cx, 16, cx, 24);
        // Small connector dot
        g.FillEllipse(fill, cx - 2, 24, 4, 4);
    }

    private static void DrawRibbonMic(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        // Tall rectangular body
        float bw = 10, bh = 18, bx = cx - bw / 2f, by = 2f;
        g.FillRoundedRectangle(fill, bx, by, bw, bh, 2f);
        // Horizontal ribbon lines
        using var linePen = new Pen(Color.FromArgb(70, 0, 0, 0), 1f);
        for (int i = 1; i <= 4; i++)
            g.DrawLine(linePen, bx + 2, by + i * 3.2f, bx + bw - 2, by + i * 3.2f);
        // Short stem
        g.DrawLine(pen, cx, by + bh + 1, cx, by + bh + 5);
        g.DrawLine(pen, cx - 4, by + bh + 5, cx + 4, by + bh + 5);
    }

    private static void DrawRecordDot(Graphics g, SolidBrush fill, Color color, float cx, int size)
    {
        float cy = size / 2f;
        // Outer ring
        using var ringPen = new Pen(color, 2.2f);
        g.DrawEllipse(ringPen, cx - 11, cy - 11, 22, 22);
        // Inner filled circle
        g.FillEllipse(fill, cx - 6, cy - 6, 12, 12);
    }

    private static void DrawMicMute(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        // Capsule mic
        float bw = 11, bh = 16, bx = cx - bw / 2f, by = 3f;
        g.FillRoundedRectangle(fill, bx, by, bw, bh, bw / 2f);
        using var arc = new Pen(color, 2.2f);
        g.DrawArc(arc, bx - 3, by + bh - 9, bw + 6, 12, 0, 180);
        g.DrawLine(pen, cx, by + bh + 3, cx, by + bh + 7);
        g.DrawLine(pen, cx - 5, by + bh + 7, cx + 5, by + bh + 7);
        // Built-in diagonal slash
        using var slash = new Pen(Color.FromArgb(80, 0, 0, 0), 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(slash, 6, 6, size - 6, size - 6);
    }

    private static void DrawEqualizer(Graphics g, SolidBrush fill, Color color, float cx, int size)
    {
        float[] heights = { 12, 20, 8, 16, 22, 10, 18 };
        float barW = 3f, gap = 1.2f;
        float totalW = heights.Length * barW + (heights.Length - 1) * gap;
        float startX = cx - totalW / 2f;
        for (int i = 0; i < heights.Length; i++)
        {
            float h = heights[i];
            float bx = startX + i * (barW + gap);
            float by = size - 4 - h;
            g.FillRectangle(fill, bx, by, barW, h);
        }
    }

    private static void DrawMusicalNote(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        // Note head (filled oval, angled)
        g.FillEllipse(fill, cx - 6, size - 12, 10, 7);
        // Stem
        using var stemPen = new Pen(color, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(stemPen, cx + 3, size - 9, cx + 3, 4);
        // Flag
        g.DrawArc(stemPen, cx + 3, 4, 8, 10, -90, 120);
    }

    private static void DrawTuningFork(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        using var forkPen = new Pen(color, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        // Left prong
        g.DrawLine(forkPen, cx - 5, 3, cx - 5, 15);
        // Right prong
        g.DrawLine(forkPen, cx + 5, 3, cx + 5, 15);
        // Curved base connecting prongs
        g.DrawArc(forkPen, cx - 5, 10, 10, 10, 0, 180);
        // Handle
        g.DrawLine(forkPen, cx, 20, cx, size - 3);
    }

    private static void DrawSatelliteDish(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        // Dish (arc)
        using var dishPen = new Pen(color, 2.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(dishPen, 3, 3, 22, 22, 180, 90);
        // Arm from dish to center
        g.DrawLine(pen, 14, 14, 22, 6);
        // Feed point
        g.FillEllipse(fill, 21, 4, 5, 5);
        // Signal waves
        using var sigPen = new Pen(color, 1.4f);
        g.DrawArc(sigPen, 22, 2, 5, 5, -60, 120);
        g.DrawArc(sigPen, 24, 0, 7, 7, -60, 120);
        // Base/mount
        g.DrawLine(pen, 6, 20, 6, size - 3);
        g.DrawLine(pen, 3, size - 3, 9, size - 3);
    }

    private static void DrawPhone(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        // Classic handset shape using arcs
        using var handsetPen = new Pen(color, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        // Earpiece
        g.FillRoundedRectangle(fill, cx - 8, 4, 7, 8, 3f);
        // Mouthpiece
        g.FillRoundedRectangle(fill, cx + 1, 18, 7, 8, 3f);
        // Handle curve connecting them
        g.DrawArc(handsetPen, cx - 6, 6, 12, 18, -60, -60);
    }

    private static void DrawWalkie(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        // Body
        g.FillRoundedRectangle(fill, cx - 6, 8, 12, 20, 2f);
        // Antenna
        using var antPen = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(antPen, cx + 3, 8, cx + 3, 2);
        // Screen area
        using var dark = new SolidBrush(Color.FromArgb(60, 0, 0, 0));
        g.FillRectangle(dark, cx - 4, 11, 8, 5);
        // Side button
        g.FillRectangle(fill, cx - 8, 14, 2, 5);
        // Speaker dots
        using var dotPen = new Pen(Color.FromArgb(60, 0, 0, 0), 1f);
        g.DrawLine(dotPen, cx - 3, 20, cx + 3, 20);
        g.DrawLine(dotPen, cx - 3, 22, cx + 3, 22);
        g.DrawLine(dotPen, cx - 3, 24, cx + 3, 24);
    }

    private static void DrawPodcastMic(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        // Boom arm (L shape from left)
        using var armPen = new Pen(color, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(armPen, 3, size - 4, 3, 10);
        g.DrawLine(armPen, 3, 10, cx, 10);
        // Mic capsule at end of arm
        g.FillEllipse(fill, cx - 1, 4, 10, 12);
        // Shock mount ring
        using var ringPen = new Pen(color, 1.5f);
        g.DrawEllipse(ringPen, cx, 6, 8, 8);
    }

    private static void DrawTallyLight(Graphics g, SolidBrush fill, Color color, float cx, int size)
    {
        float cy = size / 2f;
        // Large filled circle
        g.FillEllipse(fill, cx - 8, cy - 8, 16, 16);
        // Radiating dashes
        using var dashPen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float r1 = 11, r2 = 14;
        for (int i = 0; i < 8; i++)
        {
            double a = i * Math.PI / 4;
            g.DrawLine(dashPen,
                cx + (float)(Math.Cos(a) * r1), cy + (float)(Math.Sin(a) * r1),
                cx + (float)(Math.Cos(a) * r2), cy + (float)(Math.Sin(a) * r2));
        }
    }

    private static void DrawSilenceSymbol(Graphics g, SolidBrush fill, Color color, float cx, int size)
    {
        float cy = size / 2f, r = 12;
        // Circle
        using var ringPen = new Pen(color, 2.4f);
        g.DrawEllipse(ringPen, cx - r, cy - r, r * 2, r * 2);
        // Horizontal bar through center
        using var barPen = new Pen(color, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(barPen, cx - r + 3, cy, cx + r - 3, cy);
    }

    private static void DrawVinylRecord(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        float cy = size / 2f, r = 13;
        // Outer disc
        using var discPen = new Pen(color, 2f);
        g.DrawEllipse(discPen, cx - r, cy - r, r * 2, r * 2);
        // Groove rings
        using var groovePen = new Pen(color, 0.8f);
        g.DrawEllipse(groovePen, cx - 9, cy - 9, 18, 18);
        g.DrawEllipse(groovePen, cx - 6, cy - 6, 12, 12);
        // Center hole
        g.FillEllipse(fill, cx - 2, cy - 2, 4, 4);
    }

    private static void DrawKnob(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        float cy = size / 2f, r = 11;
        // Outer ring
        using var ringPen = new Pen(color, 2.2f);
        g.DrawEllipse(ringPen, cx - r, cy - r, r * 2, r * 2);
        // Inner filled circle
        g.FillEllipse(fill, cx - 7, cy - 7, 14, 14);
        // Indicator line pointing up
        using var indPen = new Pen(Color.FromArgb(60, 0, 0, 0), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(indPen, cx, cy - 7, cx, cy - 2);
    }

    private static void DrawBell(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        // Bell body
        var bellPath = new PointF[]
        {
            new(cx - 10, size - 8),
            new(cx - 7, 10),
            new(cx - 3, 5),
            new(cx + 3, 5),
            new(cx + 7, 10),
            new(cx + 10, size - 8),
        };
        g.FillPolygon(fill, bellPath);
        // Rim
        using var rimPen = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(rimPen, cx - 12, size - 8, cx + 12, size - 8);
        // Clapper
        g.FillEllipse(fill, cx - 2, size - 6, 4, 4);
        // Top nub
        g.FillEllipse(fill, cx - 2, 2, 4, 4);
    }

    private static void DrawAntenna(Graphics g, SolidBrush fill, Pen pen, Color color, float cx, int size)
    {
        // Vertical rod
        using var rodPen = new Pen(color, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(rodPen, cx, 3, cx, size - 4);
        // Horizontal elements (dipole)
        g.DrawLine(pen, cx - 8, 10, cx + 8, 10);
        g.DrawLine(pen, cx - 6, 16, cx + 6, 16);
        g.DrawLine(pen, cx - 4, 22, cx + 4, 22);
        // Tip
        g.FillEllipse(fill, cx - 2, 1, 4, 4);
        // Base
        g.DrawLine(rodPen, cx - 5, size - 4, cx + 5, size - 4);
    }

    // ── Mute overlays ────────────────────────────────────────────────────────

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

    // ── Custom icon loading ──────────────────────────────────────────────────

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
