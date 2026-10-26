using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class PlayAgainHandler(IGameRoomRepository rooms, TimeProvider time)
{
    public Task<Result<GameState>> HandleAsync(HostSession host) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var restarted = room.PlayAgain(host.HostToken);
            return restarted.IsSuccess ? GameState.From(room, time.GetUtcNow()) : restarted.Error!;
        });
}
