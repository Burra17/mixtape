using Mixtape.Application.Connections;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;
using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Music;

public sealed class SearchTracksHandler(IMusicCatalog catalog, IConnectionRegistry connections, IGameRoomRepository rooms)
{
    // Bara en anslutning som tillhör en spelare får söka, och bara under inskicket. Storskärmen och okända anslutningar får samma fel som ett rum som inte finns.
    public async Task<Result<IReadOnlyList<Track>>> HandleAsync(string connectionId, string? query)
    {
        if (connections.TryGetOwner(connectionId) is not { IsHost: false } owner)
        {
            return RoomErrors.NotFound;
        }

        var parsed = SearchQuery.Create(query);
        if (!parsed.IsSuccess)
        {
            return parsed.Error!;
        }

        // Fasen läses med rummets lås, men sökningen görs efter att låset släppts.
        var allowed = await rooms.WithRoomAsync(owner.RoomCode, room => room.WithGame<AuxCordGame>(game => game.CanChooseTrack()));
        if (!allowed.IsSuccess)
        {
            return allowed.Error!;
        }

        return await catalog.SearchAsync(parsed.Value);
    }
}
