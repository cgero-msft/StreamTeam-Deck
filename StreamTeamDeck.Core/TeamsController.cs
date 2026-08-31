using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StreamTeamDeck.Core;

/// <summary>
/// One-shot Teams actions for the CLI: UI Automation first, keyboard-shortcut fallback
/// (briefly focusing the meeting window, then restoring the previous foreground window).
/// </summary>
public sealed class TeamsController : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private const int MaxRetries = 3;
    private const int RetryDelayMs = 500;

    private static readonly Dictionary<TeamsButtonKind, string> Hotkeys = new()
    {
        [TeamsButtonKind.Mute] = "^+m",
        [TeamsButtonKind.Camera] = "^+o",
        [TeamsButtonKind.Hand] = "^+k",
        [TeamsButtonKind.HangUp] = "^+h",
    };

    private readonly TeamsUiFinder _finder = new();

    public async Task ExecuteAsync(TeamsButtonKind kind)
    {
        TeamsCallSession? session = null;
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try { session = _finder.FindCallSession(); }
            catch { session = null; }

            if (session != null && session.TryInvoke(kind))
            {
                return;
            }
            if (attempt < MaxRetries - 1)
            {
                await Task.Delay(RetryDelayMs);
            }
        }

        // Only fall back to keystrokes when a meeting window exists to receive them;
        // otherwise they would land in whatever app is focused.
        if (session == null)
        {
            throw new InvalidOperationException("No active Teams meeting found");
        }
        SendHotkey(kind, session);
    }

    public TeamsCallState ReadState()
    {
        try { return _finder.FindCallSession()?.ReadState() ?? TeamsCallState.NoCall; }
        catch { return TeamsCallState.NoCall; }
    }

    private static void SendHotkey(TeamsButtonKind kind, TeamsCallSession session)
    {
        // Teams shortcuts need the meeting window focused; raise it briefly and restore.
        var teamsWindow = session.WindowHandle;
        if (teamsWindow == IntPtr.Zero)
        {
            throw new InvalidOperationException("Teams meeting window is not available for the hotkey fallback");
        }
        var previous = GetForegroundWindow();
        SetForegroundWindow(teamsWindow);
        SendKeys.SendWait(Hotkeys[kind]);
        if (previous != IntPtr.Zero)
        {
            SetForegroundWindow(previous);
        }
    }

    public void Dispose() => _finder.Dispose();
}
