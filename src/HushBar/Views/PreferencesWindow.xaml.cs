using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HushBar.Models;
using HushBar.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using DrawingColor = System.Drawing.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace HushBar;

public partial class PreferencesWindow : Window
{
    private readonly AppSettings _settings;
    private readonly MicMuteService? _mic;
    private readonly HotKeyManager? _hotKey;
    private readonly Action? _onSettingsChanged;
    private bool _loading;
    private bool _recording;

    private static readonly DrawingColor[] Palette =
    {
        DrawingColor.FromArgb(255, 255,  59,  48),
        DrawingColor.FromArgb(255,  52, 199,  89),
        DrawingColor.FromArgb(255,   0, 122, 255),
        DrawingColor.FromArgb(255, 255, 149,   0),
        DrawingColor.FromArgb(255, 175,  82, 222),
        DrawingColor.FromArgb(255, 255, 204,   0),
        DrawingColor.FromArgb(255,  90, 200, 250),
        DrawingColor.FromArgb(255, 255,  45,  85),
        DrawingColor.FromArgb(255,  88,  86, 214),
        DrawingColor.FromArgb(255,   0, 199, 190),
        DrawingColor.FromArgb(255, 142, 142, 147),
        DrawingColor.FromArgb(255,  72,  72,  74),
        DrawingColor.White,
        DrawingColor.Black,
    };

    public PreferencesWindow(AppSettings settings, MicMuteService? mic = null,
                             HotKeyManager? hotKey = null, Action? onSettingsChanged = null)
    {
        InitializeComponent();
        _settings = settings;
        _mic = mic;
        _hotKey = hotKey;
        _onSettingsChanged = onSettingsChanged;

        PopulateColorPalette(OnColorPalette,      ColorTarget.BadgeOn);
        PopulateColorPalette(OnTextColorPalette,  ColorTarget.TextOn);
        PopulateColorPalette(OffColorPalette,     ColorTarget.BadgeOff);
        PopulateColorPalette(OffTextColorPalette, ColorTarget.TextOff);

        SoundCheck.IsChecked = _settings.PlaySoundOnToggle;
        SoundCheck.Checked   += (_, _) => _settings.PlaySoundOnToggle = true;
        SoundCheck.Unchecked += (_, _) => _settings.PlaySoundOnToggle = false;

        StartupCheck.IsChecked = StartupManager.IsEnabled;
        StartupCheck.Checked   += (_, _) => StartupManager.IsEnabled = true;
        StartupCheck.Unchecked += (_, _) => StartupManager.IsEnabled = false;

        HotKeyDisplay.Text = HotKeyManager.Describe(_settings.HotKeyModifiers, _settings.HotKeyVk);

        // Icon / mute style pickers - populate then wire events so initialisation doesn't fire handlers
        IconStyleBox.ItemsSource  = TrayIconRenderer.IconStyles;
        MuteStyleBox.ItemsSource  = TrayIconRenderer.MuteStyles;
        IconStyleBox.SelectedItem = _settings.IconStyle;
        MuteStyleBox.SelectedItem = _settings.MuteStyle;

        if (!string.IsNullOrEmpty(_settings.CustomIconPath))
            CustomIconPathText.Text = System.IO.Path.GetFileName(_settings.CustomIconPath);
        UpdateCustomIconPanelVisibility();

        IconStyleBox.SelectionChanged += OnIconStyleChanged;
        MuteStyleBox.SelectionChanged += OnMuteStyleChanged;

        RefreshPresetList();
        UpdateGeneralPreview();
        UpdateMicStatus();

        if (_mic is not null)
            _mic.MuteChanged += _ => Dispatcher.Invoke(UpdateMicStatus);
    }

    // -- Tab switching

    private void OnTabChanged(object sender, RoutedEventArgs e)
    {
        GeneralPanel.Visibility = TabGeneral.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        StylePanel.Visibility   = TabStyle.IsChecked   == true ? Visibility.Visible : Visibility.Collapsed;
        AboutPanel.Visibility   = TabAbout.IsChecked   == true ? Visibility.Visible : Visibility.Collapsed;
    }

    // -- General tab

    private void UpdateGeneralPreview()
    {
        var preset = _settings.SelectedPreset;
        GenPreviewOnBorder.Background  = ToBrush(preset.OnColor);
        GenPreviewOnText.Text          = preset.OnText;
        GenPreviewOnText.Foreground    = ToBrush(preset.OnTextColor);
        GenPreviewOffBorder.Background = ToBrush(preset.OffColor);
        GenPreviewOffText.Text         = preset.OffText;
        GenPreviewOffText.Foreground   = ToBrush(preset.OffTextColor);
        GenPreviewName.Text            = preset.Name;
    }

    private void UpdateMicStatus()
    {
        if (_mic is null)
        {
            MicStatusText.Foreground = WpfBrushes.Gray;
            MicStatusText.Text = "Unavailable";
            return;
        }
        bool muted = _mic.IsMuted;
        MicStatusText.Foreground = muted ? WpfBrushes.Red : WpfBrushes.LimeGreen;
        MicStatusText.Text = muted ? "Muted" : "Live";
    }

    // -- Hotkey capture

    private void OnHotKeyRecord(object sender, RoutedEventArgs e)
    {
        if (!_recording)
        {
            _recording = true;
            HotKeyRecordBtn.Content = "Stop";
            HotKeyHint.Visibility = Visibility.Visible;
            HotKeyDisplay.Text = "Press keys...";
            PreviewKeyDown += OnHotKeyCapture;
            Focus();
        }
        else StopRecording();
    }

    private void OnHotKeyCapture(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.LeftCtrl  || key == Key.RightCtrl  ||
            key == Key.LeftAlt   || key == Key.RightAlt   ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin      || key == Key.RWin) return;

        uint mods = 0;
        if (Keyboard.IsKeyDown(Key.LeftCtrl)  || Keyboard.IsKeyDown(Key.RightCtrl))  mods |= HotKeyManager.MOD_CONTROL;
        if (Keyboard.IsKeyDown(Key.LeftAlt)   || Keyboard.IsKeyDown(Key.RightAlt))   mods |= HotKeyManager.MOD_ALT;
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) mods |= HotKeyManager.MOD_SHIFT;
        if (mods == 0) return;

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (vk == 0) return;

        _settings.HotKeyModifiers = mods;
        _settings.HotKeyVk = vk;
        _hotKey?.Rebind(mods, vk);
        HotKeyDisplay.Text = HotKeyManager.Describe(mods, vk);
        StopRecording();
    }

    private void StopRecording()
    {
        _recording = false;
        HotKeyRecordBtn.Content = "Record";
        HotKeyHint.Visibility = Visibility.Collapsed;
        PreviewKeyDown -= OnHotKeyCapture;
        if (HotKeyDisplay.Text == "Press keys...")
            HotKeyDisplay.Text = HotKeyManager.Describe(_settings.HotKeyModifiers, _settings.HotKeyVk);
    }

    // -- Style tab: preset list

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
        NameBox.Text           = preset!.Name;
        OnTextBox.Text         = preset.OnText;
        OffTextBox.Text        = preset.OffText;
        OnColorBtn.Background      = ToBrush(preset.OnColor);
        OnTextColorBtn.Background  = ToBrush(preset.OnTextColor);
        OffColorBtn.Background     = ToBrush(preset.OffColor);
        OffTextColorBtn.Background = ToBrush(preset.OffTextColor);
        _loading = false;

        UpdateStylePreview();
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
        UpdateStylePreview();
    }

    private void OnOffTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || SelectedPreset is null) return;
        SelectedPreset.OffText = OffTextBox.Text;
        UpdateStylePreview();
    }

    private void UpdateStylePreview()
    {
        var preset = SelectedPreset;
        if (preset is null) return;
        PreviewOnBorder.Background  = ToBrush(preset.OnColor);
        PreviewOnText.Text          = preset.OnText;
        PreviewOnText.Foreground    = ToBrush(preset.OnTextColor);
        PreviewOffBorder.Background = ToBrush(preset.OffColor);
        PreviewOffText.Text         = preset.OffText;
        PreviewOffText.Foreground   = ToBrush(preset.OffTextColor);
    }

    // -- Color pickers

    private enum ColorTarget { BadgeOn, TextOn, BadgeOff, TextOff }

    private void OnOnColorClick(object sender, RoutedEventArgs e)      => OnColorPopup.IsOpen      = true;
    private void OnOnTextColorClick(object sender, RoutedEventArgs e)  => OnTextColorPopup.IsOpen  = true;
    private void OnOffColorClick(object sender, RoutedEventArgs e)     => OffColorPopup.IsOpen     = true;
    private void OnOffTextColorClick(object sender, RoutedEventArgs e) => OffTextColorPopup.IsOpen = true;

    private void PopulateColorPalette(WrapPanel panel, ColorTarget target)
    {
        foreach (var color in Palette)
        {
            var btn = new WpfButton
            {
                Background = ToBrush(color),
                Style      = (Style)FindResource("PopupColor"),
                Tag        = color,
            };
            btn.Click += (_, _) =>
            {
                if (SelectedPreset is null) return;
                var c = (DrawingColor)btn.Tag;
                switch (target)
                {
                    case ColorTarget.BadgeOn:
                        SelectedPreset.OnColorArgb    = c.ToArgb();
                        OnColorBtn.Background         = ToBrush(c);
                        OnColorPopup.IsOpen           = false;
                        break;
                    case ColorTarget.TextOn:
                        SelectedPreset.OnTextColorArgb = c.ToArgb();
                        OnTextColorBtn.Background      = ToBrush(c);
                        OnTextColorPopup.IsOpen        = false;
                        break;
                    case ColorTarget.BadgeOff:
                        SelectedPreset.OffColorArgb   = c.ToArgb();
                        OffColorBtn.Background        = ToBrush(c);
                        OffColorPopup.IsOpen          = false;
                        break;
                    case ColorTarget.TextOff:
                        SelectedPreset.OffTextColorArgb = c.ToArgb();
                        OffTextColorBtn.Background      = ToBrush(c);
                        OffTextColorPopup.IsOpen        = false;
                        break;
                }
                UpdateStylePreview();
            };
            panel.Children.Add(btn);
        }
    }

    // -- Preset actions

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
        UpdateGeneralPreview();
    }

    private void OnUsePreset(object sender, RoutedEventArgs e)
    {
        if (SelectedPreset is null) return;
        _settings.SelectedPresetId = SelectedPreset.Id;
        UpdateGeneralPreview();
        _onSettingsChanged?.Invoke();
    }

    private void OnDuplicatePreset(object sender, RoutedEventArgs e)
    {
        if (SelectedPreset is null) return;
        var clone = SelectedPreset.Clone();
        _settings.Presets.Add(clone);
        _settings.SelectedPresetId = clone.Id;
        RefreshPresetList();
    }

    private void OnResetPresets(object sender, RoutedEventArgs e)
    {
        _settings.Presets = BarPreset.Defaults();
        _settings.SelectedPresetId = _settings.Presets[0].Id;
        RefreshPresetList();
        UpdateGeneralPreview();
        _onSettingsChanged?.Invoke();
    }

    // -- Icon / Mute style

    private void OnIconStyleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IconStyleBox.SelectedItem is not string style) return;
        _settings.IconStyle = style;
        UpdateCustomIconPanelVisibility();
        _onSettingsChanged?.Invoke();
    }

    private void OnMuteStyleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MuteStyleBox.SelectedItem is not string style) return;
        _settings.MuteStyle = style;
        _onSettingsChanged?.Invoke();
    }

    private void UpdateCustomIconPanelVisibility()
    {
        CustomIconPanel.Visibility =
            _settings.IconStyle == "Custom" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnBrowseCustomIcon(object sender, RoutedEventArgs e)
    {
        var dlg = new WpfOpenFileDialog
        {
            Title  = "Select custom icon",
            Filter = "Image files (*.ico;*.png;*.bmp)|*.ico;*.png;*.bmp|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog(this) == true)
        {
            _settings.CustomIconPath = dlg.FileName;
            CustomIconPathText.Text  = System.IO.Path.GetFileName(dlg.FileName);
            _onSettingsChanged?.Invoke();
        }
    }

    // -- About tab links

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private void OnBuyMeACoffee(object sender, RoutedEventArgs e) => OpenUrl("https://buymeacoffee.com/ardacanbakis");
    private void OnOpenWebsite(object sender, RoutedEventArgs e)   => OpenUrl("https://ardacanbakis.com");
    private void OnOpenGitHub(object sender, RoutedEventArgs e)    => OpenUrl("https://github.com/ardacanbakis");
    private void OnOpenInstagram(object sender, RoutedEventArgs e) => OpenUrl("https://www.instagram.com/arda.canbakiss/");
    private void OnOpenYouTube(object sender, RoutedEventArgs e)   => OpenUrl("https://www.youtube.com/@arda.canbakis");
    private void OnOpenSpotify(object sender, RoutedEventArgs e)   => OpenUrl("https://open.spotify.com/user/11146430303?si=2bff0a4ba1484781");
    private void OnOpenLinkedIn(object sender, RoutedEventArgs e)  => OpenUrl("https://linkedin.com/in/ardacanbakis");

    // -- Helpers

    private static SolidColorBrush ToBrush(DrawingColor c) =>
        new(WpfColor.FromArgb(c.A, c.R, c.G, c.B));
}
