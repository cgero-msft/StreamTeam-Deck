using System.Text;

namespace StreamTeamDeck.Plugin;

/// <summary>
/// Greyed-out key images pushed while no Teams call is active. In-call images come from
/// the manifest state images; sending an empty setImage restores those.
/// </summary>
internal static class ActionImages
{
    private const string Background = "#1E2530";
    private const string DisabledInk = "#4A5261";

    private const string MicPath =
        "M12 14c1.66 0 3-1.34 3-3V5c0-1.66-1.34-3-3-3S9 3.34 9 5v6c0 1.66 1.34 3 3 3zm5-3c0 2.76-2.24 5-5 5s-5-2.24-5-5H5c0 3.53 2.61 6.43 6 6.92V21h2v-3.08c3.39-.49 6-3.39 6-6.92h-2z";
    private const string CameraPath =
        "M17 10.5V7c0-.55-.45-1-1-1H4c-.55 0-1 .45-1 1v10c0 .55.45 1 1 1h12c.55 0 1-.45 1-1v-3.5l4 4v-11l-4 4z";
    private const string HangUpPath =
        "M12 9c-1.6 0-3.15.25-4.6.72v3.1c0 .39-.23.74-.56.9-.98.49-1.87 1.12-2.66 1.85-.18.18-.43.28-.7.28-.28 0-.53-.11-.71-.29L.29 13.08c-.18-.17-.29-.42-.29-.7 0-.28.11-.53.29-.71C3.34 8.78 7.46 7 12 7s8.66 1.78 11.71 4.67c.18.18.29.43.29.71 0 .28-.11.53-.29.7l-2.48 2.48c-.18.18-.43.29-.71.29-.27 0-.52-.1-.7-.28-.79-.73-1.68-1.36-2.66-1.85-.33-.16-.56-.51-.56-.9v-3.1C15.15 9.25 13.6 9 12 9z";

    private static string DataUri(string innerSvg)
    {
        var svg = $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 144 144"><rect width="144" height="144" rx="24" fill="{Background}"/>{innerSvg}</svg>""";
        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
    }

    private static string CenteredIcon(string path, double scale, double centerY = 11.5) =>
        $"""<path transform="translate(72,72) scale({scale}) translate(-12,-{centerY})" fill="{DisabledInk}" d="{path}"/>""";

    public static readonly string MuteDisabled = DataUri(CenteredIcon(MicPath, 4.2));

    public static readonly string CameraDisabled = DataUri(CenteredIcon(CameraPath, 4.2, centerY: 12));

    public static readonly string HangUpDisabled = DataUri(CenteredIcon(HangUpPath, 4.6, centerY: 11.3));

    public static readonly string HandDisabled = DataUri(
        $"""<g fill="{DisabledInk}"><rect x="42" y="44" width="12" height="40" rx="6"/><rect x="58" y="34" width="12" height="50" rx="6"/><rect x="74" y="38" width="12" height="46" rx="6"/><rect x="90" y="48" width="12" height="36" rx="6"/><path d="M42 74h60v12a28 28 0 0 1-28 28h-6a26 26 0 0 1-26-26Z"/></g>""");
}
