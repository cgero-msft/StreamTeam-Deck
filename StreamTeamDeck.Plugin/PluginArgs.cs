namespace StreamTeamDeck.Plugin;

public sealed record PluginArgs(int Port, string PluginUuid, string RegisterEvent)
{
    public static PluginArgs Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i + 1 < args.Length; i += 2)
        {
            if (args[i].StartsWith('-'))
            {
                values[args[i].TrimStart('-')] = args[i + 1];
            }
        }

        if (!values.TryGetValue("port", out var port) ||
            !values.TryGetValue("pluginUUID", out var uuid) ||
            !values.TryGetValue("registerEvent", out var registerEvent))
        {
            throw new ArgumentException("Missing required Stream Deck arguments (-port, -pluginUUID, -registerEvent)");
        }

        return new PluginArgs(int.Parse(port), uuid, registerEvent);
    }
}
