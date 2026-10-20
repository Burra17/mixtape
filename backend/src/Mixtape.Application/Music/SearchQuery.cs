using System.Text.RegularExpressions;
using Mixtape.Domain.Common;

namespace Mixtape.Application.Music;

// Ett sökord som är trimmat, har gemener och har mellanslagen ihopslagna. Samma sökning skriven på olika sätt ger då samma träff i cachen.
public sealed partial record SearchQuery
{
    public const int MaxLength = 100;

    private SearchQuery(string value)
    {
        Value = value;
    }

    public string Value { get; }

    // Ett tomt sökord skickas aldrig till Deezer, som svarar med ett fel på det.
    public static Result<SearchQuery> Create(string? input)
    {
        var value = Whitespace().Replace(input ?? string.Empty, " ").Trim().ToLowerInvariant();
        if (value.Length is 0 or > MaxLength)
        {
            return MusicErrors.InvalidQuery;
        }

        return new SearchQuery(value);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
