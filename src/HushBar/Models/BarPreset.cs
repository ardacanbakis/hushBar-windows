using System.Drawing;
using System.Text.Json.Serialization;

namespace HushBar.Models;

public sealed class BarPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "On Air";
    public string OnText { get; set; } = "ON AIR";
    public string OffText { get; set; } = "Hush";

    public int OnColorArgb { get; set; } = Color.FromArgb(255, 255, 59, 48).ToArgb();    // red badge
    public int OnTextColorArgb { get; set; } = Color.White.ToArgb();
    public int OffColorArgb { get; set; } = Color.FromArgb(255, 142, 142, 147).ToArgb();  // gray badge
    public int OffTextColorArgb { get; set; } = Color.White.ToArgb();

    [JsonIgnore] public Color OnColor => Color.FromArgb(OnColorArgb);
    [JsonIgnore] public Color OnTextColor => Color.FromArgb(OnTextColorArgb);
    [JsonIgnore] public Color OffColor => Color.FromArgb(OffColorArgb);
    [JsonIgnore] public Color OffTextColor => Color.FromArgb(OffTextColorArgb);

    public string Tooltip(bool muted) => $"{Name}: {(muted ? OffText : OnText)}";

    public BarPreset Clone() => new()
    {
        Name = Name + " Copy",
        OnText = OnText,
        OffText = OffText,
        OnColorArgb = OnColorArgb,
        OnTextColorArgb = OnTextColorArgb,
        OffColorArgb = OffColorArgb,
        OffTextColorArgb = OffTextColorArgb,
    };

    public static List<BarPreset> Defaults() => new()
    {
        new() { Name = "ON AIR / Hush",       OnText = "ON AIR",     OffText = "Hush",    OnColorArgb = Color.FromArgb(255, 255, 59, 48).ToArgb(),   OffColorArgb = Color.FromArgb(255, 142, 142, 147).ToArgb() },
        new() { Name = "LIVE / Hushed",        OnText = "LIVE",       OffText = "Hushed",  OnColorArgb = Color.FromArgb(255, 52, 199, 89).ToArgb(),   OffColorArgb = Color.FromArgb(255, 142, 142, 147).ToArgb() },
        new() { Name = "REC / Hush",           OnText = "REC",        OffText = "Hush",    OnColorArgb = Color.FromArgb(255, 255, 59, 48).ToArgb(),   OffColorArgb = Color.FromArgb(255, 142, 142, 147).ToArgb() },
        new() { Name = "BROADCAST",            OnText = "BROADCAST",  OffText = "Off Air", OnColorArgb = Color.FromArgb(255, 175, 82, 222).ToArgb(),  OffColorArgb = Color.FromArgb(255, 142, 142, 147).ToArgb() },
        new() { Name = "In Meeting / Hush",    OnText = "In Meeting", OffText = "Hush",    OnColorArgb = Color.FromArgb(255, 0, 122, 255).ToArgb(),   OffColorArgb = Color.FromArgb(255, 142, 142, 147).ToArgb() },
        new() { Name = "ON CALL / Hushed",     OnText = "ON CALL",    OffText = "Hushed",  OnColorArgb = Color.FromArgb(255, 52, 199, 89).ToArgb(),   OffColorArgb = Color.FromArgb(255, 142, 142, 147).ToArgb() },
        new() { Name = "STREAMING",            OnText = "STREAMING",  OffText = "Offline",  OnColorArgb = Color.FromArgb(255, 175, 82, 222).ToArgb(), OffColorArgb = Color.FromArgb(255, 142, 142, 147).ToArgb() },
        new() { Name = "GAMING / Hushed",      OnText = "GAMING",     OffText = "Hushed",  OnColorArgb = Color.FromArgb(255, 52, 199, 89).ToArgb(),   OffColorArgb = Color.FromArgb(255, 142, 142, 147).ToArgb() },
        new() { Name = "ON / Hush",            OnText = "ON",         OffText = "Hush",    OnColorArgb = Color.FromArgb(255, 72, 72, 74).ToArgb(),    OffColorArgb = Color.FromArgb(255, 142, 142, 147).ToArgb(),
                OnTextColorArgb = Color.White.ToArgb(), OffTextColorArgb = Color.White.ToArgb() },
    };
}
