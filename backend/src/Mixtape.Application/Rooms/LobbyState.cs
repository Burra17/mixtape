using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

// Ögonblicksbilden av lobbyn som skickas med LobbyUpdated vid varje ändring. Gränserna följer med, så att klienten inte har egna kopior av reglerna. Speglas i hubContract.ts.
public sealed record LobbyState(
    string RoomCode,
    int Revision,
    IReadOnlyList<LobbyPlayer> Players,
    int MinPlayers,
    int MaxPlayers,
    bool CanStart,
    bool IsPaused)
{
    public static LobbyState From(GameRoom room) =>
        new(
            room.Code.Value,
            room.Revision,
            room.Players.Select(player => new LobbyPlayer(player.Id, player.Name.Value, player.ColorIndex, player.IsConnected)).ToList(),
            GameRoom.MinPlayers,
            GameRoom.MaxPlayers,
            room.CanStart,
            room.IsPaused);
}
