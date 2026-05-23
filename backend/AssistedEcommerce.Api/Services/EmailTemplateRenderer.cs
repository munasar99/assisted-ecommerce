using System.Net;
using System.Text.RegularExpressions;

namespace AssistedEcommerce.Api.Services;

public static partial class EmailTemplateRenderer
{
    public static string Apply(string template, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        return PlaceholderRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            if (!values.TryGetValue(key, out var raw))
                return match.Value;
            return WebUtility.HtmlEncode(raw ?? string.Empty);
        });
    }

    public static string WrapEmailDocument(string innerHtml, string? footerHtml)
    {
        var footer = string.IsNullOrWhiteSpace(footerHtml) ? "" : footerHtml;
        return $"""
            <!DOCTYPE html>
            <html lang="so">
            <head><meta charset="utf-8" /><meta name="viewport" content="width=device-width" /></head>
            <body style="margin:0;padding:24px;background:#f8fafc;">
              <div style="font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:0 auto;padding:28px;background:#ffffff;border-radius:12px;border:1px solid #e2e8f0;color:#0f172a;">
                {innerHtml}
                {footer}
              </div>
            </body>
            </html>
            """;
    }

    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();
}
