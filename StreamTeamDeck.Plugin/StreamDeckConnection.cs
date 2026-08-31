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

    /// <summary>Returns the next message, or null when the socket closes.</summary>
    public async Task<JsonDocument?> ReceiveAsync(CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        using var message = new MemoryStream();
        while (true)
        {
            var result = await _socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }
            message.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                break;
            }
        }
        return message.Length == 0 ? null : JsonDocument.Parse(message.ToArray());
    }

    public void Dispose()
    {
        _socket.Dispose();
        _sendLock.Dispose();
    }
}
