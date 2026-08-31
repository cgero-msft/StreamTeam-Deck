using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace StreamTeamDeck.Core;

/// <summary>
/// Locates the Teams meeting window via UI Automation. Meetings can live in the main
/// window or a popped-out window, so every top-level Teams window is checked.
/// </summary>
public sealed class TeamsUiFinder : IDisposable
{
    private static readonly string[] TeamsProcessNames = ["ms-teams", "Teams"];

    private readonly UIA3Automation _automation = new();

    public TeamsCallSession? FindCallSession()
    {
        foreach (var window in GetTeamsWindows())
        {
            if (TeamsCallSession.TryCreate(window) is { } session)
            {
                return session;
            }
        }
        return null;
    }

    private IEnumerable<AutomationElement> GetTeamsWindows()
    {
        var desktop = _automation.GetDesktop();
        foreach (var processName in TeamsProcessNames)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                AutomationElement[] windows;
                try
                {
                    windows = desktop.FindAllChildren(cf => cf.ByProcessId(process.Id));
                }
                catch
                {
                    continue;
                }
                foreach (var window in windows)
                {
                    yield return window;
                }
            }
        }
    }

    public void Dispose() => _automation.Dispose();
}
