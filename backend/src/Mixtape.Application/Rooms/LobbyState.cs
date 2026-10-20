using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

// Ögonblicksbilden av lobbyn som skickas med LobbyUpdated vid varje ändring. Speglas i hubContract.ts.
public sealed record LobbyState(string RoomCode, int Revision, IReadOnlyList<LobbyPlayer> Players, int MaxPlayers, bool CanStart)
{
    public static LobbyState From(GameRoom room) =>
        new(
            room.Code.Value,
            room.Revision,
            room.Players.Select(player => new LobbyPlayer(player.Id, player.Name.Value, player.ColorIndex)).ToList(),
            GameRoom.MaxPlayers,
            room.CanStart);
}
