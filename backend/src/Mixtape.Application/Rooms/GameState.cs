using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

// Ögonblicksbilden av rummet, samma för alla, med reglernas gränser och val så att klienten inte har egna kopior. Speglas i hubContract.ts.
public sealed record GameState(
    string RoomCode,
    int Revision,
    GamePhase Phase,
    bool IsPaused,
    int Round,
    string? Prompt,
    bool SkippedRound,
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
            room.Prompt,
            room.SkippedRound,
            room.RemainingTime(now) is { } remaining ? (int)Math.Ceiling(remaining.TotalMilliseconds) : null,
            new SettingsState(room.Settings.Rounds, room.Settings.ClipSeconds),
            GameSettings.RoundOptions,
            GameSettings.ClipSecondsOptions,
            room.Players.Select(player => new GamePlayer(player.Id, player.Name.Value, player.ColorIndex, player.IsConnected, room.HasSubmitted(player.Id))).ToList(),
            GameRoom.MinPlayers,
            GameRoom.MaxPlayers,
            room.CanStart);
}
