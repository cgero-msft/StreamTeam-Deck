using System.Text.Json;
using StreamTeamDeck.Core;

namespace StreamTeamDeck.Plugin;

/// <summary>
/// Routes Stream Deck events to Teams actions and pushes live call state back to the keys:
/// mute/camera/hand keys flip between their manifest state images, and all keys grey out
/// while no call is active.
/// </summary>
internal sealed class PluginHost : IDisposable
{
    private const string ActionPrefix = "com.cgero.streamteamdeck.";

    private readonly PluginArgs _args;
    private readonly StreamDeckConnection _connection = new();
    private readonly TeamsStateWatcher _watcher = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly object _contextLock = new();
    private readonly Dictionary<string, string> _actionByContext = new();
    private readonly SemaphoreSlim _pushLock = new(1, 1);

    public PluginHost(PluginArgs args) => _args = args;

    public async Task RunAsync()
    {
        var ct = _cts.Token;
        await _connection.ConnectAsync(_args.Port, _args.PluginUuid, _args.RegisterEvent, ct);
        Log.Write("Connected to Stream Deck");

        _watcher.StateChanged += state => { _ = PushStateToAllAsync(); };
        _watcher.Start();

        while (!ct.IsCancellationRequested)
        {
            JsonDocument? doc;
            try
            {
                doc = await _connection.ReceiveAsync(ct);
            }
            catch (Exception ex)
            {
                Log.Write($"Receive failed: {ex.Message}");
                break;
            }
            if (doc == null)
            {
                Log.Write("Stream Deck closed the connection");
                break;
            }
            using (doc)
            {
                try
                {
                    HandleMessage(doc.RootElement);
                }
                catch (Exception ex)
                {
                    Log.Write($"Handler error: {ex}");
                }
            }
        }
    }

    private void HandleMessage(JsonElement message)
    {
        if (!message.TryGetProperty("event", out var eventProperty))
        {
            return;
        }
        var eventName = eventProperty.GetString();
        var action = message.TryGetProperty("action", out var a) ? a.GetString() : null;
        var context = message.TryGetProperty("context", out var c) ? c.GetString() : null;
        if (action == null || context == null)
        {
            return;
        }

        switch (eventName)
        {
            case "willAppear":
                lock (_contextLock)
                {
                    _actionByContext[context] = action;
                }
                _ = PushStateAsync(action, context, _watcher.Current);
                break;

            case "willDisappear":
                lock (_contextLock)
                {
                    _actionByContext.Remove(context);
                }
                break;

            case "keyDown":
                HandleKeyDown(action, context);
                break;
        }
    }

    private void HandleKeyDown(string action, string context)
    {
        TeamsButtonKind? kind = action switch
        {
            ActionPrefix + "mute" => TeamsButtonKind.Mute,
            ActionPrefix + "camera" => TeamsButtonKind.Camera,
            ActionPrefix + "hangup" => TeamsButtonKind.HangUp,
            ActionPrefix + "hand" => TeamsButtonKind.Hand,
            _ => null,
        };
        if (kind == null)
        {
            return;
        }

        // Invoke off the receive loop; UIA calls can take a moment.
        _ = Task.Run(async () =>
        {
            var invoked = _watcher.Invoke(kind.Value);
            if (!invoked)
            {
                await SendAsync(new { @event = "showAlert", context });
                // The key visually toggles on press even with DisableAutomaticStates
                // unavailable; re-push the truth so it cannot drift.
                await PushStateAsync(action, context, _watcher.Current);
            }
        });
    }

    private async Task PushStateToAllAsync()
    {
        // Serialize full pushes so rapid state churn can't interleave updates out of
        // order; each push re-reads Current, so the latest state always wins.
        try
        {
            await _pushLock.WaitAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        try
        {
            var state = _watcher.Current;
            KeyValuePair<string, string>[] contexts;
            lock (_contextLock)
            {
                contexts = _actionByContext.ToArray();
            }
            foreach (var (context, action) in contexts)
            {
                await PushStateAsync(action, context, state);
            }
        }
        finally
        {
            _pushLock.Release();
        }
    }

    private async Task PushStateAsync(string action, string context, TeamsCallState state)
    {
        if (!state.InCall)
        {
            var disabledImage = action switch
            {
                ActionPrefix + "mute" => ActionImages.MuteDisabled,
                ActionPrefix + "camera" => ActionImages.CameraDisabled,
                ActionPrefix + "hangup" => ActionImages.HangUpDisabled,
                ActionPrefix + "hand" => ActionImages.HandDisabled,
                _ => null,
            };
            if (disabledImage != null)
            {
                await SendAsync(new { @event = "setImage", context, payload = new { image = disabledImage } });
            }
            return;
        }

        // In a call: clear any override so the manifest state image shows again.
        await SendAsync(new { @event = "setImage", context, payload = new { image = "" } });

        int? keyState = action switch
        {
            // State 1 is the "attention" image: muted / camera off / hand raised.
            ActionPrefix + "mute" => state.Muted == true ? 1 : 0,
            ActionPrefix + "camera" => state.CameraOn == false ? 1 : 0,
            ActionPrefix + "hand" => state.HandRaised == true ? 1 : 0,
            _ => null, // hangup has a single state
        };
        if (keyState != null)
        {
            await SendAsync(new { @event = "setState", context, payload = new { state = keyState.Value } });
        }
    }

    private async Task SendAsync(object message)
    {
        try
        {
            await _connection.SendAsync(message, _cts.Token);
        }
        catch (Exception ex)
        {
            Log.Write($"Send failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _watcher.Dispose();
        _connection.Dispose();
        _cts.Dispose();
        _pushLock.Dispose();
    }
}
