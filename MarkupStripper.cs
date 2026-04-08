using System.Text.RegularExpressions;

namespace DisplayLibrary.Internal;

/// <summary>
/// Strips Spectre Console markup tags so plain text can be forwarded to ILogger.
/// Handles both self-closing [/] and paired [tag]…[/tag] forms.
/// </summary>
internal static partial class MarkupStripper
{
    // Matches any [...] tag, including [/], [green], [bold red], etc.
    [GeneratedRegex(@"\[[^\]]*\]", RegexOptions.Compiled)]
    private static partial Regex TagPattern();

    public static string Strip(string markup) =>
        string.IsNullOrEmpty(markup) ? markup : TagPattern().Replace(markup, string.Empty);
}
