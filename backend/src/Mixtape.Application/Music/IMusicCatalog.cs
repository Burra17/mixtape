using Mixtape.Domain.Common;

namespace Mixtape.Application.Music;

// Musikkällan. I dag Deezer. En annan källa, som iTunes, blir en ny klass bakom samma interface.
public interface IMusicCatalog
{
    // Högst 8 träffar som alla har ett klipp. Inga träffar är en lyckad, tom lista.
    Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
}
