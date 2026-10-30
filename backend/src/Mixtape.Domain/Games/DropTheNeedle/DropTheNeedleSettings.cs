using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Domain.Games.DropTheNeedle;

// Det värden ställer in i lobbyn för Drop the Needle. Kategorin kommer i #69.
public sealed record DropTheNeedleSettings : IGameSettings
{
    public static readonly IReadOnlyList<int> SongOptions = [5, 10, 15, 20];

    // Klippet är också gissningstiden. Deezers klipp är 30 s, så längre än så går inte.
    public static readonly IReadOnlyList<int> ClipSecondsOptions = [20, 25, 30];

    public static readonly DropTheNeedleSettings Default = new(10, 30);

    private DropTheNeedleSettings(int songs, int clipSeconds)
    {
        Songs = songs;
        ClipSeconds = clipSeconds;
    }

    public GameMode Mode => GameMode.DropTheNeedle;

    public int MinPlayers => 2;

    public int Songs { get; }

    public int ClipSeconds { get; }

    public static Result<DropTheNeedleSettings> Create(int songs, int clipSeconds) =>
        SongOptions.Contains(songs) && ClipSecondsOptions.Contains(clipSeconds) ? new DropTheNeedleSettings(songs, clipSeconds) : RoomErrors.InvalidSettings;

    // Som för Aux Cord Battles: null behåller inställningen.
    public Result<DropTheNeedleSettings> With(int? songs, int? clipSeconds) => Create(songs ?? Songs, clipSeconds ?? ClipSeconds);
}
