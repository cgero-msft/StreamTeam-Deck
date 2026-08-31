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

    private static string DataUri(string innerSvg)
    {
        var svg = $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 144 144"><rect width="144" height="144" rx="24" fill="{Background}"/>{innerSvg}</svg>""";
        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
    }

    public static readonly string MuteDisabled = DataUri(
        $"""<rect x="58" y="30" width="28" height="52" rx="14" fill="{DisabledInk}"/><path d="M46 66v8a26 26 0 0 0 52 0v-8" fill="none" stroke="{DisabledInk}" stroke-width="8" stroke-linecap="round"/><line x1="72" y1="100" x2="72" y2="112" stroke="{DisabledInk}" stroke-width="8" stroke-linecap="round"/><line x1="56" y1="112" x2="88" y2="112" stroke="{DisabledInk}" stroke-width="8" stroke-linecap="round"/>""");

    public static readonly string CameraDisabled = DataUri(
        $"""<rect x="28" y="48" width="58" height="48" rx="10" fill="{DisabledInk}"/><path d="M92 62 L116 48 v48 L92 82 Z" fill="{DisabledInk}"/>""");

    public static readonly string HangUpDisabled = DataUri(
        $"""<path d="M30 88c0-18 84-18 84 0l-5 12c-2 4-7 5-11 3l-11-6c-6-3-30-3-36 0l-11 6c-4 2-9 1-11-3Z" fill="{DisabledInk}"/>""");

    public static readonly string HandDisabled = DataUri(
        $"""<rect x="50" y="36" width="10" height="42" rx="5" fill="{DisabledInk}"/><rect x="64" y="28" width="10" height="50" rx="5" fill="{DisabledInk}"/><rect x="78" y="32" width="10" height="46" rx="5" fill="{DisabledInk}"/><path d="M50 70h38v14a24 24 0 0 1-24 24h-2a22 22 0 0 1-22-22v-8Z" fill="{DisabledInk}"/>""");
}
