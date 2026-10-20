using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class ResumeRoomHandler(IGameRoomRepository rooms)
{
    // Ger rummet tillbaka till en storskärm som har rätt värd-token, till exempel efter en omladdning.
    public async Task<Result<HostRoom>> HandleAsync(HostSession session)
    {
        if (RoomCode.Parse(session.RoomCode) is not { } code)
        {
            return RoomErrors.NotFound;
        }

        return await rooms.WithRoomAsync<HostRoom>(code, room =>
            room.IsHost(session.HostToken) ? new HostRoom(new HostSession(room.Code.Value, room.HostToken), LobbyState.From(room)) : RoomErrors.NotFound);
    }
}
