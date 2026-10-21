using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class LeaveRoomHandler(IGameRoomRepository rooms, TimeProvider time)
{
    // Spelaren känns igen på sin token. PlayerId i sessionen litar servern inte på.
    public Task<Result<PlayerRemoved>> HandleAsync(PlayerSession session) =>
        rooms.WithRoomAsync<PlayerRemoved>(session.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var left = room.Leave(session.PlayerToken, now);
            return left.IsSuccess ? new PlayerRemoved(left.Value.Id, GameState.From(room, now)) : left.Error!;
        });
}
