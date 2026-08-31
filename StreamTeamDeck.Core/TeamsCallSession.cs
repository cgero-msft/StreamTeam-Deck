using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace StreamTeamDeck.Core;

/// <summary>
/// The call-control buttons of an active Teams meeting window. Elements are cached UIA
/// references; reads throw once the call ends or Teams rebuilds its UI, which callers
/// treat as "session is stale, rescan".
/// </summary>
public sealed class TeamsCallSession
{
    private readonly AutomationElement _window;
    private readonly Dictionary<TeamsButtonKind, AutomationElement> _buttons;

    private TeamsCallSession(AutomationElement window, Dictionary<TeamsButtonKind, AutomationElement> buttons)
    {
        _window = window;
        _buttons = buttons;
    }

    /// <summary>
    /// Classifies the window's buttons; returns null unless the window has at least a
    /// microphone and a hang-up control (i.e. it is actually a meeting window).
    /// </summary>
    public static TeamsCallSession? TryCreate(AutomationElement window)
    {
        AutomationElement[] buttons;
        try
        {
            buttons = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
        }
        catch
        {
            return null;
        }

        var found = new Dictionary<TeamsButtonKind, AutomationElement>();
        foreach (var button in buttons)
        {
            string automationId, name;
            try
            {
                automationId = button.Properties.AutomationId.ValueOrDefault ?? string.Empty;
                name = button.Name ?? string.Empty;
            }
            catch
            {
                continue;
            }

            if (Classify(automationId, name) is { } kind && !found.ContainsKey(kind))
            {
                found[kind] = button;
            }
        }

        if (!found.ContainsKey(TeamsButtonKind.Mute) || !found.ContainsKey(TeamsButtonKind.HangUp))
        {
            return null;
        }
        return new TeamsCallSession(window, found);
    }

    private static TeamsButtonKind? Classify(string automationId, string name)
    {
        static bool Has(string s, string sub) => s.Contains(sub, StringComparison.OrdinalIgnoreCase);

        // AutomationIds are stable across Teams localizations; names are the English fallback.
        // "mute" also matches "Unmute"; exclude roster-level buttons like "Mute all".
        if (Has(automationId, "microphone") || (Has(name, "mute") && !Has(name, "all"))) return TeamsButtonKind.Mute;
        if (Has(automationId, "video") || Has(name, "camera")) return TeamsButtonKind.Camera;
        if (Has(automationId, "hangup") || Has(name, "hang up") || Has(name, "leave")) return TeamsButtonKind.HangUp;
        if (Has(automationId, "raisehands") || Has(name, "raise hand") || Has(name, "lower hand") || Has(name, "raise your hand")) return TeamsButtonKind.Hand;
        return null;
    }

    /// <summary>Native handle of the meeting window, for focus-based hotkey fallback.</summary>
    public IntPtr WindowHandle
    {
        get
        {
            try { return _window.Properties.NativeWindowHandle.ValueOrDefault; }
            catch { return IntPtr.Zero; }
        }
    }

    /// <summary>
    /// Reads the live call state from the cached buttons. Throws if the elements have gone
    /// stale (call ended, window closed, UI rebuilt).
    /// </summary>
    public TeamsCallState ReadState()
    {
        // Teams buttons advertise the *action*, so the label is the inverse of the state:
        // "Unmute" means currently muted, "Turn camera on" means the camera is off.
        var muteName = ReadName(TeamsButtonKind.Mute);
        bool? muted = muteName == null ? null
            : muteName.Contains("unmute", StringComparison.OrdinalIgnoreCase) ? true
            : muteName.Contains("mute", StringComparison.OrdinalIgnoreCase) ? false
            : null;

        var cameraName = ReadName(TeamsButtonKind.Camera);
        bool? cameraOn = cameraName == null ? null
            : cameraName.Contains("camera on", StringComparison.OrdinalIgnoreCase) ? false
            : cameraName.Contains("camera off", StringComparison.OrdinalIgnoreCase) ? true
            : null;

        var handName = ReadName(TeamsButtonKind.Hand);
        bool? handRaised = handName == null ? null
            : handName.Contains("lower", StringComparison.OrdinalIgnoreCase) ? true
            : handName.Contains("raise", StringComparison.OrdinalIgnoreCase) ? false
            : null;

        return new TeamsCallState(true, muted, cameraOn, handRaised);
    }

    /// <summary>Returns null if the button was never found; throws if it has gone stale.</summary>
    private string? ReadName(TeamsButtonKind kind)
    {
        return _buttons.TryGetValue(kind, out var button) ? button.Name : null;
    }

    /// <summary>Presses a call control without changing window focus. False if unavailable.</summary>
    public bool TryInvoke(TeamsButtonKind kind)
    {
        if (!_buttons.TryGetValue(kind, out var button))
        {
            return false;
        }
        try
        {
            if (button.Patterns.Invoke.IsSupported)
            {
                button.Patterns.Invoke.Pattern.Invoke();
                return true;
            }
            if (button.Patterns.Toggle.IsSupported)
            {
                button.Patterns.Toggle.Pattern.Toggle();
                return true;
            }
        }
        catch
        {
            // Stale element or transient UIA failure; caller falls back.
        }
        return false;
    }
}
