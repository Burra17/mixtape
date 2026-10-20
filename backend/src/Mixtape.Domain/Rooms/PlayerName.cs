using System.Globalization;
using System.Text.RegularExpressions;
using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

public sealed partial record PlayerName
{
    public const int MaxLength = 16;

    private PlayerName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    // Mellanslag i början och slutet tas bort och flera i rad blir ett. Längden räknas som tecknen syns, så en emoji räknas som ett.
    public static Result<PlayerName> Create(string? input)
    {
        var value = Whitespace().Replace(input ?? string.Empty, " ").Trim();
        var length = new StringInfo(value).LengthInTextElements;
        if (length is < 1 or > MaxLength)
        {
            return PlayerErrors.InvalidName;
        }

        return new PlayerName(value);
    }

    // Två namn som bara skiljer sig i versaler räknas som samma namn.
    public bool IsSameAs(PlayerName other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
