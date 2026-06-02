using System.IO;
using System.Text.Json;

namespace HushBar.Models;

public sealed class AppSettings
{
    public List<BarPreset> Presets { get; set; } = BarPreset.Defaults();

    public Guid SelectedPresetId { get; set; }
    public bool PlaySoundOnToggle { get; set; } = true;

    public uint HotKeyModifiers { get; set; } = 0x4006; // Ctrl+Shift+NoRepeat
    public uint HotKeyVk { get; set; } = 0x4D;          // M

    public string IconStyle { get; set; } = "Capsule";
    public string MuteStyle { get; set; } = "DiagonalSlash";
    public string? CustomIconPath { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public BarPreset SelectedPreset =>
        Presets.FirstOrDefault(p => p.Id == SelectedPresetId) ?? Presets[0];

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HushBar");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded is { Presets.Count: > 0 })
                {
                    if (loaded.SelectedPresetId == Guid.Empty)
                        loaded.SelectedPresetId = loaded.Presets[0].Id;
                    return loaded;
                }
            }
        }
        catch (Exception ex) { HushBar.Services.HushLog.Write($"settings load failed: {ex.Message}"); }

        var fresh = new AppSettings();
        fresh.SelectedPresetId = fresh.Presets[0].Id;
        return fresh;
    }

    public void Save()
    {
        try { File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOpts)); }
        catch (Exception ex) { HushBar.Services.HushLog.Write($"settings save failed: {ex.Message}"); }
    }
}
