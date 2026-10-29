using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class EndGameHandler(IGameRoomRepository rooms, TimeProvider time)
{
    public Task<Result<GameState>> HandleAsync(HostSession host) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var ended = room.End(host.HostToken, now);
            return ended.IsSuccess ? GameState.From(room, now) : ended.Error!;
        });
}
