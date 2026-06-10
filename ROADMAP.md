# HushBar for Windows - Roadmap & Decision Log

The Windows port of [HushBar](https://github.com/ardacanbakis/hushBar). Same idea
as the macOS app - a tray app that mutes/unmutes the **system microphone globally**
by flipping the audio endpoint's hardware mute flag - rebuilt on the Windows audio
stack. **No code is shared with the macOS version** (different language, audio API,
and UI toolkit); only the product concept, branding, and UX are shared.

---

## Why a separate repo

Swift + CoreAudio + AppKit and C# + WASAPI + WPF share zero source. The only
common assets are icons, brand, and docs. Keeping the toolchains isolated means
each repo’s tooling assumptions stay intact and CI/releases stay independent.
The two repos cross-link in their READMEs.

---

## Stack

| Concern | Choice | Notes |
|---|---|---|
| Language / runtime | **C# / .NET 8** (`net8.0-windows`) | LTS, great tooling |
| Audio mute | **NAudio** (`CoreAudioApi`) | wraps the WASAPI/MMDevice COM APIs |
| UI | **WPF** for the Preferences window | modern XAML UI |
| Tray icon | **WinForms `NotifyIcon`** (in-proc) | most battle-tested; dynamic icon via GDI+ |
| Global hotkey | Win32 `RegisterHotKey` via a message-only window | no extra deps |
| Launch at login | `HKCU\...\Run` registry key | simplest reliable approach |
| Settings store | JSON in `%APPDATA%\HushBar\settings.json` | mirrors the Mac UserDefaults model |
| Packaging | **MSIX** (Store-ready) or **WiX/MSI**; portable zip for quick share | |
| Distribution | GitHub Releases + **winget**; optionally Scoop / Chocolatey | winget ≈ Homebrew |

`UseWPF` and `UseWindowsForms` are both enabled in the csproj so the WPF prefs
window and the WinForms tray icon coexist.

---

## How muting works on Windows (the analog to the Mac’s CoreAudio knowledge)

```
MMDeviceEnumerator
  .GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications)
  .AudioEndpointVolume.Mute = true        // system-wide hardware endpoint mute
```

- This is the true endpoint mute - every app sees it, same as `kAudioDevicePropertyMute`
  on macOS. **No microphone permission is required** to set it (unlike macOS TCC),
  because we never open a capture stream.
- **Read back** `AudioEndpointVolume.Mute` after writing rather than trusting the call,
  mirroring the Mac `readMuted` discipline.
- **Listeners** keep the UI honest:
  - `AudioEndpointVolume.OnVolumeNotification` → external mute changes (Control Panel,
    another app) - analog to the Mac mute-property listener.
  - `IMMNotificationClient` registered on the enumerator → default-capture-device
    change - analog to the Mac default-device-change listener; re-apply intent to the
    new device.
- The Windows audio stack does **not** have the aggressive ~3.8 s re-sync daemon that
  caused the macOS oscillation saga, so the `muteIntent` re-assertion machinery is
  likely unnecessary here - but keep the read-back-after-write pattern and verify with
  fast-toggle testing before assuming so.

---

## The one big UX divergence: the tray icon is not a text pill

macOS lets the menu bar show an arbitrary-width text badge (“ON AIR”, “Live·Muted”).
The Windows tray is a **fixed 16/32 px icon** - you cannot render a wide text pill there.

Approach:
- Render a **32×32 icon dynamically** (GDI+ / `System.Drawing`) showing a mic glyph
  tinted with the preset’s on/off color, with a slash when muted. See
  `Services/TrayIconRenderer.cs`.
- Put the preset **label text in the tooltip** (`NotifyIcon.Text`).
- Optional later: a small **flyout window** anchored above the tray that shows the
  full styled pill (reuse the preset colors/labels), for parity with the Mac look.

So `BarPreset` keeps its colors/labels/state, but the *primary* surface is an icon
+ tooltip rather than a wide pill.

---

## Phased plan

### Phase 0 - Project setup  ✅ scaffolded here
- .NET 8 WPF + WinForms tray, single-instance, no main window on launch.
- NuGet: `NAudio`. (Tray uses built-in WinForms; no extra package needed.)

### Phase 1 - Core mute  ✅ scaffolded (`MicMuteService.cs`)
- Default capture endpoint → `AudioEndpointVolume.Mute` toggle with read-back.
- `OnVolumeNotification` + `IMMNotificationClient` listeners.
- Tray left-click toggles; right-click context menu (Mute/Unmute, Preferences,
  Launch at login, Quit).

### Phase 2 - Tray icon rendering  ✅ scaffolded (`TrayIconRenderer.cs`)
- Dynamic 32×32 mic glyph + color + mute slash; tooltip shows preset label.
- `DestroyIcon` P/Invoke to avoid GDI handle leaks when swapping icons.

### Phase 3 - Hotkey + startup  ✅ scaffolded
- `RegisterHotKey` (default **Ctrl+Shift+M**) via a message-only `HwndSource`.
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` toggle.

### Phase 4 - Preferences UI  ✅ built
- WPF window: preset list + editor (name, colors, on/off labels), sound toggle,
  launch-at-login, mic status. JSON-persisted settings.
- Color picker popup with 12-swatch palette (system colors matching macOS HIG).
- Live preview of on/off pill badges. Add/remove presets.
- Reuses the `BarPreset` / `AppSettings` models from `Models/`.

### Phase 5 - Distribution
- **Code signing:** Authenticode cert (DigiCert/Sectigo ~$100–400/yr) or
  **Azure Trusted Signing** / SignPath (cheaper/free for OSS). Different ecosystem
  from Apple notarization - no notarytool equivalent; SmartScreen reputation builds
  over time/with signing.
- **Package:** MSIX (`MakeAppx`/Visual Studio) for Store + modern installs, or
  **WiX v4** MSI, or a portable zip for quick sharing.
- **winget:** submit a manifest to `microsoft/winget-pkgs`; users then run
  `winget install ardacanbakis.HushBar`. (This is the Homebrew analog.)
- **Scoop/Chocolatey:** optional extra channels.
- GitHub Releases: attach `.msi` / `.zip`; tag mirrors the Mac release versioning.

---

## Repo layout

```
hushbar-windows/
  HushBar.sln
  README.md
  ROADMAP.md                         this file
  .gitignore
  src/HushBar/
    HushBar.csproj                   net8.0-windows, WPF + WinForms, NAudio
    App.xaml / App.xaml.cs           tray-only bootstrap, single instance
    Services/
      MicMuteService.cs              WASAPI endpoint mute + listeners + readback
      TrayIconRenderer.cs            GDI+ 32x32 dynamic icon
      HotKeyManager.cs               RegisterHotKey via message-only window
      StartupManager.cs              HKCU Run key launch-at-login
    Models/
      AppSettings.cs                 JSON settings in %APPDATA%
      BarPreset.cs                   name, colors, on/off labels, state
    Views/
      PreferencesWindow.xaml(.cs)    WPF prefs (TODO: flesh out editor)
```

---

## Bootstrapping the new repo (on your Windows machine)

```powershell
git clone https://github.com/ardacanbakis/hushBar-windows.git
cd hushBar-windows
```

Copy this scaffold in, then:

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project src/HushBar
```

> Requires the **.NET 8 SDK** and Windows 10/11. The tray icon appears in the
> system tray (bottom-right, possibly under the `^` overflow). Left-click to toggle.

---

## Cross-linking

- Add to the macOS repo README: “**Windows version:** ardacanbakis/hushBar-windows” ✅ done.
- Add to this README: “**macOS version:** ardacanbakis/hushBar” ✅ done.
- Consider a shared landing page on the existing GitHub Pages site listing both
  downloads (Mac DMG/Homebrew, Windows MSI/winget).
