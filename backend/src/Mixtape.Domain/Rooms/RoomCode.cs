namespace Mixtape.Domain.Rooms;

public sealed record RoomCode
{
    // Bara konsonanter, så att slumpen aldrig bildar ord. Q är borta eftersom den lätt läses som O.
    public const string Alphabet = "BCDFGHJKLMNPRSTVWXZ";
    public const int Length = 4;

    private RoomCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static RoomCode Generate(Random random)
    {
        var letters = new char[Length];
        for (var i = 0; i < Length; i++)
        {
            letters[i] = Alphabet[random.Next(Alphabet.Length)];
        }

        return new RoomCode(new string(letters));
    }

    // Tolkar en kod som någon skrivit eller som kommit från klienten. Gemener godtas.
    public static RoomCode? Parse(string? input)
    {
        var value = input?.Trim().ToUpperInvariant();
        if (value is null || value.Length != Length || !value.All(Alphabet.Contains))
        {
            return null;
        }

        return new RoomCode(value);
    }

    public override string ToString() => Value;
}
