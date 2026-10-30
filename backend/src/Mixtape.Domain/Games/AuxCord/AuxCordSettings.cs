using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Domain.Games.AuxCord;

// Det värden ställer in i lobbyn för Aux Cord Battles. Bara valen nedan godtas, så att klienten inte kan skicka 100 rundor.
public sealed record AuxCordSettings : IGameSettings
{
    public static readonly IReadOnlyList<int> RoundOptions = [3, 5, 8];

    public static readonly IReadOnlyList<int> ClipSecondsOptions = [10, 15, 20];

    public static readonly IReadOnlyList<int> SubmittingSecondsOptions = [60, 90, 120];

    public static readonly AuxCordSettings Default = new(5, 15, 90);

    private AuxCordSettings(int rounds, int clipSeconds, int submittingSeconds)
    {
        Rounds = rounds;
        ClipSeconds = clipSeconds;
        SubmittingSeconds = submittingSeconds;
    }

    public GameMode Mode => GameMode.AuxCord;

    // Man kan inte rösta på sin egen låt, så med två spelare finns bara ett bidrag att rösta på.
    public int MinPlayers => 3;

    public int Rounds { get; }

    public int ClipSeconds { get; }

    public int SubmittingSeconds { get; }

    public TimeSpan ClipLength => TimeSpan.FromSeconds(ClipSeconds);

    public TimeSpan SubmittingDuration => TimeSpan.FromSeconds(SubmittingSeconds);

    public static Result<AuxCordSettings> Create(int rounds, int clipSeconds, int submittingSeconds) =>
        RoundOptions.Contains(rounds) && ClipSecondsOptions.Contains(clipSeconds) && SubmittingSecondsOptions.Contains(submittingSeconds)
            ? new AuxCordSettings(rounds, clipSeconds, submittingSeconds)
            : RoomErrors.InvalidSettings;

    // Bara inställningen som ändras skickas, och null behåller de andra. Två snabba klick skriver då inte över varandra med ett gammalt värde.
    public Result<AuxCordSettings> With(int? rounds, int? clipSeconds, int? submittingSeconds) =>
        Create(rounds ?? Rounds, clipSeconds ?? ClipSeconds, submittingSeconds ?? SubmittingSeconds);
}
