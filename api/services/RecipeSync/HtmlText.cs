using System.Net;
using System.Text.RegularExpressions;

namespace api.Services.RecipeSync;

/// <summary>Gør Mambenos HTML-felter (beskrivelse, tip, trin, titler) til ren tekst med bevarede linjeskift.</summary>
public static partial class HtmlText
{
    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var text = LineBreakTagRegex().Replace(html, "\r\n");
        text = TagRegex().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text).Replace(' ', ' ');
        text = ExcessBlankLinesRegex().Replace(text, "\r\n\r\n");
        return text.Trim();
    }

    public static string? ToPlainTextOrNull(string? html)
    {
        var text = ToPlainText(html);
        return text.Length == 0 ? null : text;
    }

    [GeneratedRegex(@"<br\s*/?>|</p>|</div>|</li>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakTagRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"(\r?\n[ \t]*){3,}")]
    private static partial Regex ExcessBlankLinesRegex();
}
