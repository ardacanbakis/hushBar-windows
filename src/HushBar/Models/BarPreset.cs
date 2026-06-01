using System.Drawing;
using System.Text.Json.Serialization;

namespace HushBar.Models;

/// <summary>
/// A named look for the tray icon + tooltip. The colors tint the dynamically
/// rendered tray icon; the labels populate the tooltip (the Windows tray can't
/// show a wide text pill like the macOS menu bar).
/// </summary>
public sealed class BarPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "On Air";
    public string OnText { get; set; } = "Live";
    public string OffText { get; set; } = "Muted";

    // Stored as ARGB ints for easy JSON round-tripping.
    public int OnColorArgb { get; set; } = Color.FromArgb(255, 52, 199, 89).ToArgb();   // green
    public int OffColorArgb { get; set; } = Color.FromArgb(255, 142, 142, 147).ToArgb(); // gray

    [JsonIgnore] public Color OnColor => Color.FromArgb(OnColorArgb);
    [JsonIgnore] public Color OffColor => Color.FromArgb(OffColorArgb);

    public string Tooltip(bool muted) => $"{Name}: {(muted ? OffText : OnText)}";
}
