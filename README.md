# StreamTeam Deck

Control Microsoft Teams meetings from an Elgato Stream Deck — mute/unmute, toggle camera, raise hand, and hang up — with **live status on the keys**, no Teams API required. Works even when Teams is not the focused window.

Two ways to use it:

1. **Stream Deck plugin (recommended)** — real plugin actions whose keys show your current state: a red slashed mic when muted, a red slashed camera when video is off, an amber hand when raised. Keys grey out when you're not in a call.
2. **Command-line utility** — a lightweight exe you can wire to Stream Deck "System → Open" actions (or anything else). Fire-and-forget, no status feedback.

## How It Works

Both use Windows **UI Automation** (via FlaUI) — the same accessibility layer screen readers use:

- **Actions**: finds the meeting window's control buttons (matching stable AutomationIds first, English labels as fallback) and invokes them directly — no window focus change, no keystrokes. If the button can't be found, falls back to Teams keyboard shortcuts, briefly focusing the meeting window and restoring your previous window.
- **Status**: Teams buttons advertise their *action* ("Unmute", "Turn camera on"), which is the inverse of the current state. The plugin polls the cached button elements (~750 ms while in a call, 3 s scan while idle) and pushes state changes to the keys — including changes you make inside Teams itself.
- Meetings in popped-out windows are found too; every top-level Teams window is checked.

> **Note**: State detection currently relies on Teams' English UI labels (with AutomationIds as the primary, language-independent match). If your Teams UI is in another language and AutomationIds don't match, buttons still work but state may show as unknown.

## Prerequisites

- Windows 10/11
- .NET 10 Runtime (SDK to build)
- Microsoft Teams (new desktop app)
- Elgato Stream Deck with Stream Deck software 6.4+

## Stream Deck Plugin

### Build & install

```powershell
.\build-plugin.ps1 -Install
```

This publishes the plugin, assembles `dist\com.cgero.streamteamdeck.sdPlugin`, copies it to `%APPDATA%\Elgato\StreamDeck\Plugins`, and restarts the Stream Deck app. You'll find a **StreamTeam Deck** category with four actions: Toggle Mute, Toggle Camera, Raise Hand, Hang Up — just drag them onto keys.

### Key behavior

| Key | Not in call | In call |
|---|---|---|
| Toggle Mute | greyed out | white mic = live, red slashed mic = muted |
| Toggle Camera | greyed out | white camera = on, red slashed camera = off |
| Raise Hand | greyed out | white hand = lowered, amber hand = raised |
| Hang Up | greyed out | red hang-up |

Pressing a key while not in a call shows the Stream Deck alert triangle. Troubleshooting: the plugin logs to `logs\plugin.log` inside its installed folder.

## Command-Line Utility

Build the solution (Visual Studio 2022+ or `dotnet build`), then:

```cmd
StreamTeamDeck.exe mute      # Toggle mute/unmute
StreamTeamDeck.exe camera    # Toggle camera on/off
StreamTeamDeck.exe hand      # Raise/lower hand
StreamTeamDeck.exe hangup    # Hang up the call
StreamTeamDeck.exe status    # Print call state as JSON, e.g. {"inCall":true,"muted":false,...}
StreamTeamDeck.exe watch     # Stream call-state changes as JSON lines (Ctrl+C to stop)
```

For Stream Deck without the plugin, drag a **System → Open** action to a key with:

```
"C:\path\to\StreamTeamDeck.exe" mute
```

(The exe is a windowed app, so `status`/`watch` output only appears when piped, e.g. `.\StreamTeamDeck.exe status | more`.)

## Project Layout

- `StreamTeamDeck.Core` — shared library: window/button discovery, state reading, state watcher, action invocation
- `StreamTeam Deck` — the CLI (`StreamTeamDeck.exe`)
- `StreamTeamDeck.Plugin` — the Stream Deck plugin host (WebSocket client for Elgato's plugin protocol) plus the `.sdPlugin` manifest and icons
- `build-plugin.ps1` — builds/installs the plugin

## Technical Details

- **Framework**: .NET 10
- **UI Automation**: FlaUI.UIA3
- **Keyboard fallback**: Teams shortcuts (`Ctrl+Shift+M` / `O` / `K` / `H`) with focus save/restore via user32.dll
- **Stream Deck**: Elgato SDK v2 WebSocket protocol, implemented directly (no plugin framework dependency)

## License

Copyright © 2026

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
