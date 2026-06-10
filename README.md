# HushBar — Windows
<img width="360" height="360" alt="image" src="https://github.com/user-attachments/assets/5178a61e-f54f-4aa4-84e5-9a815218fc85" />

**Mute your microphone from the system tray — globally, instantly, across every app.**

The Windows companion to [HushBar for macOS](https://github.com/ardacanbakis/hushBar).
A tray-only app that toggles the system microphone's hardware mute flag via the
Windows Core Audio (WASAPI) API — every app sees it at once. No capture stream is
ever opened, so nothing is ever recorded.

> **Looking for the macOS version?** [ardacanbakis/hushBar](https://github.com/ardacanbakis/hushBar)

---

## Features

- **One-click global mute** — left-click the tray icon to toggle your mic
- **Global hotkey** — default **Ctrl+Shift+M**, customizable, works from any app
- **5 tray icon styles** — Waveform, Capsule, Condenser, MicMute, RecordDot
- **8 color themes** — Classic, Traffic, Ocean, Neon, Amber, Royal, Mono, Mint — plus custom color picker
- **Stays in sync** — reflects external mute changes (Control Panel, other apps) and follows the default capture device when it changes
- **Launch at login** toggle
- **Sound feedback** — optional audio cue on toggle
- **Privacy-first** — toggles the endpoint mute flag only; no recording, no mic permission needed
- **Portable** — single self-contained `.exe`, no installer or runtime required

---

## Download

Grab the latest release from [this link](https://github.com/ardacanbakis/hushBar-windows/releases/download/v1.0/hushBar-v1.0-win-x64.zip)

1. Download `hushBar-v1.0-win-x64.zip`
2. Extract anywhere
3. Run `HushBar.exe`

No .NET runtime install needed — everything is self-contained.

---

## Build from source

Requires the **.NET 8 SDK** and Windows 10 or later.

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project src/HushBar
```

To create a release build:

```powershell
dotnet publish src/HushBar/HushBar.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:TrimMode=partial `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish
```

The tray icon appears in the bottom-right system tray (possibly under the `^` overflow arrow). Left-click toggles mute; right-click opens the menu.

---

## Tech stack

| Layer | Technology |
|-------|------------|
| Runtime | C# / .NET 8 (`net8.0-windows`) |
| Audio | [NAudio](https://github.com/naudio/NAudio) — WASAPI / MMDevice COM APIs |
| UI | WPF (Preferences window) + WinForms (`NotifyIcon` for system tray) |
| Tray icon | GDI+ dynamic 32x32 rendering with multiple styles and mute overlays |
| Global hotkey | Win32 `RegisterHotKey` via hidden message-only window |
| Settings | JSON in `%APPDATA%\HushBar\settings.json` |
| Startup | Per-user `HKCU\...\Run` registry key (no admin needed) |
| CI/CD | GitHub Actions — builds and publishes on version tags |

---

## How it works

- **Muting** — `MicMuteService` uses NAudio's `CoreAudioApi` to get the default capture endpoint and toggle `AudioEndpointVolume.Mute`. It reads the state back after writing to catch failures.
- **Staying in sync** — `OnVolumeNotification` and `IMMNotificationClient` listeners detect external mute changes and default-device switches, keeping the icon accurate.
- **Tray icon** — `TrayIconRenderer` draws 32x32 icons with GDI+ in five styles, with four mute overlay options (diagonal slash, X, red dot, crossed circle).
- **Global hotkey** — `HotKeyManager` registers a system-wide hotkey via Win32 `RegisterHotKey` through a hidden `HwndSource` window.
- **Launch at login** — `StartupManager` writes a per-user `Run` registry entry pointing to the current exe path.
- **Settings** — `AppSettings` serializes preferences (colors, hotkey, icon style) to JSON in `%APPDATA%\HushBar\`.

---

## Project structure

```
src/HushBar/
├── App.xaml.cs                  # Tray-only bootstrap, single-instance guard
├── Assets/                      # App icon (.ico, .png)
├── Services/
│   ├── MicMuteService.cs        # Core WASAPI mute + device listeners
│   ├── TrayIconRenderer.cs      # GDI+ icon drawing (5 styles + overlays)
│   ├── HotKeyManager.cs         # Win32 RegisterHotKey
│   ├── StartupManager.cs        # Registry Run key toggle
│   └── HushLog.cs               # Diagnostic logging
├── Models/
│   ├── AppSettings.cs           # JSON settings + presets
│   └── BarPreset.cs             # Preset definition (name, colors, labels)
└── Views/
    ├── PreferencesWindow.xaml    # Tabbed UI (General + About)
    └── PreferencesWindow.xaml.cs # Icon/color/hotkey customization
```

---

## Contributing

Pull requests and issues are welcome. If you have ideas for new icon styles, color themes, or distribution improvements, feel free to open an issue.

---

## Author

Made by [Arda Canbakis](https://ardacanbakis.com)

- [GitHub](https://github.com/ardacanbakis)
- [LinkedIn](https://linkedin.com/in/ardacanbakis)
- [Buy me a coffee](https://buymeacoffee.com/ardacanbakis)
