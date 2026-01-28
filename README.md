# StreamTeam Deck

A lightweight command-line utility that integrates Microsoft Teams meeting controls with Elgato Stream Deck. Control your Teams meetings with physical buttons - mute/unmute, toggle camera, raise hand, and hang up calls.

## Features

- **Toggle Mute/Unmute** - Quickly mute or unmute your microphone
- **Toggle Camera** - Turn your camera on or off
- **Raise/Lower Hand** - Raise or lower your hand in meetings
- **Hang Up** - End the current call
- **Silent Operation** - Runs in the background without showing a command prompt window
- **Dual Approach** - Uses UI Automation with keyboard shortcut fallback for reliability

## Prerequisites

- Windows 10/11
- .NET 10 Runtime
- Microsoft Teams (desktop app)
- Elgato Stream Deck

## Building the Project

1. Open the solution in Visual Studio 2022 or later
2. Build the solution (`Ctrl+Shift+B`)
3. The executable will be in `StreamTeam Deck\bin\Debug\net10.0-windows\StreamTeamDeck.exe`

For production use, build in Release mode:
- `StreamTeam Deck\bin\Release\net10.0-windows\StreamTeamDeck.exe`

## Available Commands

Run the executable with one of these commands:

```cmd
StreamTeamDeck.exe mute      # Toggle mute/unmute
StreamTeamDeck.exe camera    # Toggle camera on/off
StreamTeamDeck.exe hand      # Raise/lower hand
StreamTeamDeck.exe hangup    # Hang up the call
```

## Stream Deck Configuration

### Method 1: Using "System → Open" Action

1. Open Stream Deck software
2. Drag **"System → Open"** action to a button
3. In the **App/File** field, enter the full command:
   ```
   "C:\path\to\StreamTeamDeck.exe" mute
   ```
   (Replace `mute` with `camera`, `hand`, or `hangup` for other buttons)


## How It Works

The application uses a two-tier approach:

1. **UI Automation (Primary)**: Attempts to find and click the Teams meeting control buttons using FlaUI
2. **Keyboard Shortcuts (Fallback)**: If UI Automation fails, uses Teams global hotkeys:
   - Mute: `Ctrl+Shift+M`
   - Camera: `Ctrl+Shift+O`
   - Hand: `Ctrl+Shift+K`
   - Hang Up: `Ctrl+Shift+H`

The app includes retry logic and restores focus to the previously active window after executing actions.

## Testing

1. Join or start a Teams meeting
2. Run commands from PowerShell to test:
   ```powershell
   cd "StreamTeam Deck\bin\Debug\net10.0-windows"
   .\StreamTeamDeck.exe mute
   ```
3. Verify the action occurs in Teams


## Technical Details

- **Framework**: .NET 10
- **UI Automation**: FlaUI.UIA3
- **P/Invoke**: user32.dll for window management
- **Retry Logic**: 3 attempts with 500ms delay

## License

Copyright © 2026

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

