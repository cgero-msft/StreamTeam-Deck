using System.Text.Json;
using StreamTeamDeck.Core;

namespace StreamTeam_Deck;

class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        using var controller = new TeamsController();

        try
        {
            switch (command)
            {
                case "mute":
                    await controller.ExecuteAsync(TeamsButtonKind.Mute);
                    Console.WriteLine("Mute toggled");
                    break;
                case "hangup":
                    await controller.ExecuteAsync(TeamsButtonKind.HangUp);
                    Console.WriteLine("Call ended");
                    break;
                case "camera":
                    await controller.ExecuteAsync(TeamsButtonKind.Camera);
                    Console.WriteLine("Camera toggled");
                    break;
                case "hand":
                    await controller.ExecuteAsync(TeamsButtonKind.Hand);
                    Console.WriteLine("Hand toggled");
                    break;
                case "status":
                    Console.WriteLine(JsonSerializer.Serialize(controller.ReadState(), JsonOptions));
                    break;
                case "watch":
                    await WatchAsync();
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

    private static async Task WatchAsync()
    {
        using var watcher = new TeamsStateWatcher();
        var done = new TaskCompletionSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            done.TrySetResult();
        };
        watcher.StateChanged += state => Console.WriteLine(JsonSerializer.Serialize(state, JsonOptions));
        watcher.Start();
        Console.WriteLine(JsonSerializer.Serialize(watcher.Current, JsonOptions));
        await done.Task;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("StreamTeam Deck - Teams Controller");
        Console.WriteLine("Usage:");
        Console.WriteLine("  StreamTeamDeck.exe mute      - Toggle mute/unmute");
        Console.WriteLine("  StreamTeamDeck.exe hangup    - Hang up the call");
        Console.WriteLine("  StreamTeamDeck.exe camera    - Toggle camera on/off");
        Console.WriteLine("  StreamTeamDeck.exe hand      - Raise/lower hand");
        Console.WriteLine("  StreamTeamDeck.exe status    - Print call state as JSON");
        Console.WriteLine("  StreamTeamDeck.exe watch     - Stream call-state changes as JSON lines");
    }
}
