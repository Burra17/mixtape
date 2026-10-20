using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class CheckRoomHandler(IGameRoomRepository rooms)
{
    // Svarar bara om det sparade rummet finns kvar. Pausen hävs först när värden klickar "Fortsätt", eftersom klicket låser upp ljudet.
    public Task<Result<HostSession>> HandleAsync(HostSession session) =>
        rooms.WithRoomAsync<HostSession>(session.RoomCode, room => room.IsHost(session.HostToken) ? HostSession.For(room) : RoomErrors.NotFound);
}
