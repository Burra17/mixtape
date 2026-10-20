using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class LeaveRoomHandler(IGameRoomRepository rooms)
{
    // Spelaren känns igen på sin token. PlayerId i sessionen litar servern inte på.
    public async Task<Result<PlayerRemoved>> HandleAsync(PlayerSession session)
    {
        if (RoomCode.Parse(session.RoomCode) is not { } code)
        {
            return RoomErrors.NotFound;
        }

        return await rooms.WithRoomAsync<PlayerRemoved>(code, room =>
        {
            var left = room.Leave(session.PlayerToken);
            return left.IsSuccess ? new PlayerRemoved(left.Value.Id, LobbyState.From(room)) : left.Error!;
        });
    }
}
