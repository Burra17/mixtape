using Mixtape.Domain.Common;
using Mixtape.Domain.Games.DropTheNeedle;

namespace Mixtape.Application.Rooms;

public sealed class UpdateDropTheNeedleSettingsHandler(IGameRoomRepository rooms, TimeProvider time)
{
    public Task<Result<GameState>> HandleAsync(HostSession host, int? songs, int? clipSeconds) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var updated = room.UpdateSettings<DropTheNeedleSettings>(host.HostToken, settings => settings.With(songs, clipSeconds));
            return updated.IsSuccess ? GameState.From(room, time.GetUtcNow()) : updated.Error!;
        });
}
