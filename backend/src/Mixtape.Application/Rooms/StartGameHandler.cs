using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class StartGameHandler(IGameRoomRepository rooms, TimeProvider time)
{
    public Task<Result<GameState>> HandleAsync(HostSession host) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var started = room.StartGame(host.HostToken, now);
            return started.IsSuccess ? GameState.From(room, now) : started.Error!;
        });
}
