using Microsoft.Extensions.Caching.Memory;
using Mixtape.Application.Music;
using Mixtape.Domain.Common;

namespace Mixtape.Infrastructure.Music;

// En cache framför musikkällan. Sökordet är redan normaliserat i SearchQuery och träffarna saknar klipplänk, så inget som slutar gälla cachas.
public sealed class CachingMusicCatalog(IMusicCatalog inner, IMemoryCache cache) : IMusicCatalog
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var key = $"search:{query.Value}";
        if (cache.TryGetValue(key, out IReadOnlyList<Track>? cached) && cached is not null)
        {
            return Result<IReadOnlyList<Track>>.Success(cached);
        }

        var result = await inner.SearchAsync(query, cancellationToken);

        // Fel cachas aldrig, så att nästa försök går till Deezer igen.
        if (result.IsSuccess)
        {
            cache.Set(key, result.Value, CacheDuration);
        }

        return result;
    }

    // Går alltid till källan, eftersom klipplänken slutar gälla efter 15 minuter.
    public Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default) =>
        inner.GetPlayableTrackAsync(trackId, cancellationToken);
}
