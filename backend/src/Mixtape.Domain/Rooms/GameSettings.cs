using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

// Det värden ställer in i lobbyn. Bara valen nedan godtas, så att klienten inte kan skicka 100 rundor.
public sealed record GameSettings
{
    public static readonly IReadOnlyList<int> RoundOptions = [3, 5, 8];

    public static readonly IReadOnlyList<int> ClipSecondsOptions = [10, 15, 20];

    public static readonly IReadOnlyList<int> SubmittingSecondsOptions = [60, 90, 120];

    public static readonly GameSettings Default = new(5, 15, 90);

    private GameSettings(int rounds, int clipSeconds, int submittingSeconds)
    {
        Rounds = rounds;
        ClipSeconds = clipSeconds;
        SubmittingSeconds = submittingSeconds;
    }

    public int Rounds { get; }

    public int ClipSeconds { get; }

    public int SubmittingSeconds { get; }

    public TimeSpan ClipLength => TimeSpan.FromSeconds(ClipSeconds);

    public TimeSpan SubmittingDuration => TimeSpan.FromSeconds(SubmittingSeconds);

    public static Result<GameSettings> Create(int rounds, int clipSeconds, int submittingSeconds) =>
        RoundOptions.Contains(rounds) && ClipSecondsOptions.Contains(clipSeconds) && SubmittingSecondsOptions.Contains(submittingSeconds)
            ? new GameSettings(rounds, clipSeconds, submittingSeconds)
            : RoomErrors.InvalidSettings;
}
