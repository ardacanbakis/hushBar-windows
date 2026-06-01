using System.Windows;
using HushBar.Models;

namespace HushBar;

public partial class PreferencesWindow : Window
{
    private readonly AppSettings _settings;

    public PreferencesWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        SoundCheck.IsChecked = _settings.PlaySoundOnToggle;
        SoundCheck.Checked += (_, _) => _settings.PlaySoundOnToggle = true;
        SoundCheck.Unchecked += (_, _) => _settings.PlaySoundOnToggle = false;
    }
}
