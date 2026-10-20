using Mixtape.Application.Connections;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Music;

public sealed class SearchTracksHandler(IMusicCatalog catalog, IConnectionRegistry connections)
{
    // Bara en anslutning som tillhör en spelare får söka. Storskärmen och okända anslutningar får samma fel som ett rum som inte finns.
    public async Task<Result<IReadOnlyList<Track>>> HandleAsync(string connectionId, string? query)
    {
        if (connections.TryGetOwner(connectionId) is not { IsHost: false })
        {
            return RoomErrors.NotFound;
        }

        var parsed = SearchQuery.Create(query);
        if (!parsed.IsSuccess)
        {
            return parsed.Error!;
        }

        return await catalog.SearchAsync(parsed.Value);
    }
}
