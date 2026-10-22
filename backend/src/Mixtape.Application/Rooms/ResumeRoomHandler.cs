using Mixtape.Application.Connections;
using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class ResumeRoomHandler(IGameRoomRepository rooms, IConnectionRegistry connections, TimeProvider time)
{
    // Ger rummet tillbaka till en storskärm som har rätt värd-token, till exempel efter en omladdning, och häver pausen.
    public Task<Result<HostRoom>> HandleAsync(HostSession session, string connectionId) =>
        rooms.WithRoomAsync<HostRoom>(session.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var resumed = room.ResumeHost(session.HostToken, now);
            if (!resumed.IsSuccess)
            {
                return resumed.Error!;
            }

            connections.Register(connectionId, ConnectionOwner.Host(room.Code.Value));
            return new HostRoom(HostSession.For(room), GameState.From(room, now));
        });
}
