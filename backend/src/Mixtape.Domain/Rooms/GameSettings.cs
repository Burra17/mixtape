using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

// Det värden ställer in i lobbyn. Bara valen nedan godtas, så att klienten inte kan skicka 100 rundor.
public sealed record GameSettings
{
    public static readonly IReadOnlyList<int> RoundOptions = [3, 5, 8];

    public static readonly IReadOnlyList<int> ClipSecondsOptions = [10, 15, 20];

    public static readonly GameSettings Default = new(5, 15);

    private GameSettings(int rounds, int clipSeconds)
    {
        Rounds = rounds;
        ClipSeconds = clipSeconds;
    }

    public int Rounds { get; }

    public int ClipSeconds { get; }

    public TimeSpan ClipLength => TimeSpan.FromSeconds(ClipSeconds);

    public static Result<GameSettings> Create(int rounds, int clipSeconds) =>
        RoundOptions.Contains(rounds) && ClipSecondsOptions.Contains(clipSeconds) ? new GameSettings(rounds, clipSeconds) : RoomErrors.InvalidSettings;
}
