namespace StreamTeamDeck.Plugin;

/// <summary>Best-effort file logger (logs/plugin.log next to the plugin executable).</summary>
internal static class Log
{
    private static readonly object Lock = new();
    private static readonly string? LogPath = InitPath();

    private static string? InitPath()
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "plugin.log");
        }
        catch
        {
            return null;
        }
    }

    public static void Write(string message)
    {
        if (LogPath == null)
        {
            return;
        }
        try
        {
            lock (Lock)
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
            }
        }
        catch
        {
        }
    }
}
