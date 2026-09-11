using System.Globalization;
using System.Text;
using Racinglazing.Forum.Application.Abstractions.Services;

namespace Racinglazing.Forum.Infrastructure.Services;

/// <summary>Generates lowercase, ASCII, hyphen-separated slugs.</summary>
public sealed class SlugGenerator : ISlugGenerator
{
    private const int MaxLength = 200;

    public string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Strip diacritics (e.g. "Pérez" -> "perez").
        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        var lastWasHyphen = false;

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark) continue;

            if (char.IsLetterOrDigit(ch) && ch < 128)
            {
                sb.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && sb.Length > 0)
            {
                sb.Append('-');
                lastWasHyphen = true;
            }
        }

        var slug = sb.ToString().Trim('-');
        return slug.Length > MaxLength ? slug[..MaxLength].Trim('-') : slug;
    }
}

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
