using System.Diagnostics;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace StreamTeam_Deck;

class Program
{
    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("StreamTeam Deck - Teams Controller");
            Console.WriteLine("Usage:");
            Console.WriteLine("  StreamTeamDeck.exe mute      - Toggle mute/unmute");
            Console.WriteLine("  StreamTeamDeck.exe hangup    - Hang up the call");
            Console.WriteLine("  StreamTeamDeck.exe camera    - Toggle camera on/off");
            Console.WriteLine("  StreamTeamDeck.exe hand      - Raise/lower hand");
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var controller = new TeamsController();

        try
        {
            switch (command)
            {
                case "mute":
                    await controller.ToggleMute();
                    Console.WriteLine("Mute toggled");
                    break;
                case "hangup":
                    await controller.HangUp();
                    Console.WriteLine("Call ended");
                    break;
                case "camera":
                    await controller.ToggleCamera();
                    Console.WriteLine("Camera toggled");
                    break;
                case "hand":
                    await controller.ToggleHand();
                    Console.WriteLine("Hand toggled");
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    return 1;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

class TeamsController
{
    private const int MAX_RETRIES = 3;
    private const int RETRY_DELAY_MS = 500;

    public async Task ToggleMute()
    {
        await ExecuteTeamsAction("Mute", "mute", async () =>
        {
            // Try global hotkey first (Ctrl+Shift+M works globally in Teams)
            SendKeys.SendWait("^+m");
            await Task.Delay(100);
        });
    }

    public async Task HangUp()
    {
        await ExecuteTeamsAction("Hang up", "hang up", async () =>
        {
            // Try global hotkey first (Ctrl+Shift+H works globally in Teams)
            SendKeys.SendWait("^+h");
            await Task.Delay(100);
        });
    }

    public async Task ToggleCamera()
    {
        await ExecuteTeamsAction("Turn camera", "camera", async () =>
        {
            // Try global hotkey first (Ctrl+Shift+O works globally in Teams)
            SendKeys.SendWait("^+o");
            await Task.Delay(100);
        });
    }

    public async Task ToggleHand()
    {
        await ExecuteTeamsAction("Raise hand", "hand", async () =>
        {
            // Try global hotkey first (Ctrl+Shift+K works globally in Teams)
            SendKeys.SendWait("^+k");
            await Task.Delay(100);
        });
    }

    private async Task ExecuteTeamsAction(string buttonNamePattern, string actionName, Func<Task>? fallbackAction = null)
    {
        for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
        {
            try
            {
                var teamsProcess = Process.GetProcessesByName("ms-teams")
                    .FirstOrDefault() ?? Process.GetProcessesByName("Teams").FirstOrDefault();

                if (teamsProcess == null)
                {
                    throw new InvalidOperationException("Teams is not running");
                }

                // Get the main window
                var mainWindowHandle = teamsProcess.MainWindowHandle;
                if (mainWindowHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Teams window not found");
                }

                // Store the current foreground window
                var currentForeground = Program.GetForegroundWindow();

                // Try using UI Automation to find and click the button
                using var automation = new UIA3Automation();
                var mainWindow = automation.FromHandle(mainWindowHandle);

                // Search for the button in the control bar
                var button = FindButtonByName(mainWindow, buttonNamePattern);

                if (button != null && button.Patterns.Invoke.IsSupported)
                {
                    button.Patterns.Invoke.Pattern.Invoke();

                    // Restore the previous foreground window if it changed
                    if (currentForeground != IntPtr.Zero && Program.GetForegroundWindow() != currentForeground)
                    {
                        Program.SetForegroundWindow(currentForeground);
                    }

                    return;
                }

                // If UI Automation failed, try the fallback action (global hotkey)
                if (fallbackAction != null)
                {
                    await fallbackAction();
                    return;
                }

                if (attempt < MAX_RETRIES - 1)
                {
                    await Task.Delay(RETRY_DELAY_MS);
                }
            }
            catch (Exception ex) when (attempt < MAX_RETRIES - 1)
            {
                await Task.Delay(RETRY_DELAY_MS);
                continue;
            }
        }

        // If all retries failed, try the fallback one last time
        if (fallbackAction != null)
        {
            await fallbackAction();
        }
    }

    private AutomationElement? FindButtonByName(AutomationElement root, string namePattern)
    {
        try
        {
            var buttons = root.FindAllDescendants(x => x.ByControlType(ControlType.Button));

            foreach (var button in buttons)
            {
                try
                {
                    var name = button.Name;
                    if (!string.IsNullOrEmpty(name) && name.Contains(namePattern, StringComparison.OrdinalIgnoreCase))
                    {
                        return button;
                    }
                }
                catch
                {
                    continue;
                }
            }
        }
        catch
        {
            // Ignore automation exceptions
        }

        return null;
    }
}
