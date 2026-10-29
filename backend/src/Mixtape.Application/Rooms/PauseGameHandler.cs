using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class PauseGameHandler(IGameRoomRepository rooms, TimeProvider time)
{
    public Task<Result<GameState>> HandleAsync(HostSession host) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var paused = room.Pause(host.HostToken, now);
            return paused.IsSuccess ? GameState.From(room, now) : paused.Error!;
        });
}
