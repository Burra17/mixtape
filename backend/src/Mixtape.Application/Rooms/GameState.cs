using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

// Ögonblicksbilden av rummet som skickas med GameStateUpdated vid varje ändring, samma för alla i rummet.
// Gränserna och valen följer med, så att klienten inte har egna kopior av reglerna. Speglas i hubContract.ts.
public sealed record GameState(
    string RoomCode,
    int Revision,
    GamePhase Phase,
    bool IsPaused,
    int Round,
    int? RemainingMs,
    SettingsState Settings,
    IReadOnlyList<int> RoundOptions,
    IReadOnlyList<int> ClipSecondsOptions,
    IReadOnlyList<GamePlayer> Players,
    int MinPlayers,
    int MaxPlayers,
    bool CanStart)
{
    // Tiden som är kvar räknas om för varje bild, så att mobilens nedräkning inte beror på mobilens klocka.
    public static GameState From(GameRoom room, DateTimeOffset now) =>
        new(
            room.Code.Value,
            room.Revision,
            room.Phase,
            room.IsPaused,
            room.Round,
            room.RemainingTime(now) is { } remaining ? (int)Math.Ceiling(remaining.TotalMilliseconds) : null,
            new SettingsState(room.Settings.Rounds, room.Settings.ClipSeconds),
            GameSettings.RoundOptions,
            GameSettings.ClipSecondsOptions,
            room.Players.Select(player => new GamePlayer(player.Id, player.Name.Value, player.ColorIndex, player.IsConnected)).ToList(),
            GameRoom.MinPlayers,
            GameRoom.MaxPlayers,
            room.CanStart);
}
