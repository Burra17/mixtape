using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Domain.Games.DropTheNeedle;

// Det värden ställer in i lobbyn för Drop the Needle. Domänen vet inte vilka kategorier som finns, så handlern kontrollerar CategoryId innan det kommer hit.
public sealed record DropTheNeedleSettings : IGameSettings
{
    public static readonly IReadOnlyList<int> SongOptions = [5, 10, 15, 20];

    // Klippet är också gissningstiden. Deezers klipp är 30 s, så längre än så går inte.
    public static readonly IReadOnlyList<int> ClipSecondsOptions = [20, 25, 30];

    // 00-talet är standard, eftersom de flesta i sällskapet känner igen den eran.
    public static readonly DropTheNeedleSettings Default = new(10, 30, "00s");

    private DropTheNeedleSettings(int songs, int clipSeconds, string categoryId)
    {
        Songs = songs;
        ClipSeconds = clipSeconds;
        CategoryId = categoryId;
    }

    public GameMode Mode => GameMode.DropTheNeedle;

    public int MinPlayers => 2;

    public int Songs { get; }

    public int ClipSeconds { get; }

    public string CategoryId { get; }

    public static Result<DropTheNeedleSettings> Create(int songs, int clipSeconds, string categoryId) =>
        SongOptions.Contains(songs) && ClipSecondsOptions.Contains(clipSeconds) && !string.IsNullOrWhiteSpace(categoryId)
            ? new DropTheNeedleSettings(songs, clipSeconds, categoryId)
            : RoomErrors.InvalidSettings;

    // Som för Aux Cord Battles: null behåller inställningen.
    public Result<DropTheNeedleSettings> With(int? songs, int? clipSeconds, string? categoryId) =>
        Create(songs ?? Songs, clipSeconds ?? ClipSeconds, categoryId ?? CategoryId);
}
