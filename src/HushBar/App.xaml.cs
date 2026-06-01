using System.Media;
using System.Windows;
using System.Windows.Forms;
using HushBar.Models;
using HushBar.Services;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace HushBar;

/// <summary>
/// Tray-only WPF agent. No main window opens at launch; everything is driven from
/// the NotifyIcon. Single-instance enforced via a named Mutex.
/// </summary>
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

        _hotKey = new HotKeyManager(() => _mic.Toggle());
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
        // Left-click toggles; right-click shows the menu (handled by ContextMenuStrip).
        _tray.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left) _mic?.Toggle();
        };
    }

    private void RefreshTray()
    {
        if (_tray is null || _mic is null) return;
        var preset = _settings.SelectedPreset;

        var oldIcon = _tray.Icon;
        _tray.Icon = TrayIconRenderer.Render(_mic.IsMuted, preset.OnColor, preset.OffColor);
        oldIcon?.Dispose();

        _tray.Text = preset.Tooltip(_mic.IsMuted);
        if (_muteItem is not null) _muteItem.Text = _mic.IsMuted ? "Unmute" : "Mute";

        if (_settings.PlaySoundOnToggle)
            SystemSounds.Asterisk.Play();
    }

    private PreferencesWindow? _prefs;
    private void ShowPreferences()
    {
        if (_prefs is null)
        {
            _prefs = new PreferencesWindow(_settings);
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
