# HushBar — Windows

**Mute your microphone from the system tray — globally, instantly, across every app.**

The Windows port of [HushBar for macOS](https://github.com/ardacanbakis/hushBar).
A tray-only app that flips the system microphone's hardware mute flag via the
Windows Core Audio (WASAPI) API — every app sees it at once. No capture stream is
ever opened, so nothing is recorded.

> **macOS version:** [ardacanbakis/hushBar](https://github.com/ardacanbakis/hushBar)

---

## Features

- **One-click global mute** from the tray icon (left-click to toggle).
- **Global shortcut** — default **Ctrl+Shift+M**, works from any app.
- **Dynamic tray icon** — mic glyph tinted with your preset color, red slash when muted.
- **Stays in sync** — reflects external mute changes (Control Panel, another app) and
  follows the default capture device when it changes.
- **Launch at login** toggle.
- **Privacy-first** — toggles the endpoint mute flag; no recording, no mic permission needed.

---

## Build & run

Requires the **.NET 8 SDK** and Windows 10/11.

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project src/HushBar
```

The icon appears in the system tray (bottom-right, possibly under the `^` overflow
arrow). Left-click toggles mute; right-click opens the menu.

---

## How it works

- **Muting** (`Services/MicMuteService.cs`) uses NAudio's `CoreAudioApi` to get the
  default capture endpoint and set `AudioEndpointVolume.Mute`. It reads the state
  back after writing rather than trusting the call.
- **Staying in sync** — `OnVolumeNotification` (external mute changes) and
  `IMMNotificationClient` (default-device changes) listeners keep the icon honest.
- **Tray icon** (`Services/TrayIconRenderer.cs`) is drawn at 32×32 with GDI+. Unlike
  the macOS menu bar, the Windows tray can't show a wide text pill, so preset labels
  live in the tooltip.
- **Global shortcut** (`Services/HotKeyManager.cs`) — Win32 `RegisterHotKey` via a
  hidden message-only window.
- **Launch at login** (`Services/StartupManager.cs`) — per-user `Run` registry key.
- **Settings** (`Models/AppSettings.cs`) — JSON in `%APPDATA%\HushBar\`.

See [ROADMAP.md](ROADMAP.md) for the full plan, stack rationale, and distribution
(code signing, MSIX, winget).

---

## Status

Phases 0–3 scaffolded (core mute, tray, hotkey, startup). Phase 4 (full Preferences
preset editor) and Phase 5 (signing + packaging + winget) are TODO — see ROADMAP.md.
