using System.Media;
using System.Windows;
using System.Windows.Forms;
using HushBar.Models;
using HushBar.Services;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace HushBar;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private NotifyIcon? _tray;
    private MicMuteService? _mic;
    private HotKeyManager? _hotKey;
    private AppSettings _settings = new();
    private ToolStripMenuItem? _muteItem;
    private ToolStripMenuItem? _startupItem;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        _singleInstance = new Mutex(initiallyOwned: true, "HushBar_SingleInstance", out bool created);
        if (!created)
        {
            MessageBox.Show("HushBar is already running (check the system tray).",
                "HushBar", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _settings = AppSettings.Load();
        _mic = new MicMuteService();
        _mic.MuteChanged += _ => RefreshTray();

        BuildTray();
        RefreshTray();

        _hotKey = new HotKeyManager(() => _mic.Toggle(), _settings.HotKeyModifiers, _settings.HotKeyVk);

        if (!StartupManager.IsEnabled)
        {
            var result = MessageBox.Show(
                "Would you like hushBar to launch automatically at login?\n\n" +
                "You can change this later in Preferences.",
                "hushBar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                StartupManager.IsEnabled = true;
        }
    }

    private void BuildTray()
    {
        var menu = new ContextMenuStrip();

        _muteItem = new ToolStripMenuItem("Mute", null, (_, _) => _mic?.Toggle());
        menu.Items.Add(_muteItem);
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add(new ToolStripMenuItem("Preferences…", null, (_, _) => ShowPreferences()));

        _startupItem = new ToolStripMenuItem("Launch at login", null, (_, _) =>
        {
            StartupManager.IsEnabled = !StartupManager.IsEnabled;
            _startupItem!.Checked = StartupManager.IsEnabled;
        }) { Checked = StartupManager.IsEnabled };
        menu.Items.Add(_startupItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Quit HushBar", null, (_, _) => Shutdown()));

        _tray = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = menu,
            Text = "HushBar",
        };
        _tray.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left) _mic?.Toggle();
        };
    }

    private void RefreshTray()
    {
        if (_tray is null || _mic is null) return;

        var oldIcon = _tray.Icon;
        _tray.Icon = TrayIconRenderer.Render(
            _mic.IsMuted, _settings.OnColor, _settings.OffColor,
            _settings.IconStyle, _settings.MuteStyle, _settings.CustomIconPath);
        oldIcon?.Dispose();

        _tray.Text = _mic.IsMuted ? "HushBar — Muted" : "HushBar — Live";
        if (_muteItem is not null) _muteItem.Text = _mic.IsMuted ? "Unmute" : "Mute";

        if (_settings.PlaySoundOnToggle)
            SystemSounds.Asterisk.Play();
    }

    private PreferencesWindow? _prefs;
    private void ShowPreferences()
    {
        if (_prefs is null)
        {
            _prefs = new PreferencesWindow(_settings, _mic, _hotKey, () => RefreshTray());
            _prefs.Closed += (_, _) => { _settings.Save(); RefreshTray(); _prefs = null; };
        }
        _prefs.Show();
        _prefs.Activate();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _settings.Save();
        _hotKey?.Dispose();
        _mic?.Dispose();
        if (_tray is not null) { _tray.Visible = false; _tray.Dispose(); }
        _singleInstance?.Dispose();
    }
}
