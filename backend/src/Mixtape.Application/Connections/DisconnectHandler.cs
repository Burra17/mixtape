using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;

namespace Mixtape.Application.Connections;

public sealed class DisconnectHandler(IGameRoomRepository rooms, IConnectionRegistry connections)
{
    // Anslutningen är redan borta ur registret. Andra anslutningar kontrolleras med rummets lås, eftersom en mobil
    // som låses upp kan hinna återansluta innan den gamla anslutningens disconnect kommer fram.
    public Task<Result<LobbyState>> HandleAsync(ConnectionOwner owner) =>
        rooms.WithRoomAsync<LobbyState>(owner.RoomCode, room =>
        {
            if (connections.HasConnection(owner))
            {
                return LobbyState.From(room);
            }

            if (owner.IsHost)
            {
                room.DisconnectHost();
                return LobbyState.From(room);
            }

            var disconnected = room.Disconnect(owner.PlayerId!);
            return disconnected.IsSuccess ? LobbyState.From(room) : disconnected.Error!;
        });
}
