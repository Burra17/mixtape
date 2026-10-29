using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class ContinueGameHandler(IGameRoomRepository rooms, TimeProvider time)
{
    public Task<Result<GameState>> HandleAsync(HostSession host) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var continued = room.Continue(host.HostToken, now);
            return continued.IsSuccess ? GameState.From(room, now) : continued.Error!;
        });
}
