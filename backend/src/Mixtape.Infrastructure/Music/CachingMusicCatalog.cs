using Microsoft.Extensions.Caching.Memory;
using Mixtape.Application.Music;
using Mixtape.Domain.Common;

namespace Mixtape.Infrastructure.Music;

// Lägger en cache framför musikkällan. Sökordet är redan normaliserat i SearchQuery, så det fungerar direkt som nyckel.
// Träffarna saknar klipplänk, så inget som slutar gälla hamnar i cachen.
public sealed class CachingMusicCatalog(IMusicCatalog inner, IMemoryCache cache) : IMusicCatalog
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

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
            cache.Set(key, result.Value, Lifetime);
        }

        return result;
    }
}
