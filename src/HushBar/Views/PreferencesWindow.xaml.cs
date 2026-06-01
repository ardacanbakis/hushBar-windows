using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HushBar.Models;
using HushBar.Services;
using DrawingColor = System.Drawing.Color;

namespace HushBar;

public partial class PreferencesWindow : Window
{
    private readonly AppSettings _settings;
    private readonly MicMuteService? _mic;
    private bool _loading;

    private static readonly DrawingColor[] Palette =
    {
        DrawingColor.FromArgb(255, 52, 199, 89),   // green
        DrawingColor.FromArgb(255, 0, 122, 255),    // blue
        DrawingColor.FromArgb(255, 255, 59, 48),    // red
        DrawingColor.FromArgb(255, 255, 149, 0),    // orange
        DrawingColor.FromArgb(255, 175, 82, 222),   // purple
        DrawingColor.FromArgb(255, 255, 204, 0),    // yellow
        DrawingColor.FromArgb(255, 90, 200, 250),   // cyan
        DrawingColor.FromArgb(255, 255, 45, 85),    // pink
        DrawingColor.FromArgb(255, 88, 86, 214),    // indigo
        DrawingColor.FromArgb(255, 0, 199, 190),    // teal
        DrawingColor.FromArgb(255, 142, 142, 147),  // gray
        DrawingColor.FromArgb(255, 72, 72, 74),     // dark gray
    };

    public PreferencesWindow(AppSettings settings, MicMuteService? mic = null)
    {
        InitializeComponent();
        _settings = settings;
        _mic = mic;

        PopulateColorPalette(OnColorPalette, isOnColor: true);
        PopulateColorPalette(OffColorPalette, isOnColor: false);

        SoundCheck.IsChecked = _settings.PlaySoundOnToggle;
        SoundCheck.Checked += (_, _) => _settings.PlaySoundOnToggle = true;
        SoundCheck.Unchecked += (_, _) => _settings.PlaySoundOnToggle = false;

        StartupCheck.IsChecked = StartupManager.IsEnabled;
        StartupCheck.Checked += (_, _) => StartupManager.IsEnabled = true;
        StartupCheck.Unchecked += (_, _) => StartupManager.IsEnabled = false;

        RefreshPresetList();
        UpdateMicStatus();

        if (_mic is not null)
            _mic.MuteChanged += _ => Dispatcher.Invoke(UpdateMicStatus);
    }

    private void RefreshPresetList()
    {
        _loading = true;
        var selectedId = (PresetList.SelectedItem as BarPreset)?.Id ?? _settings.SelectedPresetId;
        PresetList.ItemsSource = null;
        PresetList.ItemsSource = _settings.Presets;
        PresetList.SelectedItem = _settings.Presets.FirstOrDefault(p => p.Id == selectedId)
                                  ?? _settings.Presets.FirstOrDefault();
        _loading = false;
        LoadSelectedPreset();
    }

    private void OnPresetSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (PresetList.SelectedItem is BarPreset preset)
            _settings.SelectedPresetId = preset.Id;
        LoadSelectedPreset();
    }

    private BarPreset? SelectedPreset => PresetList.SelectedItem as BarPreset;

    private void LoadSelectedPreset()
    {
        var preset = SelectedPreset;
        bool hasPreset = preset is not null;
        EditorPanel.IsEnabled = hasPreset;
        if (!hasPreset) return;

        _loading = true;
        NameBox.Text = preset!.Name;
        OnTextBox.Text = preset.OnText;
        OffTextBox.Text = preset.OffText;
        OnColorBtn.Background = ToBrush(preset.OnColor);
        OffColorBtn.Background = ToBrush(preset.OffColor);
        _loading = false;

        UpdatePreview();
    }

    private void OnNameChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || SelectedPreset is null) return;
        SelectedPreset.Name = NameBox.Text;
        RefreshPresetList();
    }

    private void OnOnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || SelectedPreset is null) return;
        SelectedPreset.OnText = OnTextBox.Text;
        UpdatePreview();
    }

    private void OnOffTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || SelectedPreset is null) return;
        SelectedPreset.OffText = OffTextBox.Text;
        UpdatePreview();
    }

    private void OnOnColorClick(object sender, RoutedEventArgs e) => OnColorPopup.IsOpen = true;
    private void OnOffColorClick(object sender, RoutedEventArgs e) => OffColorPopup.IsOpen = true;

    private void PopulateColorPalette(WrapPanel panel, bool isOnColor)
    {
        foreach (var color in Palette)
        {
            var btn = new Button
            {
                Background = ToBrush(color),
                Style = (Style)FindResource("PopupColor"),
                Tag = color,
            };
            btn.Click += (_, _) =>
            {
                if (SelectedPreset is null) return;
                var c = (DrawingColor)btn.Tag;
                if (isOnColor)
                {
                    SelectedPreset.OnColorArgb = c.ToArgb();
                    OnColorBtn.Background = ToBrush(c);
                    OnColorPopup.IsOpen = false;
                }
                else
                {
                    SelectedPreset.OffColorArgb = c.ToArgb();
                    OffColorBtn.Background = ToBrush(c);
                    OffColorPopup.IsOpen = false;
                }
                UpdatePreview();
            };
            panel.Children.Add(btn);
        }
    }

    private void OnAddPreset(object sender, RoutedEventArgs e)
    {
        var preset = new BarPreset { Name = "New Preset" };
        _settings.Presets.Add(preset);
        _settings.SelectedPresetId = preset.Id;
        RefreshPresetList();
    }

    private void OnRemovePreset(object sender, RoutedEventArgs e)
    {
        if (SelectedPreset is null || _settings.Presets.Count <= 1) return;
        _settings.Presets.Remove(SelectedPreset);
        _settings.SelectedPresetId = _settings.Presets[0].Id;
        RefreshPresetList();
    }

    private void UpdatePreview()
    {
        var preset = SelectedPreset;
        if (preset is null) return;
        PreviewOnBorder.Background = ToBrush(preset.OnColor);
        PreviewOnText.Text = preset.OnText;
        PreviewOffBorder.Background = ToBrush(preset.OffColor);
        PreviewOffText.Text = preset.OffText;
    }

    private void UpdateMicStatus()
    {
        if (_mic is null)
        {
            MicDot.Fill = Brushes.Gray;
            MicStatusText.Text = "Mic service unavailable";
            return;
        }
        bool muted = _mic.IsMuted;
        MicDot.Fill = muted ? Brushes.Red : Brushes.LimeGreen;
        MicStatusText.Text = muted ? "Muted" : "Live";
    }

    private static SolidColorBrush ToBrush(DrawingColor c) =>
        new(Color.FromArgb(c.A, c.R, c.G, c.B));
}
