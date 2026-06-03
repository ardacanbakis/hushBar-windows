using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HushBar.Models;
using HushBar.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfImage = System.Windows.Controls.Image;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfCursors = System.Windows.Input.Cursors;
using WpfOrientation = System.Windows.Controls.Orientation;
using DrawingColor = System.Drawing.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace HushBar;

public partial class PreferencesWindow : Window
{
    private readonly AppSettings _settings;
    private readonly MicMuteService? _mic;
    private readonly HotKeyManager? _hotKey;
    private readonly Action? _onSettingsChanged;
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

    private static readonly (string Name, DrawingColor On, DrawingColor Off)[] ColorThemes =
    {
        ("Classic",  DrawingColor.FromArgb(255,  52, 199,  89), DrawingColor.FromArgb(255, 142, 142, 147)),
        ("Traffic",  DrawingColor.FromArgb(255,  52, 199,  89), DrawingColor.FromArgb(255, 255,  59,  48)),
        ("Ocean",    DrawingColor.FromArgb(255,   0, 122, 255), DrawingColor.FromArgb(255, 142, 142, 147)),
        ("Neon",     DrawingColor.FromArgb(255,  90, 200, 250), DrawingColor.FromArgb(255, 255,  45,  85)),
        ("Amber",    DrawingColor.FromArgb(255, 255, 149,   0), DrawingColor.FromArgb(255,  72,  72,  74)),
        ("Royal",    DrawingColor.FromArgb(255, 175,  82, 222), DrawingColor.FromArgb(255, 142, 142, 147)),
        ("Mono",     DrawingColor.White,                        DrawingColor.FromArgb(255, 142, 142, 147)),
        ("Mint",     DrawingColor.FromArgb(255,   0, 199, 190), DrawingColor.FromArgb(255,  72,  72,  74)),
    };

    public PreferencesWindow(AppSettings settings, MicMuteService? mic = null,
                             HotKeyManager? hotKey = null, Action? onSettingsChanged = null)
    {
        InitializeComponent();
        _settings = settings;
        _mic = mic;
        _hotKey = hotKey;
        _onSettingsChanged = onSettingsChanged;

        SoundCheck.IsChecked = _settings.PlaySoundOnToggle;
        SoundCheck.Checked   += (_, _) => _settings.PlaySoundOnToggle = true;
        SoundCheck.Unchecked += (_, _) => _settings.PlaySoundOnToggle = false;

        StartupCheck.IsChecked = StartupManager.IsEnabled;
        StartupCheck.Checked   += (_, _) => StartupManager.IsEnabled = true;
        StartupCheck.Unchecked += (_, _) => StartupManager.IsEnabled = false;

        HotKeyDisplay.Text = HotKeyManager.Describe(_settings.HotKeyModifiers, _settings.HotKeyVk);

        BuildIconGrid();
        BuildThemePanel();
        PopulateColorPalette(OnColorPalette, true);
        PopulateColorPalette(OffColorPalette, false);
        UpdateColorSwatches();

        MuteStyleBox.ItemsSource  = TrayIconRenderer.MuteStyles;
        MuteStyleBox.SelectedItem = _settings.MuteStyle;
        MuteStyleBox.SelectionChanged += OnMuteStyleChanged;

        if (!string.IsNullOrEmpty(_settings.CustomIconPath))
            CustomIconPathText.Text = Path.GetFileName(_settings.CustomIconPath);
        UpdateCustomIconPanelVisibility();

        UpdateGeneralPreview();
        UpdateMicStatus();

        if (_mic is not null)
            _mic.MuteChanged += _ => Dispatcher.Invoke(UpdateMicStatus);
    }

    // ── Tab switching ──────────────────────────────────────────────────────

    private void OnTabChanged(object sender, RoutedEventArgs e)
    {
        GeneralPanel.Visibility = TabGeneral.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        StylePanel.Visibility   = TabStyle.IsChecked   == true ? Visibility.Visible : Visibility.Collapsed;
        AboutPanel.Visibility   = TabAbout.IsChecked   == true ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── General tab ────────────────────────────────────────────────────────

    private void UpdateGeneralPreview()
    {
        GenPreviewStyle.Text = _settings.IconStyle;
        GenOnColorDot.Background  = ToBrush(_settings.OnColor);
        GenOffColorDot.Background = ToBrush(_settings.OffColor);

        using var bmp = TrayIconRenderer.RenderPreview(_settings.IconStyle, _settings.OnColor);
        GenPreviewIcon.Source = BitmapToSource(bmp);
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

    // ── Hotkey capture ─────────────────────────────────────────────────────

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

    // ── Style tab: icon grid ───────────────────────────────────────────────

    private readonly Dictionary<string, Border> _iconCards = new();

    private void BuildIconGrid()
    {
        foreach (var style in TrayIconRenderer.IconStyles)
        {
            var card = new Border
            {
                Width = 64, Height = 64,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(WpfColor.FromRgb(0x38, 0x38, 0x38)),
                BorderThickness = new Thickness(2),
                BorderBrush = style == _settings.IconStyle
                    ? (WpfBrush)FindResource("Accent")
                    : WpfBrushes.Transparent,
                Margin = new Thickness(3),
                Cursor = WpfCursors.Hand,
                ToolTip = style,
            };

            if (style == "Custom")
            {
                card.Child = new TextBlock
                {
                    Text = "+",
                    FontSize = 24,
                    FontWeight = FontWeights.Light,
                    Foreground = (WpfBrush)FindResource("FgSecondary"),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
            }
            else
            {
                using var bmp = TrayIconRenderer.RenderPreview(style, _settings.OnColor);
                var img = new WpfImage
                {
                    Source = BitmapToSource(bmp),
                    Width = 36, Height = 36,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                card.Child = img;
            }

            card.MouseLeftButtonDown += (_, _) =>
            {
                _settings.IconStyle = style;
                HighlightSelectedIcon();
                UpdateCustomIconPanelVisibility();
                UpdateGeneralPreview();
                _onSettingsChanged?.Invoke();
            };

            _iconCards[style] = card;
            IconGrid.Children.Add(card);
        }
    }

    private void HighlightSelectedIcon()
    {
        var accent = (WpfBrush)FindResource("Accent");
        foreach (var (style, card) in _iconCards)
            card.BorderBrush = style == _settings.IconStyle ? accent : WpfBrushes.Transparent;
    }

    private void RefreshIconGridColors()
    {
        foreach (var (style, card) in _iconCards)
        {
            if (style == "Custom") continue;
            using var bmp = TrayIconRenderer.RenderPreview(style, _settings.OnColor);
            if (card.Child is WpfImage img)
                img.Source = BitmapToSource(bmp);
        }
    }

    // ── Style tab: color themes ────────────────────────────────────────────

    private void BuildThemePanel()
    {
        foreach (var (name, on, off) in ColorThemes)
        {
            var stack = new StackPanel
            {
                Orientation = WpfOrientation.Vertical,
                Margin = new Thickness(3),
                Cursor = WpfCursors.Hand,
                ToolTip = name,
            };

            var container = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(WpfColor.FromRgb(0x38, 0x38, 0x38)),
                Padding = new Thickness(6, 5, 6, 5),
            };

            var dots = new StackPanel { Orientation = WpfOrientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            dots.Children.Add(new Border { Width = 14, Height = 14, CornerRadius = new CornerRadius(7), Background = ToBrush(on), Margin = new Thickness(0, 0, 3, 0) });
            dots.Children.Add(new Border { Width = 14, Height = 14, CornerRadius = new CornerRadius(7), Background = ToBrush(off) });
            container.Child = dots;

            stack.Children.Add(container);
            stack.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 9,
                Foreground = (WpfBrush)FindResource("FgSecondary"),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0),
            });

            stack.MouseLeftButtonDown += (_, _) =>
            {
                _settings.OnColorArgb  = on.ToArgb();
                _settings.OffColorArgb = off.ToArgb();
                UpdateColorSwatches();
                RefreshIconGridColors();
                UpdateGeneralPreview();
                _onSettingsChanged?.Invoke();
            };

            ThemePanel.Children.Add(stack);
        }
    }

    private void OnToggleCustomColors(object sender, RoutedEventArgs e)
    {
        CustomColorPanel.Visibility = CustomColorPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateColorSwatches()
    {
        OnColorBtn.Background  = ToBrush(_settings.OnColor);
        OffColorBtn.Background = ToBrush(_settings.OffColor);
    }

    // ── Color pickers ──────────────────────────────────────────────────────

    private void OnOnColorClick(object sender, RoutedEventArgs e)  => OnColorPopup.IsOpen  = true;
    private void OnOffColorClick(object sender, RoutedEventArgs e) => OffColorPopup.IsOpen = true;

    private void PopulateColorPalette(WrapPanel panel, bool isOn)
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
                var c = (DrawingColor)btn.Tag;
                if (isOn)
                {
                    _settings.OnColorArgb = c.ToArgb();
                    OnColorPopup.IsOpen   = false;
                }
                else
                {
                    _settings.OffColorArgb = c.ToArgb();
                    OffColorPopup.IsOpen   = false;
                }
                UpdateColorSwatches();
                RefreshIconGridColors();
                UpdateGeneralPreview();
                _onSettingsChanged?.Invoke();
            };
            panel.Children.Add(btn);
        }
    }

    // ── Mute style ─────────────────────────────────────────────────────────

    private void OnMuteStyleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MuteStyleBox.SelectedItem is not string style) return;
        _settings.MuteStyle = style;
        _onSettingsChanged?.Invoke();
    }

    // ── Custom icon ────────────────────────────────────────────────────────

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
            CustomIconPathText.Text  = Path.GetFileName(dlg.FileName);
            _onSettingsChanged?.Invoke();
        }
    }

    // ── About tab links ────────────────────────────────────────────────────

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private void OnLogoClick(object sender, RoutedEventArgs e) => OpenUrl("https://ardacanbakis.github.io/hushBar/");
    private void OnBuyMeACoffee(object sender, RoutedEventArgs e) => OpenUrl("https://buymeacoffee.com/ardacanbakis");
    private void OnOpenWebsite(object sender, RoutedEventArgs e)   => OpenUrl("https://ardacanbakis.com");
    private void OnOpenGitHub(object sender, RoutedEventArgs e)    => OpenUrl("https://github.com/ardacanbakis");
    private void OnOpenInstagram(object sender, RoutedEventArgs e) => OpenUrl("https://www.instagram.com/arda.canbakiss/");
    private void OnOpenYouTube(object sender, RoutedEventArgs e)   => OpenUrl("https://www.youtube.com/@arda.canbakis");
    private void OnOpenSpotify(object sender, RoutedEventArgs e)   => OpenUrl("https://open.spotify.com/user/11146430303?si=2bff0a4ba1484781");
    private void OnOpenLinkedIn(object sender, RoutedEventArgs e)  => OpenUrl("https://linkedin.com/in/ardacanbakis");

    // ── Helpers ────────────────────────────────────────────────────────────

    private static SolidColorBrush ToBrush(DrawingColor c) =>
        new(WpfColor.FromArgb(c.A, c.R, c.G, c.B));

    private static BitmapSource BitmapToSource(System.Drawing.Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = ms;
        img.EndInit();
        img.Freeze();
        return img;
    }
}
