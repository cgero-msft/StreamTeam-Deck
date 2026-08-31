namespace StreamTeamDeck.Core;

/// <summary>
/// Snapshot of the current Teams meeting state, as read from the meeting window's controls.
/// Nullable fields mean the corresponding control was found but its state could not be
/// determined (e.g. a non-English UI label).
/// </summary>
public sealed record TeamsCallState(bool InCall, bool? Muted, bool? CameraOn, bool? HandRaised)
{
    public static readonly TeamsCallState NoCall = new(false, null, null, null);
}
