using Mixtape.Application.Connections;
using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class ResumeRoomHandler(IGameRoomRepository rooms, IConnectionRegistry connections)
{
    // Ger rummet tillbaka till en storskärm som har rätt värd-token, till exempel efter en omladdning, och häver pausen.
    public Task<Result<HostRoom>> HandleAsync(HostSession session, string connectionId) =>
        rooms.WithRoomAsync<HostRoom>(session.RoomCode, room =>
        {
            var resumed = room.ResumeHost(session.HostToken);
            if (!resumed.IsSuccess)
            {
                return resumed.Error!;
            }

            connections.Register(connectionId, ConnectionOwner.Host(room.Code.Value));
            return new HostRoom(new HostSession(room.Code.Value, room.HostToken), LobbyState.From(room));
        });
}
