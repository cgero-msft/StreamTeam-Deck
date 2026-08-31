namespace StreamTeamDeck.Core;

/// <summary>
/// Polls the Teams UI for live call state. While a call is active it re-reads the cached
/// button elements (cheap); while idle it does a slower full rescan for a meeting window.
/// </summary>
public sealed class TeamsStateWatcher : IDisposable
{
    private const int InCallPollMs = 750;
    private const int IdlePollMs = 3000;

    private readonly TeamsUiFinder _finder = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _pollNow = new(0, 1);
    private readonly object _sessionLock = new();
    private TeamsCallSession? _session;
    private Task? _loop;

    /// <summary>Raised on the polling thread whenever the state differs from the last poll.</summary>
    public event Action<TeamsCallState>? StateChanged;

    public TeamsCallState Current { get; private set; } = TeamsCallState.NoCall;

    public void Start() => _loop ??= Task.Run(() => RunAsync(_cts.Token));

    /// <summary>Wakes the polling loop early (e.g. right after invoking a control).</summary>
    public void RequestImmediatePoll()
    {
        try { _pollNow.Release(); }
        catch (SemaphoreFullException) { }
    }

    /// <summary>Presses a meeting control, rescanning once if the cached session is stale.</summary>
    public bool Invoke(TeamsButtonKind kind)
    {
        bool invoked;
        lock (_sessionLock)
        {
            invoked = _session?.TryInvoke(kind) ?? false;
            if (!invoked)
            {
                try { _session = _finder.FindCallSession(); }
                catch { _session = null; }
                invoked = _session?.TryInvoke(kind) ?? false;
            }
        }
        if (invoked)
        {
            RequestImmediatePoll();
        }
        return invoked;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var state = PollOnce();
            if (state != Current)
            {
                Current = state;
                try { StateChanged?.Invoke(state); }
                catch { }
            }

            try { await _pollNow.WaitAsync(state.InCall ? InCallPollMs : IdlePollMs, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private TeamsCallState PollOnce()
    {
        lock (_sessionLock)
        {
            if (_session != null)
            {
                try { return _session.ReadState(); }
                catch { _session = null; } // stale: call ended or Teams rebuilt its UI
            }

            try
            {
                _session = _finder.FindCallSession();
                return _session?.ReadState() ?? TeamsCallState.NoCall;
            }
            catch
            {
                _session = null;
                return TeamsCallState.NoCall;
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        RequestImmediatePoll();
        try { _loop?.Wait(2000); }
        catch { }
        _finder.Dispose();
        _cts.Dispose();
        _pollNow.Dispose();
    }
}
