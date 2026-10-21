using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class UpdateSettingsHandler(IGameRoomRepository rooms, TimeProvider time)
{
    public Task<Result<GameState>> HandleAsync(HostSession host, int rounds, int clipSeconds) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var updated = room.UpdateSettings(host.HostToken, rounds, clipSeconds);
            return updated.IsSuccess ? GameState.From(room, time.GetUtcNow()) : updated.Error!;
        });
}
