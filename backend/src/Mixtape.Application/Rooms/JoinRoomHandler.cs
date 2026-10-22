using Mixtape.Application.Connections;
using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class JoinRoomHandler(IGameRoomRepository rooms, IConnectionRegistry connections, TimeProvider time)
{
    public Task<Result<JoinedRoom>> HandleAsync(string? roomCode, string? name, string connectionId) =>
        rooms.WithRoomAsync<JoinedRoom>(roomCode, room =>
        {
            var joined = room.Join(name);
            if (!joined.IsSuccess)
            {
                return joined.Error!;
            }

            var player = joined.Value;
            connections.Register(connectionId, ConnectionOwner.Player(room.Code.Value, player.Id));
            return new JoinedRoom(new PlayerSession(room.Code.Value, player.Id, player.Token), GameState.From(room, time.GetUtcNow()));
        });
}
