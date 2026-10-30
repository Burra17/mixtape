using Mixtape.Domain.Common;
using Mixtape.Domain.Games;

namespace Mixtape.Application.Rooms;

public sealed class ChooseModeHandler(IGameRoomRepository rooms, TimeProvider time)
{
    public Task<Result<GameState>> HandleAsync(HostSession host, GameMode mode) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var chosen = room.ChooseMode(host.HostToken, mode);
            return chosen.IsSuccess ? GameState.From(room, time.GetUtcNow()) : chosen.Error!;
        });
}
