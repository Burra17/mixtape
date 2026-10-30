using Mixtape.Application.Music;
using Mixtape.Domain.Common;
using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class UpdateDropTheNeedleSettingsHandler(IGameRoomRepository rooms, ICategoryCatalog categories, TimeProvider time)
{
    // Domänen vet inte vilka kategorier som finns, så en okänd kategori nekas här, som ett val som inte finns.
    public Task<Result<GameState>> HandleAsync(HostSession host, int? songs, int? clipSeconds, string? categoryId)
    {
        if (categoryId is not null && categories.Find(categoryId) is null)
        {
            return Task.FromResult<Result<GameState>>(RoomErrors.InvalidSettings);
        }

        return rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var updated = room.UpdateSettings<DropTheNeedleSettings>(host.HostToken, settings => settings.With(songs, clipSeconds, categoryId));
            return updated.IsSuccess ? GameState.From(room, time.GetUtcNow()) : updated.Error!;
        });
    }
}
