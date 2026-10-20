using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class ResumeRoomHandler(IGameRoomRepository rooms)
{
    // Ger rummet tillbaka till en storskärm som har rätt värd-token, till exempel efter en omladdning.
    public Task<Result<HostRoom>> HandleAsync(HostSession session) =>
        rooms.WithRoomAsync<HostRoom>(session.RoomCode, room =>
            room.IsHost(session.HostToken) ? new HostRoom(new HostSession(room.Code.Value, room.HostToken), LobbyState.From(room)) : RoomErrors.NotFound);
}
