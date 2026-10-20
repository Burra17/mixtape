using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class LeaveRoomHandler(IGameRoomRepository rooms)
{
    // Spelaren känns igen på sin token. PlayerId i sessionen litar servern inte på.
    public Task<Result<PlayerRemoved>> HandleAsync(PlayerSession session) =>
        rooms.WithRoomAsync<PlayerRemoved>(session.RoomCode, room =>
        {
            var left = room.Leave(session.PlayerToken);
            return left.IsSuccess ? new PlayerRemoved(left.Value.Id, LobbyState.From(room)) : left.Error!;
        });
}
