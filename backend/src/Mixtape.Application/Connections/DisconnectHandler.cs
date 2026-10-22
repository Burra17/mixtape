using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;

namespace Mixtape.Application.Connections;

public sealed class DisconnectHandler(IGameRoomRepository rooms, IConnectionRegistry connections, TimeProvider time)
{
    // Anslutningen är redan borta ur registret. Andra anslutningar kontrolleras med rummets lås, eftersom en mobil
    // som låses upp kan hinna återansluta innan den gamla anslutningens disconnect kommer fram.
    public Task<Result<GameState>> HandleAsync(ConnectionOwner owner) =>
        rooms.WithRoomAsync<GameState>(owner.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            if (connections.HasConnection(owner))
            {
                return GameState.From(room, now);
            }

            if (owner.IsHost)
            {
                room.DisconnectHost(now);
                return GameState.From(room, now);
            }

            var disconnected = room.Disconnect(owner.PlayerId!, now);
            return disconnected.IsSuccess ? GameState.From(room, now) : disconnected.Error!;
        });
}
