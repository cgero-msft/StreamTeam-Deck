using StreamTeamDeck.Plugin;

// Launched by the Stream Deck app as:
//   StreamTeamDeck.Plugin.exe -port <n> -pluginUUID <uuid> -registerEvent <event> -info <json>
try
{
    var options = PluginArgs.Parse(args);
    using var host = new PluginHost(options);
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Write($"Fatal: {ex}");
    return 1;
}
return 0;
