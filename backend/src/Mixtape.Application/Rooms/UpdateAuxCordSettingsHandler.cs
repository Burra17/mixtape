using Mixtape.Domain.Common;
using Mixtape.Domain.Games.AuxCord;

namespace Mixtape.Application.Rooms;

public sealed class UpdateAuxCordSettingsHandler(IGameRoomRepository rooms, TimeProvider time)
{
    public Task<Result<GameState>> HandleAsync(HostSession host, int? rounds, int? clipSeconds, int? submittingSeconds) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var updated = room.UpdateSettings<AuxCordSettings>(host.HostToken, settings => settings.With(rounds, clipSeconds, submittingSeconds));
            return updated.IsSuccess ? GameState.From(room, time.GetUtcNow()) : updated.Error!;
        });
}
