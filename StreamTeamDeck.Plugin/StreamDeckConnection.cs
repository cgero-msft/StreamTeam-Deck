using System.Net.WebSockets;
using System.Text.Json;

namespace StreamTeamDeck.Plugin;

/// <summary>Minimal WebSocket client for the Elgato Stream Deck plugin protocol.</summary>
internal sealed class StreamDeckConnection : IDisposable
{
    private readonly ClientWebSocket _socket = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public async Task ConnectAsync(int port, string pluginUuid, string registerEvent, CancellationToken ct)
    {
        await _socket.ConnectAsync(new Uri($"ws://127.0.0.1:{port}"), ct);
        await SendAsync(new { @event = registerEvent, uuid = pluginUuid }, ct);
    }

    public async Task SendAsync(object message, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message);
        await _sendLock.WaitAsync(ct);
        try
        {
            await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Returns the next parseable message, or null when the socket closes. Malformed or
    /// oversized messages are skipped rather than tearing down the connection.
    /// </summary>
    public async Task<JsonDocument?> ReceiveAsync(CancellationToken ct)
    {
        const int MaxMessageBytes = 4 * 1024 * 1024;
        var buffer = new byte[64 * 1024];
        while (true)
        {
            using var message = new MemoryStream();
            var oversized = false;
            while (true)
            {
                var result = await _socket.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return null;
                }
                if (message.Length + result.Count > MaxMessageBytes)
                {
                    oversized = true; // keep draining frames until the message ends
                }
                else
                {
                    message.Write(buffer, 0, result.Count);
                }
                if (result.EndOfMessage)
                {
                    break;
                }
            }

            if (oversized || message.Length == 0)
            {
                Log.Write(oversized ? "Skipped oversized message" : "Skipped empty message");
                continue;
            }
            try
            {
                return JsonDocument.Parse(message.ToArray());
            }
            catch (JsonException ex)
            {
                Log.Write($"Skipped malformed message: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _socket.Dispose();
        _sendLock.Dispose();
    }
}
